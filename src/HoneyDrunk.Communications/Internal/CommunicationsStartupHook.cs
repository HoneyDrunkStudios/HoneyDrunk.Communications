using HoneyDrunk.Kernel.Abstractions.Lifecycle;

namespace HoneyDrunk.Communications.Internal;

/// <summary>
/// Startup hook used to mark Communications as participating in Kernel lifecycle orchestration.
/// </summary>
#pragma warning disable CA1812 // Registered through Microsoft.Extensions.DependencyInjection.
internal sealed class CommunicationsStartupHook : IStartupHook
{
    /// <summary>
    /// Gets the startup hook priority.
    /// </summary>
    public int Priority => 0;

    /// <summary>
    /// Executes Communications startup work.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
