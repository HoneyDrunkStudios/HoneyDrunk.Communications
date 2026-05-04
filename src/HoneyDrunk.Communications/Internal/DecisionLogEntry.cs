using HoneyDrunk.Communications.Abstractions;

namespace HoneyDrunk.Communications.Internal;

/// <summary>
/// In-memory append-only audit record for a Communications runtime decision.
/// </summary>
/// <param name="Id">Unique decision-log entry identifier.</param>
/// <param name="Timestamp">UTC timestamp when the decision was recorded.</param>
/// <param name="TenantId">Tenant isolation boundary for the decision.</param>
/// <param name="IntentKind">Intent kind evaluated by Communications.</param>
/// <param name="Recipient">Recipient targeted by the decision.</param>
/// <param name="Decision">Decision outcome.</param>
/// <param name="CorrelationId">Grid correlation identifier.</param>
public sealed record DecisionLogEntry(
    Guid Id,
    DateTimeOffset Timestamp,
    string TenantId,
    string IntentKind,
    RecipientHandle Recipient,
    MessageDecision Decision,
    string CorrelationId);
