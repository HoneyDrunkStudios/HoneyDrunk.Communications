using HoneyDrunk.Communications.Abstractions;
using HoneyDrunk.Communications.Intents;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Communications.Internal;

/// <summary>
/// Tenant-scoped in-memory cadence policy for welcome email intents.
/// </summary>
public sealed class InMemoryCadencePolicy : ICadencePolicy
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<CadenceKey, DateTimeOffset> sentAt = [];

    /// <inheritdoc />
    public Task<CadenceVerdict> CheckAsync(
        TenantId tenantId,
        RecipientHandle recipient,
        IMessageIntent intent,
        CancellationToken cancellationToken = default)
    {
        if (IsInternalTenant(tenantId))
        {
            return Task.FromResult(new CadenceVerdict(CadenceOutcome.Allow, DeferUntil: null, Reason: "internal-tenant-bypass"));
        }

        if (intent.IntentKind is not WelcomeEmailIntent.Kind and not WelcomeFollowupIntent.Kind)
        {
            return Task.FromResult(new CadenceVerdict(CadenceOutcome.Allow, DeferUntil: null, Reason: "untracked-intent-kind"));
        }

        var key = new CadenceKey(tenantId, recipient.Identity, intent.IntentKind);
        var recordedAt = DateTimeOffset.UtcNow;

        if (this.sentAt.TryAdd(key, recordedAt))
        {
            return Task.FromResult(new CadenceVerdict(CadenceOutcome.Allow, DeferUntil: null, Reason: "first-send"));
        }

        return Task.FromResult(new CadenceVerdict(CadenceOutcome.Suppress, DeferUntil: null, Reason: "already-sent"));
    }

    private static bool IsInternalTenant(TenantId tenantId) =>
        string.Equals(tenantId.ToString(), "00000000000000000000000000", StringComparison.OrdinalIgnoreCase)
        || string.Equals(tenantId.ToString(), "internal", StringComparison.OrdinalIgnoreCase);

    private readonly record struct CadenceKey(TenantId TenantId, string Identity, string IntentKind);
}
