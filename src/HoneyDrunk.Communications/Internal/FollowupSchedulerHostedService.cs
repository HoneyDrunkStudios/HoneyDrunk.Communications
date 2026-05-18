using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace HoneyDrunk.Communications.Internal;

/// <summary>
/// Hosted-service adapter that runs the singleton in-memory follow-up scheduler.
/// </summary>
/// <param name="scheduler">Scheduler instance owned by the Communications runtime registration.</param>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Microsoft.Extensions.DependencyInjection.")]
internal sealed class FollowupSchedulerHostedService(InMemoryFollowupScheduler scheduler) : IHostedService
{
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => scheduler.StartAsync(cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => scheduler.StopAsync(cancellationToken);
}
