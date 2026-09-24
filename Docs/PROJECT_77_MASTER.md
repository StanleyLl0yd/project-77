# PROJECT 77 — MASTER DOCUMENTATION v0.7

**Status:** pre-production + Prototype 0.1 COMPLETE; active phase = **Gate P2 / Vertical Slice 0.1**.

This file is an index/navigation layer. It intentionally does **not** duplicate the full documentation package.

For coding agents, root [`../AGENTS.md`](../AGENTS.md) is the operating contract. `13_DECISION_LOG.md` remains the product/high-level decision-status source of truth.

## Active implementation stack

Read in this order for current work:

1. [`37_VERTICAL_SLICE_01_SPEC.md`](37_VERTICAL_SLICE_01_SPEC.md) — active Gate P2 implementation contract.
2. [`38_VERTICAL_SLICE_IMPLEMENTATION_BACKLOG.md`](38_VERTICAL_SLICE_IMPLEMENTATION_BACKLOG.md) — ordered P2 tasks and DoD.
3. [`31_ENGINEERING_CONVENTIONS.md`](31_ENGINEERING_CONVENTIONS.md) — Unity/C# implementation rules.
4. [`18_PRODUCT_GATES.md`](18_PRODUCT_GATES.md) — Gate P2 decision authority.
5. [`19_ACCOUNT_SAVE_SPEC.md`](19_ACCOUNT_SAVE_SPEC.md) — save/account constraints.
6. [`25_QA_DEVICE_PERFORMANCE.md`](25_QA_DEVICE_PERFORMANCE.md) — device/performance baseline.
7. [`10_UX_ONBOARDING.md`](10_UX_ONBOARDING.md) — FTUE direction.
8. [`17_ART_AUDIO_DIRECTION.md`](17_ART_AUDIO_DIRECTION.md) — island/77 visual and audio direction.
9. [`30_PROTOTYPE_ANALYTICS_CONTRACT.md`](30_PROTOTYPE_ANALYTICS_CONTRACT.md) — retained event vocabulary until P2 analytics coverage extends it.
10. [`07_ROADMAP.md`](07_ROADMAP.md) — phase sequence.

## Governance

- [`13_DECISION_LOG.md`](13_DECISION_LOG.md) — LOCKED / PROVISIONAL / OPEN / REJECTED decisions.
- [`36_P1_GATE_DECISION.md`](36_P1_GATE_DECISION.md) — P1 owner CONTINUE and permanent evidence limitation.
- [`CHANGELOG.md`](CHANGELOG.md) — documentation-baseline changes.
- [`PROJECT_CONTEXT_FOR_AI.md`](PROJECT_CONTEXT_FOR_AI.md) — compact context for a new AI session.
- [`project77_manifest.json`](project77_manifest.json) — machine-readable active status.
- [`11_BACKLOG_IDEAS.md`](11_BACKLOG_IDEAS.md) — **uncommitted ideas only**.

## Current Gate P2 summary

- Gate P0: **CONTINUE**.
- Gate P1: **owner-authorized CONTINUE**, qualitative/retrospective; numeric threshold not evaluated because raw session exports were not retained.
- Selected core: **Energy Routing**.
- Active target: one coherent **20–30 minute** vertical slice.
- Required slice: one near-final island sector, 77, one mystery beat, versioned local save/load, FTUE/level analytics and reference-device performance evidence.
- P77-101 local save v1: implemented.
- P77-102 persistent selected-meta state: implemented.
- P77-103 localization-key baseline: implemented.

## Gate P2 scope boundary

The P0 + P1 CONTINUE condition allows Vertical Slice work; it does not authorize unrestricted production.

Deferred from the first Vertical Slice 0.1 unless a specific P2 test requires a narrow sandbox:

- mass content;
- planets;
- ship gameplay;
- full production backend;
- full monetization stack;
- Season Pass/subscription production;
- LiveOps production stack;
- social production stack.

## Historical Prototype 0.1 package

These remain authoritative for historical decisions/evidence but are no longer the active implementation contract:

