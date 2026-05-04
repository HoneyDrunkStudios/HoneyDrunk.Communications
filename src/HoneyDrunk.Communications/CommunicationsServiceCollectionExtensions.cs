using HoneyDrunk.Communications.Abstractions;
using HoneyDrunk.Communications.Internal;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Kernel.Abstractions.Telemetry;
using HoneyDrunk.Notify.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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
    /// <exception cref="InvalidOperationException">Thrown when required Kernel or Notify services are not already registered.</exception>
    public static IServiceCollection AddCommunications(
        this IServiceCollection services,
        Action<CommunicationsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        ValidateKernelService<IGridContextAccessor>(services);
        ValidateKernelService<IOperationContextAccessor>(services);
        ValidateKernelService<ITelemetryActivityFactory>(services);
        ValidateKernelService<INotificationSender>(services);

        services.AddOptions<CommunicationsOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<IRecipientResolver, DefaultRecipientResolver>();
        services.TryAddSingleton<IPreferenceStore, InMemoryPreferenceStore>();
        services.TryAddSingleton<ICadencePolicy, InMemoryCadencePolicy>();
        services.TryAddSingleton<IDecisionLog, InMemoryDecisionLog>();
        services.TryAddSingleton<InMemoryFollowupScheduler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService>(provider => provider.GetRequiredService<InMemoryFollowupScheduler>()));
        services.TryAddSingleton<ICommunicationOrchestrator, CommunicationOrchestrator>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupHook, CommunicationsStartupHook>());

        if (HealthChecksEnabled(configure))
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHealthContributor, CommunicationsHealthContributor>());
        }

        return services;
    }

    private static void ValidateKernelService<TService>(IServiceCollection services)
    {
        if (!services.Any(service => service.ServiceType == typeof(TService)))
        {
            throw new InvalidOperationException(
                $"HoneyDrunk.Communications requires {typeof(TService).Name} to be registered before AddCommunications.");
        }
    }

    private static bool HealthChecksEnabled(Action<CommunicationsOptions>? configure)
    {
        if (configure is null)
        {
            return true;
        }

        var options = new CommunicationsOptions();
        configure(options);
        return options.EnableHealthChecks;
    }
}
