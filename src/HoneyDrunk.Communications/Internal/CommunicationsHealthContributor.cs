using HoneyDrunk.Kernel.Abstractions.Health;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Communications.Internal;

/// <summary>
/// Health contributor for Communications runtime wiring and the non-durable follow-up scheduler.
/// </summary>
/// <param name="scheduler">The in-memory follow-up scheduler.</param>
/// <param name="options">Communications options.</param>
public sealed class CommunicationsHealthContributor(
    InMemoryFollowupScheduler scheduler,
    IOptionsMonitor<CommunicationsOptions> options) : IHealthContributor
{
    /// <summary>
    /// Gets the health contributor name.
    /// </summary>
    public string Name => "communications";

    /// <summary>
    /// Gets the contributor priority.
    /// </summary>
    public int Priority => 0;

    /// <summary>
    /// Gets a value indicating whether this health contributor is critical.
    /// </summary>
    public bool IsCritical => false;

#pragma warning disable SA1316 // Tuple element names mirror the Kernel interface contract.
    /// <summary>
    /// Checks Communications runtime health.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The health status and optional diagnostic message.</returns>
    public Task<(HealthStatus status, string? message)> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        if (!scheduler.IsRunning)
        {
            return Task.FromResult<(HealthStatus status, string? message)>((HealthStatus.Degraded, "Follow-up scheduler has not started."));
        }

        var maxLag = scheduler.MaxLag;
        if (maxLag > options.CurrentValue.FollowupSchedulerInterval * 2)
        {
            return Task.FromResult<(HealthStatus status, string? message)>((HealthStatus.Degraded, "Follow-up scheduler is behind."));
        }

        return Task.FromResult<(HealthStatus status, string? message)>((HealthStatus.Healthy, "Communications runtime is healthy."));
    }
#pragma warning restore SA1316
}
