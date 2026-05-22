# Changelog

## Unreleased

### Internal

- Migrated Communications tests to AwesomeAssertions and refreshed HoneyDrunk.Standards to 0.2.8.

- Backfilled Communications test coverage to 83.4% and seeded the Grid PR coverage gate baseline at 83.3% to avoid rounded-threshold drift.
- Split PR validation from the default-branch coverage baseline ratchet so pull requests keep read-only contents permissions.

## 0.2.0 - 2026-05-18

- Removed the unnecessary full `HoneyDrunk.Kernel` runtime dependency from the Communications runtime package.
- Updated Communications packages to consume `HoneyDrunk.Kernel.Abstractions` 0.7.0 for the current Kernel context contract.
- Updated the runtime Notify boundary dependency to `HoneyDrunk.Notify.Abstractions` 0.3.0.
- Kept `AddCommunications(...)` fail-fast validation for required Kernel abstractions and `INotificationSender` registration.
- Added a hosted-service adapter so the singleton follow-up scheduler registers cleanly as `IHostedService`.
- Added registration tests covering abstractions-only Kernel prerequisites and the Notify sender boundary.
- Aligned the solution package version to 0.2.0.

## 0.1.0 - Initial release

- Created the HoneyDrunk.Communications solution scaffold.
- Added Abstractions and runtime package projects with repo/package metadata, README/CHANGELOG files, and PR validation workflow.
- Added the Communications contract surface: orchestrator, intent, recipient resolver, preference store, cadence policy, and decision log.
- Added tenant-aware records and verdict/decision types using Kernel `TenantId`.
- Wired runtime `AddCommunications(...)`, startup hook, health contributor, and Kernel service validation.
- Added the concrete welcome-email `ICommunicationOrchestrator` runtime path.
- Added in-memory preference, cadence, follow-up scheduling, and append-only decision-log services.
- Consolidated internal-tenant detection across in-memory preference and cadence enforcement.
- Added `WelcomeEmailIntent` and `WelcomeFollowupIntent` plus Notify.Abstractions delegation.
- Added tests for tenant isolation, internal tenant short-circuiting, shared internal-tenant bypass behavior, and the Notify boundary canary.
- Added tag-driven Abstractions and runtime release workflows.
