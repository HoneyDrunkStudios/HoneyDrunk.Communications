# Changelog

## [0.3.1] - 2026-09-26

### Changed

- Refresh dependency and shared build-tooling versions; preserve target frameworks and existing public contracts. See the [repository dependency changes](../../CHANGELOG.md).

## 0.3.0 - 2026-06-14

- Aligned the Abstractions package version with the Communications runtime 0.3.0 release; no public Abstractions API changes.

## 0.2.0 - 2026-05-18

- Updated `HoneyDrunk.Kernel.Abstractions` to 0.7.0 so Communications contracts use the current canonical `TenantId` primitive.

## 0.1.0 - Initial release

- Created the Abstractions package scaffold with package metadata and README/CHANGELOG files.
- Added `ICommunicationOrchestrator`, `IMessageIntent`, `IRecipientResolver`, `IPreferenceStore`, `ICadencePolicy`, and `ICommunicationDecisionLog`.
- Added tenant-aware supporting records and enums: `MessageIntent`, `RecipientHandle`, `RecipientPreferences`, `MessageDecision`, `MessageDecisionOutcome`, `CadenceVerdict`, `CadenceOutcome`, and `CommunicationDecisionLogEntry`.
- Added `MessageDecisionOutcome.Allowed` so evaluation-only calls can report "would send" without claiming delivery occurred.
- Added the `HoneyDrunk.Kernel.Abstractions` dependency for canonical `TenantId` usage.
