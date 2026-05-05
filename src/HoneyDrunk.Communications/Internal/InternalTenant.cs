using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Communications.Internal;

/// <summary>
/// Centralizes Communications handling for the Grid internal tenant sentinel.
/// </summary>
internal static class InternalTenant
{
    private const string InternalAlias = "internal";
    private const string InternalTenantIdText = "00000000000000000000000000";

    /// <summary>
    /// Gets the canonical internal tenant identifier used when no external tenant is present.
    /// </summary>
    internal static TenantId Id { get; } = new(InternalTenantIdText);

    /// <summary>
    /// Returns a tenant identifier from Grid context, defaulting malformed or missing tenant values to the internal tenant.
    /// </summary>
    /// <param name="gridContext">Grid context.</param>
    /// <returns>The resolved tenant identifier.</returns>
    internal static TenantId FromContext(IGridContext gridContext)
    {
        var tenantIdText = gridContext.TenantId?.ToString();
        if (string.IsNullOrWhiteSpace(tenantIdText) || string.Equals(tenantIdText, InternalAlias, StringComparison.OrdinalIgnoreCase))
        {
            return Id;
        }

        return TenantId.TryParse(tenantIdText, out var tenantId) ? tenantId : Id;
    }

    /// <summary>
    /// Determines whether the tenant identifier represents Communications' internal tenant path.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <returns><see langword="true" /> when the tenant is internal; otherwise <see langword="false" />.</returns>
    internal static bool IsInternal(TenantId tenantId)
    {
        var tenantIdText = tenantId.ToString();
        return string.Equals(tenantIdText, InternalTenantIdText, StringComparison.Ordinal)
            || string.Equals(tenantIdText, InternalAlias, StringComparison.OrdinalIgnoreCase);
    }
}
