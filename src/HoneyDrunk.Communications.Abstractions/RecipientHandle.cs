namespace HoneyDrunk.Communications.Abstractions;

/// <summary>
/// Identifies a recipient targeted by a communication intent.
/// </summary>
/// <param name="Identity">Stable recipient identity within the calling Node's user model.</param>
/// <param name="PreferredChannel">Preferred delivery channel hint, such as <c>email</c> or <c>sms</c>.</param>
public sealed record RecipientHandle(string Identity, string PreferredChannel);
