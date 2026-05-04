# HoneyDrunk.Communications

HoneyDrunk.Communications is the Grid's tenant-aware orchestration layer for outbound messaging: it decides whether a message should be sent, to whom, when, and as part of what workflow, while delegating delivery mechanics to HoneyDrunk.Notify.

## Status

Seed. This repository currently contains the solution scaffold only; public contracts and runtime behavior arrive in later packets.

## Packages

- `HoneyDrunk.Communications.Abstractions` — future contract package for communication orchestration interfaces and message-decision primitives. No public types yet.
- `HoneyDrunk.Communications` — future runtime package for Kernel-integrated orchestration behavior. No public types yet.

## Canonical Node Entry

- HoneyDrunk.Architecture catalog: [`catalogs/nodes.json`](https://github.com/HoneyDrunkStudios/HoneyDrunk.Architecture/blob/main/catalogs/nodes.json)
