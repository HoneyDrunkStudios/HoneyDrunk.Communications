using HoneyDrunk.Communications.Abstractions;
using HoneyDrunk.Communications.Intents;
using HoneyDrunk.Kernel.Abstractions.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Communications.Internal;

/// <summary>
/// In-process scheduler for non-durable welcome follow-up intents.
/// </summary>
/// <param name="options">Communications options.</param>
public sealed class InMemoryFollowupScheduler(IOptionsMonitor<CommunicationsOptions> options) : BackgroundService
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<ScheduledFollowup> followups = [];

    /// <summary>
    /// Gets a value indicating whether the scheduler loop has started.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets the largest current due follow-up lag.
    /// </summary>
    public TimeSpan MaxLag => this.followups.TryPeek(out var next) && next.ScheduledFor < DateTimeOffset.UtcNow
        ? DateTimeOffset.UtcNow - next.ScheduledFor
        : TimeSpan.Zero;

    internal IReadOnlyCollection<ScheduledFollowup> PendingFollowups => this.followups.ToArray();

    internal void Schedule(TenantId tenantId, WelcomeFollowupIntent intent, DateTimeOffset scheduledFor) =>
        this.followups.Enqueue(new ScheduledFollowup(tenantId, intent.Recipient, intent, scheduledFor));

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.IsRunning = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(options.CurrentValue.FollowupSchedulerInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    internal sealed record ScheduledFollowup(
        TenantId TenantId,
        RecipientHandle Recipient,
        WelcomeFollowupIntent Intent,
        DateTimeOffset ScheduledFor);
}
