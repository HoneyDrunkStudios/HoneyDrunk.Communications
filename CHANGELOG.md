# Changelog

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
