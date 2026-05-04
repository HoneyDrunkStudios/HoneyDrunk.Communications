using HoneyDrunk.Communications.Abstractions;

namespace HoneyDrunk.Communications.Intents;

/// <summary>
/// Message intent for the first welcome email sent after a user signs up.
/// </summary>
/// <param name="Recipient">Recipient targeted by the welcome email.</param>
/// <param name="TriggerEventId">Opaque identifier of the user-signup business event.</param>
/// <param name="Payload">Template payload, typically including displayName and accountUrl.</param>
public sealed record WelcomeEmailIntent(
    RecipientHandle Recipient,
    string TriggerEventId,
    IReadOnlyDictionary<string, string> Payload) : IMessageIntent
{
    /// <summary>
    /// Gets the stable welcome email intent kind.
    /// </summary>
    public const string Kind = "welcome-email";

    /// <inheritdoc />
    public string IntentKind => Kind;
}
