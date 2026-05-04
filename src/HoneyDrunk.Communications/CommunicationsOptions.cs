namespace HoneyDrunk.Communications;

/// <summary>
/// Options for configuring the Communications runtime package.
/// </summary>
public sealed class CommunicationsOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether Communications should register health contributors.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Gets or sets the non-durable welcome follow-up delay.
    /// </summary>
    public TimeSpan WelcomeFollowupDelay { get; set; } = TimeSpan.FromDays(2);

    /// <summary>
    /// Gets or sets the in-process follow-up scheduler polling interval.
    /// </summary>
    public TimeSpan FollowupSchedulerInterval { get; set; } = TimeSpan.FromMinutes(5);
}
