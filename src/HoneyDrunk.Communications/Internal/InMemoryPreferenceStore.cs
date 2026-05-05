using HoneyDrunk.Communications.Abstractions;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Communications.Internal;

/// <summary>
/// Tenant-scoped in-memory preference store used by the Phase 2 welcome flow.
/// </summary>
public sealed class InMemoryPreferenceStore : IPreferenceStore
{
    private static readonly RecipientPreferences DefaultPreferences = new(
        OptedOut: false,
        SuppressedIntentKinds: new HashSet<string>(StringComparer.Ordinal),
        QuietHoursStart: null,
        QuietHoursEnd: null,
        PreferredChannel: null);

    private readonly System.Collections.Concurrent.ConcurrentDictionary<PreferenceKey, RecipientPreferences> preferences = [];

    /// <inheritdoc />
    public Task<RecipientPreferences> GetAsync(TenantId tenantId, RecipientHandle recipient, CancellationToken cancellationToken = default)
    {
        if (InternalTenant.IsInternal(tenantId))
        {
            return Task.FromResult(DefaultPreferences);
        }

        return Task.FromResult(this.preferences.GetValueOrDefault(new PreferenceKey(tenantId, recipient.Identity), DefaultPreferences));
    }

    /// <inheritdoc />
    public Task SetAsync(
        TenantId tenantId,
        RecipientHandle recipient,
        RecipientPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        if (!InternalTenant.IsInternal(tenantId))
        {
            this.preferences[new PreferenceKey(tenantId, recipient.Identity)] = preferences;
        }

        return Task.CompletedTask;
    }

    private readonly record struct PreferenceKey(TenantId TenantId, string Identity);
}
