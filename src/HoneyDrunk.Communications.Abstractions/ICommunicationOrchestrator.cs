namespace HoneyDrunk.Communications.Abstractions;

/// <summary>
/// Top-level entry point for evaluating and sending outbound communications from business events.
/// </summary>
/// <remarks>
/// Implementations decide what should be sent, to whom, when, and why. Delivery mechanics are delegated to Notify.
/// </remarks>
public interface ICommunicationOrchestrator
{
    /// <summary>
    /// Evaluates an intent and returns the communication decision without performing delivery.
    /// </summary>
    /// <param name="intent">The message intent to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resulting message decision.</returns>
    public Task<MessageDecision> EvaluateAsync(IMessageIntent intent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates an intent and performs delivery when the decision allows it.
    /// </summary>
    /// <param name="intent">The message intent to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resulting message decision.</returns>
    public Task<MessageDecision> SendAsync(IMessageIntent intent, CancellationToken cancellationToken = default);
}
