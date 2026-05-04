using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Communications.Abstractions;

/// <summary>
/// Enforces tenant-scoped communication cadence rules for recipients and intent kinds.
/// </summary>
/// <remarks>
/// Cadence state is scoped to the <c>(tenantId, recipient, intent kind)</c> tuple. When <c>tenantId.IsInternal</c> is
/// <c>true</c>, implementations should return
/// <c>new CadenceVerdict(CadenceOutcome.Allow, DeferUntil: null, Reason: "internal-tenant-bypass")</c> without consulting
/// any backing store. Internal Grid traffic is not subject to per-tenant cadence enforcement.
/// </remarks>
public interface ICadencePolicy
{
    /// <summary>
    /// Checks whether the supplied intent may be sent to the recipient within the tenant.
    /// </summary>
    /// <param name="tenantId">The tenant isolation boundary.</param>
    /// <param name="recipient">The recipient targeted by the intent.</param>
    /// <param name="intent">The intent being evaluated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cadence verdict.</returns>
    public Task<CadenceVerdict> CheckAsync(TenantId tenantId, RecipientHandle recipient, IMessageIntent intent, CancellationToken cancellationToken = default);
}
