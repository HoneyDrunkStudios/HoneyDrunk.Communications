namespace HoneyDrunk.Communications.Internal;

/// <summary>
/// Internal runtime append-only decision log abstraction.
/// </summary>
public interface IDecisionLog
{
    /// <summary>
    /// Gets the appended decision log entries.
    /// </summary>
    public IReadOnlyCollection<DecisionLogEntry> Entries { get; }

    /// <summary>
    /// Appends a decision log entry.
    /// </summary>
    /// <param name="entry">The entry to append.</param>
    public void Append(DecisionLogEntry entry);
}
