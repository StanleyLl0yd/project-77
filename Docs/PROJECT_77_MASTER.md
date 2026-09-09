# PROJECT 77 — MASTER DOCUMENTATION v0.6

**Status:** pre-production COMPLETE; active phase = **Prototype 0.1**.

This file is an index/navigation layer. It intentionally does **not** duplicate the full documentation package.

For coding agents, root [`../AGENTS.md`](../AGENTS.md) is the operating contract. `13_DECISION_LOG.md` remains the product/high-level decision-status source of truth.

## Active implementation stack

Read in this order for current work:

1. [`28_PROTOTYPE_01_SPEC.md`](28_PROTOTYPE_01_SPEC.md) — active implementation contract.
2. [`32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md`](32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md) — ordered tasks and DoD.
3. [`31_ENGINEERING_CONVENTIONS.md`](31_ENGINEERING_CONVENTIONS.md) — minimal Unity/C# implementation rules.
4. [`30_PROTOTYPE_ANALYTICS_CONTRACT.md`](30_PROTOTYPE_ANALYTICS_CONTRACT.md) — canonical P0/P1 event schema.
5. [`29_PROTOTYPE_PLAYTEST_PROTOCOL.md`](29_PROTOTYPE_PLAYTEST_PROTOCOL.md) — external-test procedure.
6. [`34_P1_EXTERNAL_PLAYTEST_PLAN.md`](34_P1_EXTERNAL_PLAYTEST_PLAN.md) — preregistered Gate P1 batch hypothesis/environment rules.
7. [`18_PRODUCT_GATES.md`](18_PRODUCT_GATES.md) — P0/P1 decision gates.
8. [`02_CORE_GAMEPLAY.md`](02_CORE_GAMEPLAY.md) — mechanic hypotheses.
9. [`07_ROADMAP.md`](07_ROADMAP.md) — phase sequence.

## Governance

- [`13_DECISION_LOG.md`](13_DECISION_LOG.md) — LOCKED / PROVISIONAL / OPEN / REJECTED product decisions.
- [`CHANGELOG.md`](CHANGELOG.md) — documentation-baseline changes.
- [`PROJECT_CONTEXT_FOR_AI.md`](PROJECT_CONTEXT_FOR_AI.md) — compact context for a new AI session.
- [`project77_manifest.json`](project77_manifest.json) — machine-readable project status.
- [`11_BACKLOG_IDEAS.md`](11_BACKLOG_IDEAS.md) — **uncommitted ideas only**, not active scope.

## Product and game design

- [`00_PRODUCT_VISION.md`](00_PRODUCT_VISION.md)
- [`01_GAME_DESIGN_DOCUMENT.md`](01_GAME_DESIGN_DOCUMENT.md)
- [`02_CORE_GAMEPLAY.md`](02_CORE_GAMEPLAY.md)
- [`03_NARRATIVE_BIBLE.md`](03_NARRATIVE_BIBLE.md)
- [`04_WORLD_CONTENT_SYSTEM.md`](04_WORLD_CONTENT_SYSTEM.md)
- [`10_UX_ONBOARDING.md`](10_UX_ONBOARDING.md)
- [`12_RISKS_GUARDRAILS.md`](12_RISKS_GUARDRAILS.md)
- [`17_ART_AUDIO_DIRECTION.md`](17_ART_AUDIO_DIRECTION.md)

## Economy, LiveOps and growth — future-phase direction

These constrain later production but are **not a request to implement them during P0/P1**.

- [`05_ECONOMY_MONETIZATION.md`](05_ECONOMY_MONETIZATION.md)
- [`06_LIVEOPS_SOCIAL.md`](06_LIVEOPS_SOCIAL.md)
- [`09_ANALYTICS_KPI.md`](09_ANALYTICS_KPI.md)
- [`15_GO_TO_MARKET.md`](15_GO_TO_MARKET.md)
- [`22_ECONOMY_SIMULATION.md`](22_ECONOMY_SIMULATION.md)
- [`23_CONTENT_PRODUCTION_MODEL.md`](23_CONTENT_PRODUCTION_MODEL.md)

## Engineering and operations — production-capable direction

Current prototype implementation must still obey the scope freeze in `28_PROTOTYPE_01_SPEC.md`.

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

## Current P0/P1 authoritative summary

- Gate P0: **CONTINUE** (owner-authorized; evidence limitation recorded).
- Selected P1 core: **Energy Routing**.
- Minimum P0 comparison content: **10 validated levels per variant**; add more only to answer a defined test question.
- P1 winner path: **10–20 selected-core levels -> reward -> repair generator -> visible island change -> unlock -> discover 77 -> genuine next-puzzle choice**.
- Placeholder visuals are expected.
- No production backend/store/ads/subscription/Season Pass/planets/ship layer/mass content before P0 + P1 CONTINUE.

## Soft-launch target authority

Initial Gate P4 decision targets:

- D1 >= 30%;
- D7 8–12%+;
- D30 4–7%+;
- crash/ANR-free >99.5%.

The older 35% / 12–15% / 5% set is retired as an authoritative second target set.

## Locked current baseline

- Internal codename: **Project 77**; commercial title not yet cleared.
- Companion/mascot: robot **77**.
- Macro arc: abandoned island -> underground complex -> ancient vessel -> space -> other planets -> world network.
- Persistent island home.
- Main story remains free.
- Monetization direction: Season Pass, optional subscription, cosmetics, convenience IAP, rewarded ads.
- Unity 6.3 LTS + C# + URP.
- Android-first, cross-platform architecture.
- Prototype before production expansion.
- No free-form chat/UGC at launch.
- No paid-loot-box launch dependency.
