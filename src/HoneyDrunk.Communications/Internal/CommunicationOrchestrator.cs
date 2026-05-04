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
public sealed class CommunicationOrchestrator : ICommunicationOrchestrator
{
    private static readonly TenantId InternalTenantId = new("00000000000000000000000000");

    private readonly IRecipientResolver recipientResolver;
    private readonly IPreferenceStore preferenceStore;
    private readonly ICadencePolicy cadencePolicy;
    private readonly INotificationSender notificationSender;
    private readonly IDecisionLog decisionLog;
    private readonly IGridContextAccessor gridContextAccessor;
    private readonly ITelemetryActivityFactory telemetryActivityFactory;
    private readonly InMemoryFollowupScheduler followupScheduler;
    private readonly IOptionsMonitor<CommunicationsOptions> options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommunicationOrchestrator"/> class.
    /// </summary>
    /// <param name="recipientResolver">Recipient resolver.</param>
    /// <param name="preferenceStore">Preference store.</param>
    /// <param name="cadencePolicy">Cadence policy.</param>
    /// <param name="notificationSender">Notify sender boundary.</param>
    /// <param name="decisionLog">Decision log.</param>
    /// <param name="gridContextAccessor">Grid context accessor.</param>
    /// <param name="telemetryActivityFactory">Telemetry activity factory.</param>
    /// <param name="followupScheduler">Follow-up scheduler.</param>
    /// <param name="options">Communications options.</param>
    public CommunicationOrchestrator(
        IRecipientResolver recipientResolver,
        IPreferenceStore preferenceStore,
        ICadencePolicy cadencePolicy,
        INotificationSender notificationSender,
        IDecisionLog decisionLog,
        IGridContextAccessor gridContextAccessor,
        ITelemetryActivityFactory telemetryActivityFactory,
        InMemoryFollowupScheduler followupScheduler,
        IOptionsMonitor<CommunicationsOptions> options)
    {
        this.recipientResolver = recipientResolver;
        this.preferenceStore = preferenceStore;
        this.cadencePolicy = cadencePolicy;
        this.notificationSender = notificationSender;
        this.decisionLog = decisionLog;
        this.gridContextAccessor = gridContextAccessor;
        this.telemetryActivityFactory = telemetryActivityFactory;
        this.followupScheduler = followupScheduler;
        this.options = options;
        this.followupScheduler.ConfigureDispatcher(this.DispatchScheduledFollowupAsync);
    }

