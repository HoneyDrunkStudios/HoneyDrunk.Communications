using AwesomeAssertions;
using HoneyDrunk.Communications.Abstractions;
using HoneyDrunk.Communications.Intents;
using HoneyDrunk.Communications.Internal;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Kernel.Abstractions.Telemetry;
using HoneyDrunk.Notify.Abstractions;
using Microsoft.Extensions.DependencyInjection;
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
        var gateway = new FakeNotificationGateway();
        var decisionLog = new InMemoryDecisionLog();
        var orchestrator = CreateOrchestrator(TenantId.Internal, gateway, decisionLog);
        var intent = new WelcomeEmailIntent(
            new RecipientHandle("user@example.com", "email"),
            "signup-1",
            new Dictionary<string, string>());

        var decision = await orchestrator.SendAsync(intent);

        decision.Outcome.Should().Be(MessageDecisionOutcome.Sent);
        gateway.Requests.Should().ContainSingle();
        decisionLog.Entries.Should().ContainSingle(entry => entry.TenantId == TenantId.Internal.ToString());
    }

    /// <summary>
    /// Verifies the internal tenant bypass is shared by cadence and preference stores.
    /// </summary>
    /// <returns>A task that completes when the test finishes.</returns>
    [Fact]
    public async Task Internal_tenant_bypass_is_shared_by_cadence_and_preferences()
    {
        var tenantId = TenantId.Internal;
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
    /// Verifies Communications runtime registration only requires Kernel abstractions and Notify contracts.
    /// </summary>
    [Fact]
    public void AddCommunications_registers_runtime_when_required_contracts_are_present()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGridContextAccessor>(new FakeGridContextAccessor(new FakeGridContext(TenantId.Internal)));
        services.AddSingleton<IOperationContextAccessor>(new FakeOperationContextAccessor());
        services.AddSingleton<ITelemetryActivityFactory, FakeTelemetryActivityFactory>();
        services.AddSingleton<INotificationGateway, FakeNotificationGateway>();

        services.AddCommunications(options => options.EnableHealthChecks = false);

        services.Should().Contain(service => service.ServiceType == typeof(ICommunicationOrchestrator));
    }

    /// <summary>
    /// Verifies AddCommunications fails fast when the Notify gateway boundary is missing.
    /// </summary>
    [Fact]
    public void AddCommunications_requires_notify_gateway_boundary()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGridContextAccessor>(new FakeGridContextAccessor(new FakeGridContext(TenantId.Internal)));
        services.AddSingleton<IOperationContextAccessor>(new FakeOperationContextAccessor());
        services.AddSingleton<ITelemetryActivityFactory, FakeTelemetryActivityFactory>();

        var act = () => services.AddCommunications();

        act.Should().Throw<InvalidOperationException>().WithMessage("*INotificationGateway*");
    }

    /// <summary>
    /// Verifies the Notify boundary receives the expected welcome-email request.
    /// </summary>
    /// <returns>A task that completes when the test finishes.</returns>
    [Fact]
    public async Task Welcome_email_is_sent_through_notify_boundary()
    {
        var tenantId = TenantId.NewId();
        var gridContextAccessor = new FakeGridContextAccessor(new FakeGridContext(tenantId));
        var gateway = new FakeNotificationGateway(gridContextAccessor: gridContextAccessor);
        var decisionLog = new InMemoryDecisionLog();
        var orchestrator = CreateOrchestrator(gridContextAccessor, gateway, decisionLog);
        var intent = new WelcomeEmailIntent(
            new RecipientHandle("user@example.com", "email"),
            "signup-1",
            new Dictionary<string, string> { ["displayName"] = "Oleg" });

        var decision = await orchestrator.SendAsync(intent);

        decision.Outcome.Should().Be(MessageDecisionOutcome.Sent);
        gateway.Requests.Should().ContainSingle();
        var request = gateway.Requests.Single();
        request.Channel.Should().Be(NotificationChannel.Email);
        request.Recipient.Address.Should().Be("user@example.com");
        request.TemplateKey.ToString().Should().Be(WelcomeEmailIntent.Kind);
        request.Model.Should().ContainKey("displayName").WhoseValue.Should().Be("Oleg");
        request.Tags.Should().Contain(WelcomeEmailIntent.Kind);
        request.IdempotencyKey.Should().NotBeNull();
        request.IdempotencyKey!.Value.Value.Should().StartWith("communications:");
        request.IdempotencyKey.Value.Value.Should().HaveLength(79);
        decisionLog.Entries.Should().ContainSingle(entry =>
            entry.TenantId == tenantId.ToString() && entry.CorrelationId == "corr-1");

        gateway.AcceptedEnvelopes.Should().ContainSingle();
        var envelope = gateway.AcceptedEnvelopes.Single();
        envelope.CorrelationId.Should().Be("corr-1");
        envelope.CausationId.Should().Be("cause-1");
        envelope.NodeId.Should().Be("honeydrunk-communications");
        envelope.Environment.Should().Be("test");
        envelope.TenantId.Should().Be(tenantId.ToString());
    }

    /// <summary>
    /// Verifies Notify intake rejections are surfaced as failed Communications decisions.
    /// </summary>
    /// <returns>A task that completes when the test finishes.</returns>
    [Fact]
    public async Task Notify_rejection_returns_failed_decision()
    {
        var gateway = new FakeNotificationGateway(NotificationOutcome.Rejected(
            NotificationId.NewId(),
            DateTimeOffset.UtcNow,
            RejectionReason.ValidationFailed,
            "template missing"));
        var decisionLog = new InMemoryDecisionLog();
        var orchestrator = CreateOrchestrator(TenantId.NewId(), gateway, decisionLog);
        var intent = new MessageIntent(
            "sms.alert",
            "event-1",
            new RecipientHandle("+15555550100", "sms"),
            new Dictionary<string, string> { ["message"] = "Heads up" });

        var decision = await orchestrator.SendAsync(intent);

        decision.Outcome.Should().Be(MessageDecisionOutcome.Failed);
        decision.Reason.Should().Be("template missing");
        gateway.Requests.Should().ContainSingle();
        var request = gateway.Requests.Single();
        request.Channel.Should().Be(NotificationChannel.Sms);
        request.Recipient.Address.Should().Be("+15555550100");
        request.Model.Should().ContainKey("message").WhoseValue.Should().Be("Heads up");
        decisionLog.Entries.Should().ContainSingle(entry => entry.Decision == decision);
    }

    /// <summary>
    /// Verifies repeated sends of the same intent carry the same Notify idempotency key.
    /// </summary>
    /// <returns>A task that completes when the test finishes.</returns>
    [Fact]
    public async Task Repeated_intent_uses_deterministic_notify_idempotency_key()
    {
        var gateway = new FakeNotificationGateway(rejectDuplicateIdempotencyKeys: true);
        var decisionLog = new InMemoryDecisionLog();
        var orchestrator = CreateOrchestrator(TenantId.Internal, gateway, decisionLog);
        var intent = new MessageIntent(
            "sms.alert",
            "event-1",
            new RecipientHandle("+15555550100", "sms"),
            new Dictionary<string, string> { ["message"] = "Heads up" });

        var first = await orchestrator.SendAsync(intent);
        var second = await orchestrator.SendAsync(intent);

        first.Outcome.Should().Be(MessageDecisionOutcome.Sent);
        second.Outcome.Should().Be(MessageDecisionOutcome.Failed);
        second.Reason.Should().Be("duplicate idempotency key");
        gateway.Requests.Should().HaveCount(2);
        var firstKey = gateway.Requests[0].IdempotencyKey;
        firstKey.Should().NotBeNull();
        firstKey.Should().Be(gateway.Requests[1].IdempotencyKey);
        firstKey!.Value.Value.Should().StartWith("communications:");
        firstKey.Value.Value.Should().HaveLength(79);
    }

    /// <summary>
    /// Verifies otherwise identical sends from different tenants do not collide in Notify dedupe.
    /// </summary>
    /// <returns>A task that completes when the test finishes.</returns>
    [Fact]
    public async Task Idempotency_key_is_scoped_by_tenant()
    {
        var gateway = new FakeNotificationGateway(rejectDuplicateIdempotencyKeys: true);
        var firstTenantLog = new InMemoryDecisionLog();
        var secondTenantLog = new InMemoryDecisionLog();
        var firstOrchestrator = CreateOrchestrator(TenantId.NewId(), gateway, firstTenantLog);
        var secondOrchestrator = CreateOrchestrator(TenantId.NewId(), gateway, secondTenantLog);
        var intent = new MessageIntent(
            "sms.alert",
            "event-1",
            new RecipientHandle("+15555550100", "sms"),
            new Dictionary<string, string> { ["message"] = "Heads up" });

        var first = await firstOrchestrator.SendAsync(intent);
        var second = await secondOrchestrator.SendAsync(intent);

        first.Outcome.Should().Be(MessageDecisionOutcome.Sent);
        second.Outcome.Should().Be(MessageDecisionOutcome.Sent);
        gateway.Requests.Should().HaveCount(2);
        gateway.Requests[0].IdempotencyKey.Should().NotBe(gateway.Requests[1].IdempotencyKey);
        gateway.Requests[0].IdempotencyKey!.Value.Value.Should().StartWith("communications:");
        gateway.Requests[1].IdempotencyKey!.Value.Value.Should().StartWith("communications:");
    }

    private static CommunicationOrchestrator CreateOrchestrator(
        TenantId tenantId,
        FakeNotificationGateway gateway,
        InMemoryDecisionLog decisionLog) =>
        CreateOrchestrator(new FakeGridContextAccessor(new FakeGridContext(tenantId)), gateway, decisionLog);

    private static CommunicationOrchestrator CreateOrchestrator(
        FakeGridContextAccessor gridContextAccessor,
        FakeNotificationGateway gateway,
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
            gateway,
            decisionLog,
            gridContextAccessor,
            new FakeTelemetryActivityFactory(),
            scheduler,
            options);
    }

    private sealed class FakeNotificationGateway(
        NotificationOutcome? outcome = null,
        IGridContextAccessor? gridContextAccessor = null,
        bool rejectDuplicateIdempotencyKeys = false) : INotificationGateway
    {
        private readonly HashSet<IdempotencyKey> acceptedKeys = [];

        public List<NotificationRequest> Requests { get; } = [];

        public List<NotificationEnvelope> AcceptedEnvelopes { get; } = [];

        public Task<NotificationOutcome> EnqueueAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        {
            this.Requests.Add(request);
            if (outcome is not null)
            {
                return Task.FromResult(outcome);
            }

            var acceptedAt = DateTimeOffset.UtcNow;
            var notificationId = NotificationId.NewId();
            if (rejectDuplicateIdempotencyKeys &&
                request.IdempotencyKey is IdempotencyKey idempotencyKey &&
                !this.acceptedKeys.Add(idempotencyKey))
            {
                return Task.FromResult(NotificationOutcome.Rejected(
                    notificationId,
                    acceptedAt,
                    RejectionReason.DuplicateIdempotencyKey,
                    "duplicate idempotency key"));
            }

            this.AcceptedEnvelopes.Add(CreateEnvelope(notificationId, acceptedAt, request));
            return Task.FromResult(NotificationOutcome.Accepted(notificationId, acceptedAt));
        }

        private NotificationEnvelope CreateEnvelope(
            NotificationId notificationId,
            DateTimeOffset acceptedAt,
            NotificationRequest request)
        {
            var gridContext = gridContextAccessor?.GridContext;
            return new NotificationEnvelope(
                notificationId,
                request.Channel,
                request.Recipient,
                request.TemplateKey,
                request.Model)
            {
                CorrelationId = gridContext?.CorrelationId,
                CausationId = gridContext?.CausationId,
                NodeId = gridContext?.NodeId,
                TenantId = gridContext?.TenantId.ToString(),
                Environment = gridContext?.Environment,
                Priority = request.Priority,
                Tags = request.Tags,
                IdempotencyKey = request.IdempotencyKey,
                CreatedAtUtc = acceptedAt,
            };
        }
    }

    private sealed class FakeGridContextAccessor(IGridContext gridContext) : IGridContextAccessor
    {
        public IGridContext GridContext { get; } = gridContext;
    }

    private sealed class FakeOperationContextAccessor : IOperationContextAccessor
    {
        public IOperationContext? Current { get; set; }
    }

    private sealed class FakeGridContext(TenantId tenantId) : IGridContext
    {
        public bool IsInitialized => true;

        public string CorrelationId { get; } = "corr-1";

        public string? CausationId { get; } = "cause-1";

        public string NodeId { get; } = "honeydrunk-communications";

        public string StudioId { get; } = "honeydrunk";

        public string Environment { get; } = "test";

        public TenantId TenantId { get; } = tenantId;

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
