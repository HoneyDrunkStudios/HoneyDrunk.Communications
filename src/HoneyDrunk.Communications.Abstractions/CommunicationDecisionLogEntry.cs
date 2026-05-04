using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Communications.Abstractions;

/// <summary>
/// Append-only audit record for a communications send-or-suppress decision.
/// </summary>
/// <param name="Id">Unique decision-log entry identifier.</param>
/// <param name="Timestamp">UTC timestamp when the decision was recorded.</param>
/// <param name="TenantId">Tenant isolation boundary for the decision. Internal traffic uses <c>TenantId.Internal</c>.</param>
/// <param name="IntentKind">Intent kind evaluated by Communications.</param>
/// <param name="Recipient">Recipient targeted by the decision.</param>
/// <param name="Decision">Result of the decision.</param>
/// <param name="CorrelationId">Grid correlation identifier linked to the originating operation.</param>
public sealed record CommunicationDecisionLogEntry(
    Guid Id,
    DateTimeOffset Timestamp,
    TenantId TenantId,
    string IntentKind,
    RecipientHandle Recipient,
    MessageDecision Decision,
    string CorrelationId);
