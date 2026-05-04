# HoneyDrunk.Communications

Runtime package for the Grid communications decision layer.

Phase 2 registers the first concrete `ICommunicationOrchestrator`: a welcome-email path that evaluates recipient preferences, enforces tenant-scoped cadence, records append-only decisions, delegates delivery to `HoneyDrunk.Notify.Abstractions`, and schedules the non-durable two-day welcome follow-up intent.

## Registration

```csharp
services.AddHoneyDrunkNode(...);
services.AddNotify(...);
services.AddCommunications(options =>
{
    options.EnableHealthChecks = true;
    options.WelcomeFollowupDelay = TimeSpan.FromDays(2);
});
```

`AddCommunications(...)` expects Kernel services (`IGridContextAccessor`, `IOperationContextAccessor`, `ITelemetryActivityFactory`) and `INotificationSender` to be registered first. This keeps Communications tenant-aware and delivery-agnostic while preserving Notify as the outbound boundary.
