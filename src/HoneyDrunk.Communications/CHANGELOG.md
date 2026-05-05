# Changelog

## Unreleased - Welcome flow runtime

- Added the concrete `CommunicationOrchestrator` for the welcome-email flow.
- Added in-memory preferences, cadence policy, follow-up scheduler, and decision log.
- Consolidated internal-tenant detection across in-memory preference and cadence enforcement.
- Added Notify.Abstractions integration through `INotificationSender`.
- Added runtime options for welcome follow-up delay and scheduler interval.

## Unreleased - Kernel integration wiring

- Added `CommunicationsOptions` and `AddCommunications(...)` service registration.
- Added Phase 1 no-op startup hook and healthy-by-default health contributor.
- Added Kernel service validation for required Grid context and telemetry services.
- Added Kernel package references required by the runtime wiring.

## 0.1.0 - Initial scaffold

- Created the empty runtime package scaffold.
- Added package metadata and README/CHANGELOG files.
