using HoneyDrunk.Communications.Internal;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Kernel.Abstractions.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HoneyDrunk.Communications;

/// <summary>
/// Dependency injection extensions for the Communications runtime package.
/// </summary>
public static class CommunicationsServiceCollectionExtensions
{
    /// <summary>
    /// Registers Communications runtime services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration callback.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required Kernel services are not already registered.</exception>
    public static IServiceCollection AddCommunications(
        this IServiceCollection services,
        Action<CommunicationsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        ValidateKernelService<IGridContextAccessor>(services);
        ValidateKernelService<IOperationContextAccessor>(services);
        ValidateKernelService<ITelemetryActivityFactory>(services);

        services.AddOptions<CommunicationsOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupHook, CommunicationsStartupHook>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHealthContributor, CommunicationsHealthContributor>());

        return services;
    }

    private static void ValidateKernelService<TService>(IServiceCollection services)
    {
        if (!services.Any(service => service.ServiceType == typeof(TService)))
        {
            throw new InvalidOperationException(
                $"HoneyDrunk.Communications requires {typeof(TService).Name} to be registered before AddCommunications. Call the Kernel registration first.");
        }
    }
}
