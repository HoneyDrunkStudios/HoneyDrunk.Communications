namespace HoneyDrunk.Communications.Abstractions;

/// <summary>
/// Resolves one or more recipients for a communication intent.
/// </summary>
public interface IRecipientResolver
{
    /// <summary>
    /// Resolves the recipients targeted by the supplied intent.
    /// </summary>
    /// <param name="intent">The intent whose recipients should be resolved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous stream of recipients.</returns>
    public IAsyncEnumerable<RecipientHandle> ResolveAsync(IMessageIntent intent, CancellationToken cancellationToken = default);
}
