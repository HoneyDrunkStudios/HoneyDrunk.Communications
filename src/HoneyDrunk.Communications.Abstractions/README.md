# HoneyDrunk.Communications.Abstractions

Contract package for the Grid communications decision layer.

## Public surface

- `ICommunicationOrchestrator` — evaluates and sends communication intents.
- `IMessageIntent` / `MessageIntent` — describes the business event, recipient, and payload.
- `IRecipientResolver` — resolves one or more recipients for an intent.
- `IPreferenceStore` — tenant-scoped recipient preference lookups and updates.
- `ICadencePolicy` — tenant-scoped cadence checks.
- `ICommunicationDecisionLog` — append-only audit surface for send-or-suppress decisions.
- Supporting records/enums: `RecipientHandle`, `RecipientPreferences`, `MessageDecision`, `MessageDecisionOutcome`, `CadenceVerdict`, `CadenceOutcome`, `CommunicationDecisionLogEntry`.

Tenancy uses `HoneyDrunk.Kernel.Abstractions.Identity.TenantId`. Communications does not define a parallel tenant primitive or accept string-shaped tenant identifiers.

## Example

```csharp
var intent = new MessageIntent(
    IntentKind: "welcome-email",
    TriggerEventId: userSignedUpEventId,
    Recipient: new RecipientHandle(userId, "email"),
    Payload: new Dictionary<string, string> { ["displayName"] = displayName });

MessageDecision decision = await orchestrator.EvaluateAsync(intent, cancellationToken);
```
