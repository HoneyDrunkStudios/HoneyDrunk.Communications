namespace HoneyDrunk.Communications.Abstractions;

/// <summary>
/// Snapshot of recipient communication preferences for a specific tenant.
/// </summary>
/// <param name="OptedOut">Whether the recipient opted out of communications.</param>
/// <param name="SuppressedIntentKinds">Intent kinds suppressed for this recipient.</param>
/// <param name="QuietHoursStart">Optional quiet-hours start time.</param>
/// <param name="QuietHoursEnd">Optional quiet-hours end time.</param>
/// <param name="PreferredChannel">Optional channel override selected by the recipient.</param>
public sealed record RecipientPreferences(
    bool OptedOut,
    IReadOnlySet<string> SuppressedIntentKinds,
    TimeSpan? QuietHoursStart = null,
    TimeSpan? QuietHoursEnd = null,
    string? PreferredChannel = null);
