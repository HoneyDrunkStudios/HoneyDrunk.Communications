namespace HoneyDrunk.Communications.Abstractions;

/// <summary>
/// Records append-only audit entries for every communications send-or-suppress decision.
/// </summary>
public interface ICommunicationDecisionLog
{
    /// <summary>
    /// Appends a decision-log entry.
    /// </summary>
    /// <param name="entry">The decision-log entry to append.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the entry has been recorded.</returns>
    public Task AppendAsync(CommunicationDecisionLogEntry entry, CancellationToken cancellationToken = default);
}
