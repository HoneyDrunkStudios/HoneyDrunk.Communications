using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Communications.Internal;

/// <summary>
/// Centralizes Communications handling for the Grid internal tenant sentinel.
/// </summary>
internal static class InternalTenant
{
    /// <summary>
    /// Gets the canonical internal tenant identifier used when no external tenant is present.
    /// </summary>
    internal static TenantId Id => TenantId.Internal;

    /// <summary>
    /// Returns the Kernel-provided tenant identifier from Grid context.
    /// </summary>
    /// <param name="gridContext">Grid context.</param>
    /// <returns>The resolved tenant identifier.</returns>
    internal static TenantId FromContext(IGridContext gridContext)
    {
        ArgumentNullException.ThrowIfNull(gridContext);

        return gridContext.TenantId;
    }

    /// <summary>
    /// Determines whether the tenant identifier represents Communications' internal tenant path.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <returns><see langword="true" /> when the tenant is internal; otherwise <see langword="false" />.</returns>
    internal static bool IsInternal(TenantId tenantId) => tenantId.IsInternal;
}
