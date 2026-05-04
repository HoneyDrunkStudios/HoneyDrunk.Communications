using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Communications.Abstractions;

/// <summary>
/// Stores tenant-scoped recipient communication preferences.
/// </summary>
/// <remarks>
/// Lookups are keyed by <see cref="TenantId" /> and <see cref="RecipientHandle" />. When <c>tenantId.IsInternal</c>
/// is <c>true</c>, implementations should return the default opted-in preference snapshot without consulting any backing
/// store. Internal Grid traffic is not subject to per-tenant preference enforcement.
/// </remarks>
public interface IPreferenceStore
{
    /// <summary>
    /// Gets the current preferences for a recipient within a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant isolation boundary.</param>
    /// <param name="recipient">The recipient whose preferences should be loaded.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recipient preference snapshot.</returns>
    public Task<RecipientPreferences> GetAsync(TenantId tenantId, RecipientHandle recipient, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the current preferences for a recipient within a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant isolation boundary.</param>
    /// <param name="recipient">The recipient whose preferences should be stored.</param>
    /// <param name="preferences">The preference snapshot to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when preferences have been stored.</returns>
    public Task SetAsync(TenantId tenantId, RecipientHandle recipient, RecipientPreferences preferences, CancellationToken cancellationToken = default);
}
