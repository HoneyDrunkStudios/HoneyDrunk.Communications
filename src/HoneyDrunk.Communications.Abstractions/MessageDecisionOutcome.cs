namespace HoneyDrunk.Communications.Abstractions;

/// <summary>
/// Outcomes produced by the communications decision layer.
/// </summary>
public enum MessageDecisionOutcome
{
    /// <summary>
    /// The message was approved and sent through the delivery backend.
    /// </summary>
    Sent,

    /// <summary>
    /// The message was suppressed by recipient preferences.
    /// </summary>
    SuppressedByPreference,

    /// <summary>
    /// The message was suppressed by cadence policy.
    /// </summary>
    SuppressedByCadence,

    /// <summary>
    /// The message was accepted but scheduled for a future send.
    /// </summary>
    Scheduled,

    /// <summary>
    /// The message failed while being evaluated or delegated.
    /// </summary>
    Failed
}
