# HoneyDrunk.Communications

HoneyDrunk.Communications is the Grid's tenant-aware orchestration layer for outbound messaging: it decides whether a message should be sent, to whom, when, and as part of what workflow, while delegating delivery mechanics to HoneyDrunk.Notify.

## Status

Seed runtime is at `0.3.0`. The current cut includes the contract surface, Kernel abstractions integration, and a concrete welcome-email path with in-memory preferences, cadence, decision logging, and Notify.Abstractions delegation.

## Packages

- `HoneyDrunk.Communications.Abstractions` — contract package for communication orchestration interfaces and message-decision primitives.
- `HoneyDrunk.Communications` — Kernel-abstractions-integrated runtime package with the Phase 2 welcome-email orchestrator and in-memory stores.

## Canonical Node Entry

- HoneyDrunk.Studio catalog: [`catalogs/nodes.json`](https://github.com/HoneyDrunkStudios/HoneyDrunk.Studio/blob/main/catalogs/nodes.json)

## .NET SDK

Build with the .NET 11 SDK pinned in `global.json` (11.0.100, stable patch roll-forward). Runtime and test projects target `net11.0`; Abstractions retain `net10.0` alongside `net11.0` and public contract shapes are unchanged.
