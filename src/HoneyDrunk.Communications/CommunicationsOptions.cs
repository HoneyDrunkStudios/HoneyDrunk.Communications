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
}
