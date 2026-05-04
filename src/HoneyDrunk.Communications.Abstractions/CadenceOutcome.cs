namespace HoneyDrunk.Communications.Abstractions;

/// <summary>
/// Outcomes returned by a cadence policy check.
/// </summary>
public enum CadenceOutcome
{
    /// <summary>
    /// The communication may proceed now.
    /// </summary>
    Allow,

    /// <summary>
    /// The communication should not be sent.
    /// </summary>
    Suppress,

    /// <summary>
    /// The communication may proceed at a later time.
    /// </summary>
    Defer
}
