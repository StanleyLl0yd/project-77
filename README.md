# Project 77

**Project 77** is the internal codename for a narrative-driven mobile puzzle adventure built with **Unity 6.3 LTS + C# + URP**.

The game begins as a small mystery about restoring an abandoned island. Over time the player uncovers an underground research complex, an ancient vessel, other planets, and eventually a network of worlds.

> Each time the player thinks they understand the scale of the game, the world becomes larger.

## Current status

**Gate P2 — Vertical Slice 0.1**

Pre-production and Prototype 0.1 are closed. Gate P0 and Gate P1 both have owner-authorized **CONTINUE** decisions; **Energy Routing** remains the selected core.

P1 closure is deliberately evidence-limited: corrected Unity Build #12 / P1-002 passed artifact and physical-device acceptance and testers reported the experience as simple and understandable, but raw per-session telemetry/moderation exports were not retained. The preregistered P1 numeric threshold is therefore **not quantitatively evaluated** and is not reconstructed from memory.

Active P2 goal: a coherent 20–30 minute slice with one near-final island sector, 77, one complete mystery/narrative beat, versioned local save/load, FTUE + level analytics, and reference-device performance evidence.

## Start here

Coding agents must read [`AGENTS.md`](AGENTS.md) first.

Current implementation documents:

- [`Docs/37_VERTICAL_SLICE_01_SPEC.md`](Docs/37_VERTICAL_SLICE_01_SPEC.md) — active Gate P2 implementation contract
- [`Docs/38_VERTICAL_SLICE_IMPLEMENTATION_BACKLOG.md`](Docs/38_VERTICAL_SLICE_IMPLEMENTATION_BACKLOG.md) — ordered P2 tasks and Definition of Done
- [`Docs/36_P1_GATE_DECISION.md`](Docs/36_P1_GATE_DECISION.md) — P1 retrospective owner decision and evidence limitation
- [`Docs/28_PROTOTYPE_01_SPEC.md`](Docs/28_PROTOTYPE_01_SPEC.md) — completed Prototype 0.1 contract
- [`Docs/32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md`](Docs/32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md) — completed Prototype 0.1 backlog
- [`Docs/31_ENGINEERING_CONVENTIONS.md`](Docs/31_ENGINEERING_CONVENTIONS.md) — Unity/C# conventions
- [`Docs/30_PROTOTYPE_ANALYTICS_CONTRACT.md`](Docs/30_PROTOTYPE_ANALYTICS_CONTRACT.md) — prototype event schema
- [`Docs/29_PROTOTYPE_PLAYTEST_PROTOCOL.md`](Docs/29_PROTOTYPE_PLAYTEST_PROTOCOL.md) — external playtest protocol
- [`Docs/34_P1_EXTERNAL_PLAYTEST_PLAN.md`](Docs/34_P1_EXTERNAL_PLAYTEST_PLAN.md) — preregistered Gate P1 test plan
- [`Docs/35_P1_BUILD_11_DEVICE_FINDING.md`](Docs/35_P1_BUILD_11_DEVICE_FINDING.md) — latest physical-device iteration evidence
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

## Vertical Slice scope rule

P0 + P1 owner CONTINUE decisions allow Project 77 to enter Gate P2. P2 remains deliberately narrow: one coherent slice, one island sector, the selected core, 77, one mystery beat, basic save/load, analytics and performance validation.

Do not treat P1 closure as permission to build the entire production roadmap at once. Mass content, planets, ship gameplay, full monetization/backend and LiveOps production remain later-stage work unless a specific P2 test requires a small sandbox.

## Commercial name

`Project 77` is a codename, not a cleared public product name. Final naming will be selected later through store, trademark, domain, social-handle, and IP clearance.