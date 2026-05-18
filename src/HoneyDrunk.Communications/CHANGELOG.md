# Changelog

## 0.2.0 - 2026-05-18

- Removed the full `HoneyDrunk.Kernel` package dependency; the runtime now consumes Kernel contracts through `HoneyDrunk.Kernel.Abstractions` only.
- Updated `HoneyDrunk.Kernel.Abstractions` to 0.7.0 and `HoneyDrunk.Notify.Abstractions` to 0.3.0.
- Preserved `AddCommunications(...)` validation for required Grid context, operation context, telemetry, and Notify sender registrations.
- Added a hosted-service adapter so the singleton follow-up scheduler is registered through a concrete `IHostedService` implementation.
- Added tests for runtime registration with Kernel abstractions-only prerequisites and fail-fast Notify boundary validation.

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
