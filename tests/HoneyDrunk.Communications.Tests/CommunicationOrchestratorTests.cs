using FluentAssertions;
using HoneyDrunk.Communications.Abstractions;
using HoneyDrunk.Communications.Intents;
using HoneyDrunk.Communications.Internal;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Kernel.Abstractions.Telemetry;
using HoneyDrunk.Notify.Abstractions;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Communications.Tests;

/// <summary>
/// Tests for <see cref="CommunicationOrchestrator"/>.
/// </summary>
public sealed class CommunicationOrchestratorTests
{
    /// <summary>
    /// Verifies cadence isolation across tenant boundaries.
    /// </summary>
    /// <returns>A task that completes when the test finishes.</returns>
    [Fact]
    public async Task Cadence_policy_is_tenant_scoped()
    {
        var policy = new InMemoryCadencePolicy();
        var recipient = new RecipientHandle("user@example.com", "email");
        var intent = new WelcomeEmailIntent(recipient, "signup-1", new Dictionary<string, string>());
        var firstTenant = TenantId.NewId();
        var secondTenant = TenantId.NewId();

        var firstVerdict = await policy.CheckAsync(firstTenant, recipient, intent);
        var duplicateVerdict = await policy.CheckAsync(firstTenant, recipient, intent);
        var secondTenantVerdict = await policy.CheckAsync(secondTenant, recipient, intent);

        firstVerdict.Outcome.Should().Be(CadenceOutcome.Allow);
        duplicateVerdict.Outcome.Should().Be(CadenceOutcome.Suppress);
        secondTenantVerdict.Outcome.Should().Be(CadenceOutcome.Allow);
    }

    /// <summary>
    /// Verifies internal tenant handling bypasses preference and cadence enforcement while still exercising Notify delivery.
    /// </summary>
    /// <returns>A task that completes when the test finishes.</returns>
    [Fact]
    public async Task Internal_tenant_bypasses_enforcement_but_still_delivers()
    {
        var sender = new FakeNotificationSender();
        var decisionLog = new InMemoryDecisionLog();
        var orchestrator = CreateOrchestrator("internal", sender, decisionLog);
        var intent = new WelcomeEmailIntent(
            new RecipientHandle("user@example.com", "email"),
            "signup-1",
            new Dictionary<string, string>());

        var decision = await orchestrator.SendAsync(intent);

        decision.Outcome.Should().Be(MessageDecisionOutcome.Sent);
        sender.Envelopes.Should().ContainSingle();
        decisionLog.Entries.Should().ContainSingle(entry => entry.TenantId == "00000000000000000000000000");
    }

    /// <summary>
    /// Verifies the internal tenant bypass is shared by cadence and preference stores.
    /// </summary>
    /// <returns>A task that completes when the test finishes.</returns>
    [Fact]
    public async Task Internal_tenant_bypass_is_shared_by_cadence_and_preferences()
    {
        var tenantId = new TenantId("00000000000000000000000000");
        var recipient = new RecipientHandle("user@example.com", "email");
        var intent = new WelcomeEmailIntent(recipient, "signup-1", new Dictionary<string, string>());
        var cadencePolicy = new InMemoryCadencePolicy();
        var preferenceStore = new InMemoryPreferenceStore();

        await preferenceStore.SetAsync(
            tenantId,
            recipient,
            new RecipientPreferences(
                OptedOut: true,
                SuppressedIntentKinds: new HashSet<string>(StringComparer.Ordinal) { intent.IntentKind },
                QuietHoursStart: null,
                QuietHoursEnd: null,
                PreferredChannel: "sms"));

        var preferences = await preferenceStore.GetAsync(tenantId, recipient);
        var firstCadence = await cadencePolicy.CheckAsync(tenantId, recipient, intent);
        var duplicateCadence = await cadencePolicy.CheckAsync(tenantId, recipient, intent);

        preferences.OptedOut.Should().BeFalse();
        preferences.SuppressedIntentKinds.Should().BeEmpty();
        preferences.PreferredChannel.Should().BeNull();
        firstCadence.Outcome.Should().Be(CadenceOutcome.Allow);
        firstCadence.Reason.Should().Be("internal-tenant-bypass");
        duplicateCadence.Outcome.Should().Be(CadenceOutcome.Allow);
        duplicateCadence.Reason.Should().Be("internal-tenant-bypass");
    }

