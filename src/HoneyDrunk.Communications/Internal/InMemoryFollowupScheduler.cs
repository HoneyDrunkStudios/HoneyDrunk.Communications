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
    private readonly Lock gate = new();
    private readonly List<ScheduledFollowup> followups = [];
    private Func<ScheduledFollowup, CancellationToken, Task>? dispatcher;

    /// <summary>
    /// Gets a value indicating whether the scheduler loop has started.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets the largest current due follow-up lag.
    /// </summary>
    public TimeSpan MaxLag
    {
        get
        {
            var now = DateTimeOffset.UtcNow;
            lock (this.gate)
            {
                return this.followups
                    .Where(followup => followup.ScheduledFor < now)
                    .Select(followup => now - followup.ScheduledFor)
                    .DefaultIfEmpty(TimeSpan.Zero)
                    .Max();
            }
        }
    }

    internal IReadOnlyCollection<ScheduledFollowup> PendingFollowups
    {
        get
        {
            lock (this.gate)
            {
                return this.followups.ToArray();
            }
        }
    }

    internal void ConfigureDispatcher(Func<ScheduledFollowup, CancellationToken, Task> dispatch) =>
        this.dispatcher = dispatch;

    internal void Schedule(TenantId tenantId, WelcomeFollowupIntent intent, DateTimeOffset scheduledFor)
    {
        lock (this.gate)
        {
            this.followups.Add(new ScheduledFollowup(tenantId, intent.Recipient, intent, scheduledFor));
        }
    }

    internal async Task DispatchDueAsync(CancellationToken cancellationToken = default)
    {
        var dispatcherSnapshot = this.dispatcher;
        if (dispatcherSnapshot is null)
        {
            return;
        }

        foreach (var followup in this.GetDueFollowups(DateTimeOffset.UtcNow))
        {
            await dispatcherSnapshot(followup, cancellationToken).ConfigureAwait(false);

            lock (this.gate)
            {
                this.followups.Remove(followup);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.IsRunning = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            await this.DispatchDueAsync(stoppingToken).ConfigureAwait(false);
            await Task.Delay(options.CurrentValue.FollowupSchedulerInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private IReadOnlyList<ScheduledFollowup> GetDueFollowups(DateTimeOffset now)
    {
        lock (this.gate)
        {
            return this.followups
                .Where(followup => followup.ScheduledFor <= now)
                .OrderBy(followup => followup.ScheduledFor)
                .ToArray();
        }
    }

    internal sealed record ScheduledFollowup(
        TenantId TenantId,
        RecipientHandle Recipient,
        WelcomeFollowupIntent Intent,
        DateTimeOffset ScheduledFor);
}