- [`28_PROTOTYPE_01_SPEC.md`](28_PROTOTYPE_01_SPEC.md)
- [`29_PROTOTYPE_PLAYTEST_PROTOCOL.md`](29_PROTOTYPE_PLAYTEST_PROTOCOL.md)
- [`30_PROTOTYPE_ANALYTICS_CONTRACT.md`](30_PROTOTYPE_ANALYTICS_CONTRACT.md)
- [`32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md`](32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md)
- [`33_P0_GATE_DECISION.md`](33_P0_GATE_DECISION.md)
- [`34_P1_EXTERNAL_PLAYTEST_PLAN.md`](34_P1_EXTERNAL_PLAYTEST_PLAN.md)
- [`35_P1_BUILD_11_DEVICE_FINDING.md`](35_P1_BUILD_11_DEVICE_FINDING.md)
- [`36_P1_GATE_DECISION.md`](36_P1_GATE_DECISION.md)

## Product and game design

- [`00_PRODUCT_VISION.md`](00_PRODUCT_VISION.md)
- [`01_GAME_DESIGN_DOCUMENT.md`](01_GAME_DESIGN_DOCUMENT.md)
- [`02_CORE_GAMEPLAY.md`](02_CORE_GAMEPLAY.md)
- [`03_NARRATIVE_BIBLE.md`](03_NARRATIVE_BIBLE.md)
- [`04_WORLD_CONTENT_SYSTEM.md`](04_WORLD_CONTENT_SYSTEM.md)
- [`10_UX_ONBOARDING.md`](10_UX_ONBOARDING.md)
- [`12_RISKS_GUARDRAILS.md`](12_RISKS_GUARDRAILS.md)
- [`17_ART_AUDIO_DIRECTION.md`](17_ART_AUDIO_DIRECTION.md)

## Economy, LiveOps and growth — later-stage direction

These constrain later production but are **not immediate Gate P2 implementation scope**.

- [`05_ECONOMY_MONETIZATION.md`](05_ECONOMY_MONETIZATION.md)
- [`06_LIVEOPS_SOCIAL.md`](06_LIVEOPS_SOCIAL.md)
- [`09_ANALYTICS_KPI.md`](09_ANALYTICS_KPI.md)
- [`15_GO_TO_MARKET.md`](15_GO_TO_MARKET.md)
- [`22_ECONOMY_SIMULATION.md`](22_ECONOMY_SIMULATION.md)
- [`23_CONTENT_PRODUCTION_MODEL.md`](23_CONTENT_PRODUCTION_MODEL.md)

## Engineering and operations direction

- [`08_TECH_ARCHITECTURE.md`](08_TECH_ARCHITECTURE.md)
- [`14_RELEASE_AND_LIVE_CHECKLIST.md`](14_RELEASE_AND_LIVE_CHECKLIST.md)
- [`16_PRODUCTION_TEAM.md`](16_PRODUCTION_TEAM.md)
- [`19_ACCOUNT_SAVE_SPEC.md`](19_ACCOUNT_SAVE_SPEC.md)
- [`20_BACKEND_SECURITY_MODEL.md`](20_BACKEND_SECURITY_MODEL.md)
- [`21_REMOTE_CONFIG_SPEC.md`](21_REMOTE_CONFIG_SPEC.md)
- [`24_COMPLIANCE_PRIVACY.md`](24_COMPLIANCE_PRIVACY.md)
- [`25_QA_DEVICE_PERFORMANCE.md`](25_QA_DEVICE_PERFORMANCE.md)
- [`26_INCIDENT_ROLLBACK_PLAN.md`](26_INCIDENT_ROLLBACK_PLAN.md)
- [`27_IP_BRAND_NAMING.md`](27_IP_BRAND_NAMING.md)

## Soft-launch target authority

Initial Gate P4 decision targets remain later-stage:

- D1 >= 30%;
- D7 8–12%+;
- D30 4–7%+;
- crash/ANR-free >99.5%.

## Locked baseline

- Internal codename: **Project 77**; commercial title not yet cleared.
- Companion/mascot: robot **77**.
- Macro arc: abandoned island -> underground complex -> ancient vessel -> space -> other planets -> world network.
- Persistent island home.
- Main story remains free.
- Unity 6.3 LTS + C# + URP.
- Android-first, cross-platform core architecture.
- No free-form chat/UGC at launch.
- No paid-loot-box launch dependency.