    /// <inheritdoc />
    public async Task<MessageDecision> EvaluateAsync(IMessageIntent intent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var gridContext = this.gridContextAccessor.GridContext;
        var tenantId = GetTenantId(gridContext);

        using var activity = this.telemetryActivityFactory.Start(
            "communications.evaluate",
            new Dictionary<string, object?>
            {
                ["tenant_id"] = tenantId.ToString(),
                ["communications.intent_kind"] = intent.IntentKind,
            });

        var recipient = await this.ResolveRecipientsAsync(intent, cancellationToken).ConfigureAwait(false);
        return await this.EvaluateRecipientAsync(intent, recipient[0], tenantId, gridContext.CorrelationId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<MessageDecision> SendAsync(IMessageIntent intent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var gridContext = this.gridContextAccessor.GridContext;
        var tenantId = GetTenantId(gridContext);

        using var activity = this.telemetryActivityFactory.Start(
            "communications.send",
            new Dictionary<string, object?>
            {
                ["tenant_id"] = tenantId.ToString(),
                ["communications.intent_kind"] = intent.IntentKind,
            });

        return await this.SendCoreAsync(intent, tenantId, gridContext, scheduleFollowup: true, cancellationToken).ConfigureAwait(false);
    }

    private static TenantId GetTenantId(IGridContext gridContext)
    {
        var tenantIdText = gridContext.TenantId?.ToString();
        if (string.IsNullOrWhiteSpace(tenantIdText) || string.Equals(tenantIdText, "internal", StringComparison.OrdinalIgnoreCase))
        {
            return InternalTenantId;
        }

        return TenantId.TryParse(tenantIdText, out var tenantId) ? tenantId : InternalTenantId;
    }

    private async Task<MessageDecision> SendCoreAsync(
        IMessageIntent intent,
        TenantId tenantId,
        IGridContext gridContext,
        bool scheduleFollowup,
        CancellationToken cancellationToken)
    {
        var recipients = await this.ResolveRecipientsAsync(intent, cancellationToken).ConfigureAwait(false);
        MessageDecision? result = null;

        foreach (var recipient in recipients)
        {
            var decision = await this.EvaluateRecipientAsync(intent, recipient, tenantId, gridContext.CorrelationId, cancellationToken).ConfigureAwait(false);
            if (decision.Outcome is MessageDecisionOutcome.Allowed)
            {
                var envelope = this.CreateEnvelope(intent, recipient, gridContext, tenantId);
                var outcome = await this.notificationSender.SendAsync(envelope, cancellationToken).ConfigureAwait(false);
                decision = outcome.Status is DeliveryStatus.Succeeded or DeliveryStatus.Deferred
                    ? new MessageDecision(MessageDecisionOutcome.Sent, outcome.Status.ToString(), CorrelationKey: gridContext.CorrelationId)
                    : new MessageDecision(MessageDecisionOutcome.Failed, outcome.ErrorMessage ?? "notify-delivery-failed", CorrelationKey: gridContext.CorrelationId);

                if (decision.Outcome is MessageDecisionOutcome.Sent && scheduleFollowup && intent is WelcomeEmailIntent welcomeEmailIntent)
                {
                    this.ScheduleWelcomeFollowup(tenantId, welcomeEmailIntent, recipient, gridContext.CorrelationId);
                }
            }

            this.decisionLog.Append(new DecisionLogEntry(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                tenantId.ToString(),
                intent.IntentKind,
                recipient,
                decision,
                gridContext.CorrelationId));

            result ??= decision;
            if (decision.Outcome is MessageDecisionOutcome.Failed)
            {
                result = decision;
            }
        }

        return result ?? new MessageDecision(MessageDecisionOutcome.Failed, "no-recipient", CorrelationKey: gridContext.CorrelationId);
    }

    private async Task<MessageDecision> EvaluateRecipientAsync(
        IMessageIntent intent,
        RecipientHandle recipient,
        TenantId tenantId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var preferences = await this.preferenceStore.GetAsync(tenantId, recipient, cancellationToken).ConfigureAwait(false);
        if (preferences.OptedOut || preferences.SuppressedIntentKinds.Contains(intent.IntentKind))
        {
            return new MessageDecision(MessageDecisionOutcome.SuppressedByPreference, "recipient-preference", CorrelationKey: correlationId);
        }

        var cadence = await this.cadencePolicy.CheckAsync(tenantId, recipient, intent, cancellationToken).ConfigureAwait(false);
        if (cadence.Outcome is CadenceOutcome.Allow)
        {
            return new MessageDecision(MessageDecisionOutcome.Allowed, cadence.Reason, CorrelationKey: correlationId);
        }

        if (cadence.Outcome is CadenceOutcome.Defer)
        {
            return new MessageDecision(MessageDecisionOutcome.Scheduled, cadence.Reason, cadence.DeferUntil, correlationId);
        }

        return new MessageDecision(MessageDecisionOutcome.SuppressedByCadence, cadence.Reason, CorrelationKey: correlationId);
    }

    private async Task<IReadOnlyList<RecipientHandle>> ResolveRecipientsAsync(IMessageIntent intent, CancellationToken cancellationToken)
    {
        var recipients = new List<RecipientHandle>();
        await foreach (var recipient in this.recipientResolver.ResolveAsync(intent, cancellationToken).ConfigureAwait(false))
        {
            recipients.Add(recipient);
        }

        if (recipients.Count == 0)
        {
            recipients.Add(intent.Recipient);
        }

        return recipients;
    }

    private NotificationEnvelope CreateEnvelope(IMessageIntent intent, RecipientHandle recipient, IGridContext gridContext, TenantId tenantId)
    {
        var channel = string.Equals(recipient.PreferredChannel, "sms", StringComparison.OrdinalIgnoreCase)
            ? NotificationChannel.Sms
            : NotificationChannel.Email;

        return new NotificationEnvelope(
            NotificationId.NewId(),
            channel,
            new Recipient(channel, recipient.Identity),
            new TemplateKey(intent.IntentKind),
            intent.Payload.ToDictionary(pair => pair.Key, pair => (object?)pair.Value, StringComparer.Ordinal))
        {
            CorrelationId = gridContext.CorrelationId,
            CausationId = gridContext.CausationId,
            NodeId = gridContext.NodeId,
            TenantId = tenantId.ToString(),
            Environment = gridContext.Environment,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Tags = ["communications", intent.IntentKind],
        };
    }

    private void ScheduleWelcomeFollowup(TenantId tenantId, WelcomeEmailIntent intent, RecipientHandle recipient, string correlationId)
    {
        var followup = new WelcomeFollowupIntent(
            recipient,
            $"{intent.TriggerEventId}:followup",
            correlationId,
            intent.Payload);

        this.followupScheduler.Schedule(tenantId, followup, DateTimeOffset.UtcNow + this.options.CurrentValue.WelcomeFollowupDelay);
    }

    private Task DispatchScheduledFollowupAsync(InMemoryFollowupScheduler.ScheduledFollowup followup, CancellationToken cancellationToken)
    {
        var gridContext = this.gridContextAccessor.GridContext;
        return this.SendCoreAsync(followup.Intent, followup.TenantId, gridContext, scheduleFollowup: false, cancellationToken);
    }
}
