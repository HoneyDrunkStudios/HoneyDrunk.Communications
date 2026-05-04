using HoneyDrunk.Communications.Abstractions;
using HoneyDrunk.Communications.Intents;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Kernel.Abstractions.Telemetry;
using HoneyDrunk.Notify.Abstractions;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Communications.Internal;

/// <summary>
/// Default Communications orchestrator for the Phase 2 welcome flow.
/// </summary>
public sealed class CommunicationOrchestrator(
    IRecipientResolver recipientResolver,
    IPreferenceStore preferenceStore,
    ICadencePolicy cadencePolicy,
    INotificationSender notificationSender,
    IDecisionLog decisionLog,
    IGridContextAccessor gridContextAccessor,
    ITelemetryActivityFactory telemetryActivityFactory,
    InMemoryFollowupScheduler followupScheduler,
    IOptionsMonitor<CommunicationsOptions> options) : ICommunicationOrchestrator
{
    /// <inheritdoc />
    public async Task<MessageDecision> EvaluateAsync(IMessageIntent intent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var gridContext = gridContextAccessor.GridContext;
        var tenantIdText = GetTenantIdText(gridContext);

        using var activity = telemetryActivityFactory.Start(
            "communications.evaluate",
            new Dictionary<string, object?>
            {
                ["tenant_id"] = tenantIdText,
                ["communications.intent_kind"] = intent.IntentKind,
            });

        var recipient = await this.ResolveFirstRecipientAsync(intent, cancellationToken).ConfigureAwait(false);

        if (IsInternalTenant(tenantIdText))
        {
            return new MessageDecision(MessageDecisionOutcome.Sent, "internal-tenant-bypass", CorrelationKey: gridContext.CorrelationId);
        }

        var tenantId = new TenantId(tenantIdText);
        var preferences = await preferenceStore.GetAsync(tenantId, recipient, cancellationToken).ConfigureAwait(false);
        if (preferences.OptedOut || preferences.SuppressedIntentKinds.Contains(intent.IntentKind))
        {
            return new MessageDecision(MessageDecisionOutcome.SuppressedByPreference, "recipient-preference", CorrelationKey: gridContext.CorrelationId);
        }

        var cadence = await cadencePolicy.CheckAsync(tenantId, recipient, intent, cancellationToken).ConfigureAwait(false);
        if (cadence.Outcome is CadenceOutcome.Allow)
        {
            return new MessageDecision(MessageDecisionOutcome.Sent, cadence.Reason, CorrelationKey: gridContext.CorrelationId);
        }

        if (cadence.Outcome is CadenceOutcome.Defer)
        {
            return new MessageDecision(MessageDecisionOutcome.Scheduled, cadence.Reason, cadence.DeferUntil, gridContext.CorrelationId);
        }

        return new MessageDecision(MessageDecisionOutcome.SuppressedByCadence, cadence.Reason, CorrelationKey: gridContext.CorrelationId);
    }

    /// <inheritdoc />
    public async Task<MessageDecision> SendAsync(IMessageIntent intent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var gridContext = gridContextAccessor.GridContext;
        var tenantIdText = GetTenantIdText(gridContext);

        using var activity = telemetryActivityFactory.Start(
            "communications.send",
            new Dictionary<string, object?>
            {
                ["tenant_id"] = tenantIdText,
                ["communications.intent_kind"] = intent.IntentKind,
            });

        var decision = await this.EvaluateAsync(intent, cancellationToken).ConfigureAwait(false);
        if (decision.Outcome is MessageDecisionOutcome.Sent && !IsInternalTenant(tenantIdText))
        {
            var envelope = this.CreateEnvelope(intent, gridContext, tenantIdText);
            var outcome = await notificationSender.SendAsync(envelope, cancellationToken).ConfigureAwait(false);
            if (outcome.Status is not DeliveryStatus.Succeeded and not DeliveryStatus.Deferred)
            {
                decision = new MessageDecision(MessageDecisionOutcome.Failed, outcome.ErrorMessage ?? "notify-delivery-failed", CorrelationKey: gridContext.CorrelationId);
            }
            else if (intent is WelcomeEmailIntent welcomeEmailIntent)
            {
                this.ScheduleWelcomeFollowup(new TenantId(tenantIdText), welcomeEmailIntent, gridContext.CorrelationId);
            }
        }

        var recipient = await this.ResolveFirstRecipientAsync(intent, cancellationToken).ConfigureAwait(false);
        decisionLog.Append(new DecisionLogEntry(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            tenantIdText,
            intent.IntentKind,
            recipient,
            decision,
            gridContext.CorrelationId));

        return decision;
    }

    private static bool IsInternalTenant(string tenantId) =>
        string.Equals(tenantId, "internal", StringComparison.OrdinalIgnoreCase);

    private static string GetTenantIdText(IGridContext gridContext) =>
        string.IsNullOrWhiteSpace(gridContext.TenantId) ? "internal" : gridContext.TenantId;

    private async Task<RecipientHandle> ResolveFirstRecipientAsync(IMessageIntent intent, CancellationToken cancellationToken)
    {
        await foreach (var recipient in recipientResolver.ResolveAsync(intent, cancellationToken).ConfigureAwait(false))
        {
            return recipient;
        }

        return intent.Recipient;
    }

    private NotificationEnvelope CreateEnvelope(IMessageIntent intent, IGridContext gridContext, string tenantId)
    {
        var channel = string.Equals(intent.Recipient.PreferredChannel, "sms", StringComparison.OrdinalIgnoreCase)
            ? NotificationChannel.Sms
            : NotificationChannel.Email;

        return new NotificationEnvelope(
            NotificationId.NewId(),
            channel,
            new Recipient(channel, intent.Recipient.Identity),
            new TemplateKey(intent.IntentKind),
            intent.Payload.ToDictionary(pair => pair.Key, pair => (object?)pair.Value, StringComparer.Ordinal))
        {
            CorrelationId = gridContext.CorrelationId,
            CausationId = gridContext.CausationId,
            NodeId = gridContext.NodeId,
            TenantId = tenantId,
            Environment = gridContext.Environment,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Tags = ["communications", intent.IntentKind],
        };
    }

    private void ScheduleWelcomeFollowup(TenantId tenantId, WelcomeEmailIntent intent, string correlationId)
    {
        var followup = new WelcomeFollowupIntent(
            intent.Recipient,
            $"{intent.TriggerEventId}:followup",
            correlationId,
            intent.Payload);

        followupScheduler.Schedule(tenantId, followup, DateTimeOffset.UtcNow + options.CurrentValue.WelcomeFollowupDelay);
    }
}
