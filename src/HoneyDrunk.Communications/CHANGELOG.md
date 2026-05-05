# Changelog

## 0.1.0 - Initial release

- Created the runtime package scaffold with package metadata and README/CHANGELOG files.
- Added `CommunicationsOptions` and `AddCommunications(...)` service registration.
- Added startup hook and healthy-by-default health contributor.
- Added Kernel service validation for required Grid context and telemetry services.
- Added Kernel package references required by the runtime wiring.
- Added the concrete `CommunicationOrchestrator` for the welcome-email flow.
- Added in-memory preferences, cadence policy, follow-up scheduler, and decision log.
- Consolidated internal-tenant detection across in-memory preference and cadence enforcement.
- Added Notify.Abstractions integration through `INotificationSender`.
- Added runtime options for welcome follow-up delay and scheduler interval.
