namespace HoneyDrunk.Communications.Abstractions;

/// <summary>
/// Result of checking whether a communication passes cadence policy.
/// </summary>
/// <param name="Outcome">The cadence outcome.</param>
/// <param name="DeferUntil">When the communication may proceed, if deferred.</param>
/// <param name="Reason">Audit-friendly reason for the verdict.</param>
public sealed record CadenceVerdict(CadenceOutcome Outcome, DateTimeOffset? DeferUntil, string Reason);