    /// <summary>
    /// Verifies the Notify boundary receives the expected welcome-email envelope.
    /// </summary>
    /// <returns>A task that completes when the test finishes.</returns>
    [Fact]
    public async Task Welcome_email_is_sent_through_notify_boundary()
    {
        var tenantId = TenantId.NewId().ToString();
        var sender = new FakeNotificationSender();
        var decisionLog = new InMemoryDecisionLog();
        var orchestrator = CreateOrchestrator(tenantId, sender, decisionLog);
        var intent = new WelcomeEmailIntent(
            new RecipientHandle("user@example.com", "email"),
            "signup-1",
            new Dictionary<string, string> { ["displayName"] = "Oleg" });

        var decision = await orchestrator.SendAsync(intent);

        decision.Outcome.Should().Be(MessageDecisionOutcome.Sent);
        sender.Envelopes.Should().ContainSingle();
        var envelope = sender.Envelopes.Single();
        envelope.Channel.Should().Be(NotificationChannel.Email);
        envelope.Recipient.Address.Should().Be("user@example.com");
        envelope.TemplateKey.ToString().Should().Be(WelcomeEmailIntent.Kind);
        envelope.TenantId.Should().Be(tenantId);
        envelope.Tags.Should().Contain(WelcomeEmailIntent.Kind);
        decisionLog.Entries.Should().ContainSingle(entry => entry.TenantId == tenantId);
    }

    private static CommunicationOrchestrator CreateOrchestrator(
        string tenantId,
        FakeNotificationSender sender,
        InMemoryDecisionLog decisionLog)
    {
        var options = new FakeOptionsMonitor(new CommunicationsOptions { WelcomeFollowupDelay = TimeSpan.FromDays(2) });
#pragma warning disable CA2000 // Owned by the orchestrator/service provider in production; test lifetime is process-bound.
        var scheduler = new InMemoryFollowupScheduler(options);
#pragma warning restore CA2000

        return new CommunicationOrchestrator(
            new DefaultRecipientResolver(),
            new InMemoryPreferenceStore(),
            new InMemoryCadencePolicy(),
            sender,
            decisionLog,
            new FakeGridContextAccessor(new FakeGridContext(tenantId)),
            new FakeTelemetryActivityFactory(),
            scheduler,
            options);
    }

    private sealed class FakeNotificationSender : INotificationSender
    {
        public List<NotificationEnvelope> Envelopes { get; } = [];

        public Task<DeliveryOutcome> SendAsync(NotificationEnvelope envelope, CancellationToken cancellationToken = default)
        {
            this.Envelopes.Add(envelope);
            return Task.FromResult(DeliveryOutcome.Succeeded(
                envelope.NotificationId,
                AttemptId.NewId(),
                envelope.Channel,
                "fake"));
        }
    }

    private sealed class FakeGridContextAccessor(IGridContext gridContext) : IGridContextAccessor
    {
        public IGridContext GridContext { get; } = gridContext;
    }

    private sealed class FakeGridContext(string tenantId) : IGridContext
    {
        public bool IsInitialized => true;

        public string CorrelationId { get; } = "corr-1";

        public string? CausationId { get; } = "cause-1";

        public string NodeId { get; } = "communications";

        public string StudioId { get; } = "honeydrunk";

        public string Environment { get; } = "test";

        public string? TenantId { get; } = tenantId;

        public string? ProjectId { get; }

        public CancellationToken Cancellation { get; } = CancellationToken.None;

        public IReadOnlyDictionary<string, string> Baggage { get; } = new Dictionary<string, string>();

        public DateTimeOffset CreatedAtUtc { get; } = DateTimeOffset.UtcNow;

        public void AddBaggage(string key, string value)
        {
        }
    }

    private sealed class FakeTelemetryActivityFactory : ITelemetryActivityFactory
    {
        public System.Diagnostics.Activity? Start(string name, IReadOnlyDictionary<string, object?>? additionalTags = null) => null;

        public System.Diagnostics.Activity? StartExplicit(
            string name,
            IGridContext gridContext,
            IOperationContext? operationContext = null,
            IReadOnlyDictionary<string, object?>? additionalTags = null) => null;
    }

    private sealed class FakeOptionsMonitor(CommunicationsOptions options) : IOptionsMonitor<CommunicationsOptions>
    {
        public CommunicationsOptions CurrentValue { get; } = options;

        public CommunicationsOptions Get(string? name) => this.CurrentValue;

        public IDisposable? OnChange(Action<CommunicationsOptions, string?> listener) => null;
    }
}
