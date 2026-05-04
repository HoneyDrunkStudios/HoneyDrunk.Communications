namespace HoneyDrunk.Communications.Abstractions;

/// <summary>
/// Describes a business event that should be evaluated by the communications orchestration layer.
/// </summary>
public interface IMessageIntent
{
    /// <summary>
    /// Gets the stable intent kind, such as <c>welcome-email</c> or <c>subscription-expiring</c>.
    /// </summary>
    public string IntentKind { get; }

    /// <summary>
    /// Gets the opaque identifier of the business event that triggered this intent.
    /// </summary>
    public string TriggerEventId { get; }

    /// <summary>
    /// Gets the recipient targeted by this intent.
    /// </summary>
    public RecipientHandle Recipient { get; }

    /// <summary>
    /// Gets intent-specific payload values used by downstream mapping and templates.
    /// </summary>
    public IReadOnlyDictionary<string, string> Payload { get; }
}
