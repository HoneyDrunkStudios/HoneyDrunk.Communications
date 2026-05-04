# HoneyDrunk.Communications

Runtime package for the Grid communications decision layer.

Phase 1 wires Kernel-aware runtime registration and health/startup hooks. It intentionally does not register a concrete `ICommunicationOrchestrator`; the first implementation lands with the welcome flow.

## Registration

```csharp
services.AddHoneyDrunkNode(...);
services.AddCommunications();
```

`AddCommunications(...)` expects Kernel services such as `IGridContextAccessor`, `IOperationContextAccessor`, and `ITelemetryActivityFactory` to be registered first. This keeps Communications tenant-aware from the first runtime package without introducing its own context storage.
