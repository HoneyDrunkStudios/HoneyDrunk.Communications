namespace HoneyDrunk.Communications.Abstractions;

/// <summary>
/// Immutable value-shaped message intent descriptor for simple communication flows.
/// </summary>
/// <param name="IntentKind">Stable intent kind.</param>
/// <param name="TriggerEventId">Opaque identifier of the triggering business event.</param>
/// <param name="Recipient">Recipient targeted by this intent.</param>
/// <param name="Payload">Intent-specific payload values.</param>
public sealed record MessageIntent(
    string IntentKind,
    string TriggerEventId,
    RecipientHandle Recipient,
    IReadOnlyDictionary<string, string> Payload) : IMessageIntent;
