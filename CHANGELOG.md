# Changelog

## Unreleased - Phase 1 contracts and Kernel wiring

- Added the Communications contract surface: orchestrator, intent, recipient resolver, preference store, cadence policy, and decision log.
- Added tenant-aware records and verdict/decision types using Kernel `TenantId`.
- Wired runtime `AddCommunications(...)`, startup hook, health contributor, and Kernel service validation.
- Added the tag-driven Abstractions release workflow.

## 0.1.0 - Initial scaffold

- Created the HoneyDrunk.Communications solution scaffold for ADR-0013.
- Added empty Abstractions and runtime package projects.
- Added repo-level metadata, package README/CHANGELOG files, and PR validation workflow.

