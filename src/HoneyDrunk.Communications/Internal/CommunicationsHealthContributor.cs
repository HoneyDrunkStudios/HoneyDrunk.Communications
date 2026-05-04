using HoneyDrunk.Kernel.Abstractions.Health;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;

namespace HoneyDrunk.Communications.Internal;

/// <summary>
/// Health contributor for Phase 1 Communications runtime wiring.
/// </summary>
#pragma warning disable CA1812 // Registered through Microsoft.Extensions.DependencyInjection.
internal sealed class CommunicationsHealthContributor : IHealthContributor
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
    public Task<(HealthStatus status, string? message)> CheckHealthAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<(HealthStatus status, string? message)>((HealthStatus.Healthy, "Communications Phase 1 wiring is registered."));
#pragma warning restore SA1316
}
