using System.Collections.Concurrent;

namespace HoneyDrunk.Communications.Internal;

/// <summary>
/// In-memory append-only decision log for Phase 2 runtime decisions.
/// </summary>
public sealed class InMemoryDecisionLog : IDecisionLog
{
    private readonly ConcurrentBag<DecisionLogEntry> entries = [];

    /// <inheritdoc />
    public IReadOnlyCollection<DecisionLogEntry> Entries => this.entries.ToArray();

    /// <inheritdoc />
    public void Append(DecisionLogEntry entry) => this.entries.Add(entry);
}
