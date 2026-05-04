using HoneyDrunk.Communications.Abstractions;

namespace HoneyDrunk.Communications.Internal;

/// <summary>
/// Default recipient resolver that treats the intent recipient as the full target audience.
/// </summary>
public sealed class DefaultRecipientResolver : IRecipientResolver
{
    /// <inheritdoc />
    public async IAsyncEnumerable<RecipientHandle> ResolveAsync(
        IMessageIntent intent,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return intent.Recipient;
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
