namespace HoneyDrunk.Communications.Abstractions;

/// <summary>
/// Captures the result of evaluating or sending a communication intent.
/// </summary>
/// <param name="Outcome">The decision outcome.</param>
/// <param name="Reason">Human-readable reason suitable for audit logs.</param>
/// <param name="ScheduledFor">Future send time when <paramref name="Outcome" /> is <see cref="MessageDecisionOutcome.Scheduled" />.</param>
/// <param name="CorrelationKey">Optional caller-supplied or generated key linking this decision to the triggering intent.</param>
public sealed record MessageDecision(
    MessageDecisionOutcome Outcome,
    string Reason,
    DateTimeOffset? ScheduledFor = null,
    string? CorrelationKey = null);
