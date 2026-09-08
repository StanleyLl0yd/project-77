# Project 77

**Project 77** is the internal codename for a narrative-driven mobile puzzle adventure built with **Unity 6.3 LTS + C# + URP**.

The game begins as a small mystery about restoring an abandoned island. Over time the player uncovers an underground research complex, an ancient vessel, other planets, and eventually a network of worlds.

> Each time the player thinks they understand the scale of the game, the world becomes larger.

## Current status

**Prototype Phase — Prototype 0.1 / Gate P1**

Pre-production is complete. Gate P0 has an owner-authorized **CONTINUE** decision. **Energy Routing** is now the selected core for the integrated P1 prototype; the retained P0 evidence limitation is documented in `Docs/33_P0_GATE_DECISION.md`.

Active test flow:

`Energy Routing -> Reward -> Generator Repair -> Island Change -> Area Unlock -> Discover 77 -> Voluntary Next Puzzle`

P1 uses the validated 10-level Energy Routing set as the initial integrated path. Path / Expedition Routing and Flow / Network Restoration remain P0/debug implementations while P1 validates whether the meta loop increases desire to continue.

## Start here

Coding agents must read [`AGENTS.md`](AGENTS.md) first.

Current implementation documents:

- [`Docs/28_PROTOTYPE_01_SPEC.md`](Docs/28_PROTOTYPE_01_SPEC.md) — active implementation contract
- [`Docs/32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md`](Docs/32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md) — ordered tasks and Definition of Done
- [`Docs/31_ENGINEERING_CONVENTIONS.md`](Docs/31_ENGINEERING_CONVENTIONS.md) — Unity/C# conventions
- [`Docs/30_PROTOTYPE_ANALYTICS_CONTRACT.md`](Docs/30_PROTOTYPE_ANALYTICS_CONTRACT.md) — prototype event schema
- [`Docs/29_PROTOTYPE_PLAYTEST_PROTOCOL.md`](Docs/29_PROTOTYPE_PLAYTEST_PROTOCOL.md) — external playtest protocol
- [`Docs/18_PRODUCT_GATES.md`](Docs/18_PRODUCT_GATES.md) — P0/P1 gates
- [`Docs/13_DECISION_LOG.md`](Docs/13_DECISION_LOG.md) — product/high-level decision status

Full documentation index: [`Docs/README.md`](Docs/README.md).

`Docs/11_BACKLOG_IDEAS.md` is not committed scope.

## Technology baseline

- Unity 6.3 LTS
- C#
- URP
- Android-first, cross-platform architecture
- Android API 26 minimum baseline
- targetSdk API 36 baseline; compileSdk >= target and compatible with the current Unity/Android toolchain
- arm64-v8a mandatory for release
- AAB primary Android store artifact; APK allowed for development/playtests/direct distribution
- 16 KB memory-page compatibility required for final 64-bit native dependencies/artifacts

Gameplay/puzzle rules stay independent from billing, ads, analytics providers, auth, cloud save, notifications, and a specific store.

## Prototype scope rule

Until both Gate P0 and Gate P1 return **CONTINUE**, do not build production art, production backend, IAP/store, ads/mediation, subscription, Season Pass, other planets, ship gameplay, or mass content production.

Placeholder visuals are expected. A running build is not enough: external players must understand the loop and **voluntarily choose to continue**.

## Commercial name

`Project 77` is a codename, not a cleared public product name. Final naming will be selected later through store, trademark, domain, social-handle, and IP clearance.
