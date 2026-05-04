using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Notify.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Communications.Internal;

/// <summary>
/// Startup hook that validates Communications runtime dependencies.
/// </summary>
/// <param name="serviceProvider">The service provider.</param>
#pragma warning disable CA1812 // Registered through Microsoft.Extensions.DependencyInjection.
internal sealed class CommunicationsStartupHook(IServiceProvider serviceProvider) : IStartupHook
{
    /// <summary>
    /// Gets the startup hook priority.
    /// </summary>
    public int Priority => 0;

    /// <summary>
    /// Validates required Communications runtime dependencies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _ = serviceProvider.GetRequiredService<INotificationSender>();
        _ = serviceProvider.GetRequiredService<InMemoryFollowupScheduler>();
        return Task.CompletedTask;
    }
}
