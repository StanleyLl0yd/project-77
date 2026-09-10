# Project 77 — Documentation Changelog

## v0.6.1 — Build 11 first-use UX iteration — 2026-09-10

- Physical-device review of Unity Build #11 found a blocking first-use comprehension problem before the protocol-grade P1 fresh-tester batch.
- Build #11 remains valid technical evidence for commit `357356a9fdf34052cc666bf6ac16abec9e419848`, but its P1-001 freeze is superseded for external Gate P1 testing.
- Added `35_P1_BUILD_11_DEVICE_FINDING.md` as the recorded device finding.
- External Android/playtest launch is being constrained to the selected P1 path; legacy P0 A/B/C selection remains editor/debug-only.
- Energy Routing onboarding is being revised for explicit one-finger drag guidance, redundant endpoint labels, obvious blocked-cell markers and clearer reward/island causality.
- P77-071 remains blocked until a corrected Android APK passes physical-device first-use smoke and receives a new artifact-bound freeze.

## v0.6 — Gate P1 Execution Baseline — 2026-09-09

- Gate P0 owner-authorized CONTINUE and Energy Routing selection are reflected in the active documentation baseline.
- Milestone G integrated P1 engineering path is implemented: short intro, selected core, reward/resources, generator repair, island change, area unlock, 77 discovery and genuine continuation choice.
- Added `33_P0_GATE_DECISION.md` and `34_P1_EXTERNAL_PLAYTEST_PLAN.md`.
- Gate P1 test question, >=50% formal post-island voluntary-continuation target, 15-second window and 30-second default help threshold are preregistered from the existing Product Gates/Playtest Protocol.
- P1 freeze manifest schema v2 binds the exact APK/build/content plus orientation, device targets and help threshold into the batch fingerprint.
- P1 moderation, event-audit and gate-report tooling are active; production scope remains blocked until Gate P1 CONTINUE.

## v0.5 — Prototype Readiness Audit — 2026-09-03

Repository/documentation consistency audit and implementation-readiness pass.

Added:

- root `AGENTS.md` as the coding-agent operating contract;
- `29_PROTOTYPE_PLAYTEST_PROTOCOL.md`;
- `30_PROTOTYPE_ANALYTICS_CONTRACT.md`;
- `31_ENGINEERING_CONVENTIONS.md`;
- `32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md`.

Resolved:

- Prototype C is consistently **Flow / Network Restoration**; old `Signal Sequence` definition retired;
- P0 content is minimum 10 validated levels per variant, with further levels only when a test question needs them;
- Soft-launch KPI authority is D1 >=30%, D7 8–12%+, D30 4–7%+, crash/ANR-free >99.5%; older 35% / 12–15% / 5% set retired;
- source hierarchy now explicitly puts owner instruction -> `AGENTS.md` -> Decision Log -> active Prototype spec -> specialized docs -> backlog ideas;
- 77-reveal sequencing is no longer contradictory: Prototype 0.1 tests repair/unlock -> 77, while exact production FTUE timing is explicitly OPEN (O-012);
- long-term architecture documents are explicitly non-mandatory implementation scope during P0/P1;
- Prototype 0.1 now defines deterministic puzzle-domain, stable level ID/revision/config semantics, exact telemetry and Android prototype baseline;
- documentation index/context/manifest/roadmap/gates synchronized.

Repository hygiene:

- Git LFS remains intentionally disabled until real large-binary need exists;
- Unity-generated/cache/build artifacts remain ignored while `Assets`/`Packages`/`ProjectSettings` are intended to be tracked;
- no production backend/store/ads/monetization implementation was added.

## v0.4 — Prototype Phase

- pre-production formally marked COMPLETE;
- project status changed to active **Prototype 0.1**;
- added `28_PROTOTYPE_01_SPEC.md` as the implementation contract;
- locked Prototype scope freeze until P0/P1 CONTINUE;
- formalized technical vs product Definition of Done;
- locked the key criterion: external player voluntarily wants to continue after puzzle -> reward -> island change/discovery;
- documented explicit OUT-of-scope items before Prototype Gate;
- formalized `Data over speculation` process;
- marked orientation, early difficulty, reveal pacing, IAP prices, real retention and content velocity as intentionally unresolved until tests/data;
- Roadmap, Product Gates, Decision Log, README, AI context, manifest and Master synchronized.

## v0.3 — Production Readiness — 2026-09-02

Added:

- Product Gates;
- Account & Save specification;
- Backend/Offline/Security model;
- Remote Config specification;
- Economy Simulation specification;
- Content Production model;
- Compliance/Privacy/Audience baseline;
- QA/Device/Performance budgets;
- Incident/Rollback plan;
- IP/Brand/Naming process.

Key decisions:

- Project 77 is internal codename, not cleared commercial title;
- teens/adults (13+ product direction), not child-directed;
- no free-form UGC/chat at launch;
- guest-first + versioned cloud saves;
- server authority for money/value/time/social state;
- Remote Config rollback/kill-switch requirement;
- no paid loot-box launch;
- API 36+ Google Play baseline as of 2026-09-02;
- 16 KB page-size compatibility becomes release gate;
- LiveOps cadence must follow measured content velocity;
- explicit incident-recovery and performance budgets required before soft launch.

## v0.2 — Implementation Baseline

Locked Unity 6.3 LTS / C# / URP / Android-first, cross-platform service interfaces and Prototype 0.1 milestone.

## v0.1 — Concept Package

Established product vision, GDD, narrative, economy, LiveOps, roadmap, tech baseline, analytics, UX, risks, production and art direction.