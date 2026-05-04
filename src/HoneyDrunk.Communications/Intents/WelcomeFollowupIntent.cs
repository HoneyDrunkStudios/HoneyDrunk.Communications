using HoneyDrunk.Communications.Abstractions;

namespace HoneyDrunk.Communications.Intents;

/// <summary>
/// Message intent for the follow-up welcome email sent after the initial welcome window.
/// </summary>
/// <param name="Recipient">Recipient targeted by the follow-up.</param>
/// <param name="TriggerEventId">Opaque identifier of the follow-up trigger.</param>
/// <param name="OriginalDecisionCorrelationKey">Correlation key of the original welcome decision.</param>
/// <param name="Payload">Template payload, typically including displayName and accountUrl.</param>
public sealed record WelcomeFollowupIntent(
    RecipientHandle Recipient,
    string TriggerEventId,
    string OriginalDecisionCorrelationKey,
    IReadOnlyDictionary<string, string> Payload) : IMessageIntent
{
    /// <summary>
    /// Gets the stable welcome follow-up intent kind.
    /// </summary>
    public const string Kind = "welcome-followup";

    /// <inheritdoc />
    public string IntentKind => Kind;
}
