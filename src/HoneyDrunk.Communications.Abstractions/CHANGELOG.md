# Changelog

## Unreleased - Runtime welcome-flow alignment

- No breaking contract changes.
- Version remains aligned with the unreleased package baseline.

## Unreleased - Phase 1 contract surface

- Added `ICommunicationOrchestrator`, `IMessageIntent`, `IRecipientResolver`, `IPreferenceStore`, `ICadencePolicy`, and `ICommunicationDecisionLog`.
- Added tenant-aware supporting records and enums: `MessageIntent`, `RecipientHandle`, `RecipientPreferences`, `MessageDecision`, `MessageDecisionOutcome`, `CadenceVerdict`, `CadenceOutcome`, and `CommunicationDecisionLogEntry`.
- Added the `HoneyDrunk.Kernel.Abstractions` dependency for canonical `TenantId` usage.

## 0.1.0 - Initial scaffold

- Created the empty Abstractions package scaffold.
- Added package metadata and README/CHANGELOG files.
