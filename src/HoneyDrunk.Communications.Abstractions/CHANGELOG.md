# Changelog

## 0.1.0 - Initial release

- Created the Abstractions package scaffold with package metadata and README/CHANGELOG files.
- Added `ICommunicationOrchestrator`, `IMessageIntent`, `IRecipientResolver`, `IPreferenceStore`, `ICadencePolicy`, and `ICommunicationDecisionLog`.
- Added tenant-aware supporting records and enums: `MessageIntent`, `RecipientHandle`, `RecipientPreferences`, `MessageDecision`, `MessageDecisionOutcome`, `CadenceVerdict`, `CadenceOutcome`, and `CommunicationDecisionLogEntry`.
- Added `MessageDecisionOutcome.Allowed` so evaluation-only calls can report "would send" without claiming delivery occurred.
- Added the `HoneyDrunk.Kernel.Abstractions` dependency for canonical `TenantId` usage.
