# HoneyDrunk.Communications

HoneyDrunk.Communications is the Grid's tenant-aware orchestration layer for outbound messaging: it decides whether a message should be sent, to whom, when, and as part of what workflow, while delegating delivery mechanics to HoneyDrunk.Notify.

## Status

Seed runtime. Phase 1 defines the contract surface and Kernel integration. Phase 2 adds the first concrete welcome-email path with in-memory preferences, cadence, decision logging, and Notify.Abstractions delegation.

## Packages

- `HoneyDrunk.Communications.Abstractions` — contract package for communication orchestration interfaces and message-decision primitives.
- `HoneyDrunk.Communications` — Kernel-integrated runtime package with the Phase 2 welcome-email orchestrator and in-memory stores.

## Canonical Node Entry

- HoneyDrunk.Architecture catalog: [`catalogs/nodes.json`](https://github.com/HoneyDrunkStudios/HoneyDrunk.Architecture/blob/main/catalogs/nodes.json)
