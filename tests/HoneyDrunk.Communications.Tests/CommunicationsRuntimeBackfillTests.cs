using FluentAssertions;
using HoneyDrunk.Communications.Abstractions;
using HoneyDrunk.Communications.Intents;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Health;
using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Kernel.Abstractions.Telemetry;
using HoneyDrunk.Notify.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HoneyDrunk.Communications.Tests;

/// <summary>
/// Runtime-focused coverage tests for Communications wiring and hosted follow-up behavior.
/// </summary>
public sealed class CommunicationsRuntimeBackfillTests
{
    /// <summary>
    /// Verifies health and startup hooks are registered and reflect scheduler lifecycle state.
    /// </summary>
    /// <returns>A task that completes when the test finishes.</returns>
    [Fact]
    public async Task Runtime_wiring_exposes_startup_and_health_lifecycle()
    {
        await using var provider = BuildProvider(
            configure: options =>
            {
                options.EnableHealthChecks = true;
                options.FollowupSchedulerInterval = TimeSpan.FromMilliseconds(250);
            });

        var startupHook = provider.GetRequiredService<IEnumerable<IStartupHook>>().Should().ContainSingle().Subject;
        await startupHook.ExecuteAsync(CancellationToken.None);

        var healthContributor = provider.GetRequiredService<IEnumerable<IHealthContributor>>().Should().ContainSingle().Subject;
        healthContributor.Name.Should().Be("communications");
        healthContributor.Priority.Should().Be(0);
        healthContributor.IsCritical.Should().BeFalse();

        var stoppedHealth = await healthContributor.CheckHealthAsync();
        stoppedHealth.status.Should().Be(HealthStatus.Degraded);
        stoppedHealth.message.Should().Be("Follow-up scheduler has not started.");

        var hostedService = GetFollowupSchedulerHostedService(provider);
        await hostedService.StartAsync(CancellationToken.None);
        try
        {
            (HealthStatus status, string? message) runningHealth = default;
            await WaitForAsync(async () =>
            {
                runningHealth = await healthContributor.CheckHealthAsync();
                return runningHealth.status is HealthStatus.Healthy;
            });

            runningHealth.status.Should().Be(HealthStatus.Healthy);
            runningHealth.message.Should().Be("Communications runtime is healthy.");
        }
        finally
        {
            await hostedService.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// Verifies the hosted scheduler dispatches due welcome follow-up intents without re-scheduling loops.
    /// </summary>
    /// <returns>A task that completes when the test finishes.</returns>
    [Fact]
    public async Task Hosted_scheduler_dispatches_due_welcome_followup_once()
    {
        var sender = new RecordingNotificationSender();
        await using var provider = BuildProvider(
            sender,
            options =>
            {
                options.WelcomeFollowupDelay = TimeSpan.Zero;
                options.FollowupSchedulerInterval = TimeSpan.FromMilliseconds(10);
            });

        var hostedService = GetFollowupSchedulerHostedService(provider);
        await hostedService.StartAsync(CancellationToken.None);
        try
        {
            var orchestrator = provider.GetRequiredService<ICommunicationOrchestrator>();
            var decision = await orchestrator.SendAsync(new WelcomeEmailIntent(
                new RecipientHandle("user@example.com", "email"),
                "signup-1",
                new Dictionary<string, string> { ["displayName"] = "Oleg" }));

            decision.Outcome.Should().Be(MessageDecisionOutcome.Sent);
            await WaitForAsync(() => sender.Envelopes.Count >= 2);

            sender.Envelopes.Should().HaveCount(2);
            sender.Envelopes.Select(envelope => envelope.TemplateKey.ToString()).Should().Equal(
                WelcomeEmailIntent.Kind,
                WelcomeFollowupIntent.Kind);
        }
        finally
        {
            await hostedService.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// Verifies public contract records preserve tenant, recipient, decision, and payload values.
    /// </summary>
    [Fact]
    public void Communication_contract_records_preserve_decision_context()
    {
        var tenantId = TenantId.NewId();
        var recipient = new RecipientHandle("user@example.com", "sms");
        var decision = new MessageDecision(
            MessageDecisionOutcome.Scheduled,
            "quiet-hours",
            DateTimeOffset.Parse("2026-05-20T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            "corr-1");
        var intent = new MessageIntent(
            "custom-intent",
            "event-1",
            recipient,
            new Dictionary<string, string> { ["name"] = "Oleg" });

        var entry = new CommunicationDecisionLogEntry(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DateTimeOffset.Parse("2026-05-19T20:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            tenantId,
            intent.IntentKind,
            intent.Recipient,
            decision,
            "corr-1");

        intent.TriggerEventId.Should().Be("event-1");
        intent.Payload.Should().ContainKey("name").WhoseValue.Should().Be("Oleg");
        entry.TenantId.Should().Be(tenantId);
        entry.Recipient.PreferredChannel.Should().Be("sms");
        entry.Decision.Should().Be(decision);
        entry.CorrelationId.Should().Be("corr-1");
    }

    private static IHostedService GetFollowupSchedulerHostedService(IServiceProvider provider)
    {
        return provider.GetRequiredService<IEnumerable<IHostedService>>()
            .Should()
            .ContainSingle(service => service.GetType().Name == "FollowupSchedulerHostedService")
            .Subject;
    }

    private static ServiceProvider BuildProvider(
        RecordingNotificationSender? sender = null,
        Action<CommunicationsOptions>? configure = null)
    {
        sender ??= new RecordingNotificationSender();

        var services = new ServiceCollection();
        services.AddSingleton<IGridContextAccessor>(new FakeGridContextAccessor(new FakeGridContext(TenantId.NewId())));
        services.AddSingleton<IOperationContextAccessor>(new FakeOperationContextAccessor());
        services.AddSingleton<ITelemetryActivityFactory>(new FakeTelemetryActivityFactory());
        services.AddSingleton<INotificationSender>(sender);
        services.AddCommunications(configure);

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static Task WaitForAsync(Func<bool> condition) =>
        WaitForAsync(() => Task.FromResult(condition()));

    private static async Task WaitForAsync(Func<Task<bool>> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!await condition().ConfigureAwait(false))
        {
            timeout.Token.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromMilliseconds(25), timeout.Token).ConfigureAwait(false);
        }
    }

    private sealed class RecordingNotificationSender : INotificationSender
    {
        private readonly Lock gate = new();
        private readonly List<NotificationEnvelope> envelopes = [];

        public IReadOnlyList<NotificationEnvelope> Envelopes
        {
            get
            {
                lock (this.gate)
                {
                    return this.envelopes.ToArray();
                }
            }
        }

        public Task<DeliveryOutcome> SendAsync(NotificationEnvelope envelope, CancellationToken cancellationToken = default)
        {
            lock (this.gate)
            {
                this.envelopes.Add(envelope);
            }

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
}
