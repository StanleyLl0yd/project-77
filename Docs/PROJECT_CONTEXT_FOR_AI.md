# PROJECT 77 — CONTEXT FOR CHATGPT / CODEX

Compact context only. For coding-agent behavior, root `../AGENTS.md` is the operating contract.

## Source hierarchy

When instructions conflict:

1. project owner's current explicit instruction;
2. root `AGENTS.md`;
3. `13_DECISION_LOG.md` for product/high-level decision state;
4. `37_VERTICAL_SLICE_01_SPEC.md` for active Gate P2 scope;
5. relevant specialized docs;
6. `11_BACKLOG_IDEAS.md` only as uncommitted ideas.

Do not treat backlog ideas as committed features.

## Current phase

**Pre-production and Prototype 0.1 are complete. Active phase: Gate P2 — Vertical Slice 0.1.**

Gate P0 and Gate P1 both have owner-authorized **CONTINUE** decisions. Energy Routing is the selected core.

P1 evidence limitation is permanent and explicit:

- corrected Build #12 / P1-002 passed artifact and physical-device acceptance;
- testers were reported by the owner as finding the corrected experience simple and understandable;
- raw per-session telemetry/moderation exports were not retained;
- the preregistered P1 numeric threshold was therefore **not quantitatively evaluated** and must not be reconstructed from memory.

Closure record: `36_P1_GATE_DECISION.md`.

## Active Gate P2 goal

Build one coherent **20–30 minute** Android-first vertical slice:

`arrival/island context -> Energy Routing -> reward -> repair/world change -> 77 -> exploration/restoration -> mystery beat -> clear next objective`.

Required evidence/capabilities:

- one near-final island sector visual language;
- Energy Routing as the selected core;
- robot 77 as a memorable product element;
- one complete mystery/narrative beat;
- versioned local save/load;
- FTUE + level analytics;
- reference-device performance evidence.

A polished build alone is not enough. Fresh players must understand the loop, remember 77/the mystery, and want to know what happens next.

## Current implementation status

Completed P2 foundation work:

- P77-100 — P2 phase transition and active contract/backlog;
- P77-101 — versioned local save v1 with validation, migration boundary and primary/temp/backup recovery;
- P77-102 — selected-meta state persistence/resume across app relaunch;
- P77-103 — player-facing localization-key baseline with visible missing-key behavior.

Active implementation documents:

- `37_VERTICAL_SLICE_01_SPEC.md` — Gate P2 implementation contract;
- `38_VERTICAL_SLICE_IMPLEMENTATION_BACKLOG.md` — ordered P2 backlog;
- `31_ENGINEERING_CONVENTIONS.md` — Unity/C# conventions;
- `18_PRODUCT_GATES.md` — product gate authority;
- `19_ACCOUNT_SAVE_SPEC.md` — save/account constraints;
- `25_QA_DEVICE_PERFORMANCE.md` — device/performance baseline;
- `10_UX_ONBOARDING.md` and `17_ART_AUDIO_DIRECTION.md` — UX/art direction.

Historical prototype docs remain valid as evidence/history, not active scope:

- `28_PROTOTYPE_01_SPEC.md`;
- `29_PROTOTYPE_PLAYTEST_PROTOCOL.md`;
- `30_PROTOTYPE_ANALYTICS_CONTRACT.md`;
- `32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md`;
- `33_P0_GATE_DECISION.md`;
- `34_P1_EXTERNAL_PLAYTEST_PLAN.md`;
- `35_P1_BUILD_11_DEVICE_FINDING.md`;
- `36_P1_GATE_DECISION.md`.

## Gate P2 scope discipline

Do not jump directly to the whole production roadmap.

First Vertical Slice 0.1 intentionally defers:

- mass content;
- other planets;
- ship gameplay;
- full production backend;
- full store/IAP/ads/subscription implementation;
- Season Pass;
- LiveOps production stack;
- social production stack.

A narrow sandbox is allowed only when a concrete P2 question requires it.

## Product invariants

- Project 77 is the internal codename, not a cleared commercial title.
- Companion/mascot: robot 77.
- Macro arc: abandoned island -> underground complex -> ancient vessel -> space -> other planets -> world network.
- Cosmic scale is deliberately hidden early.
- The island remains the player's permanent home.
- Main story is free.
- Monetization direction remains later-stage: Season Pass, optional subscription, cosmetics, convenience IAP, rewarded ads.
- Core puzzle interaction remains one-finger and short-session friendly.
- Social is asynchronous first; no launch dependence on free-form UGC/chat.
- Intended product positioning: teens/adults (13+ direction), not child-directed.

## Tech baseline

- Unity 6.3 LTS + C# + URP.
- Android-first; core remains platform-neutral.
- min Android API 26.
- targetSdk 36 baseline; compileSdk >= target and supported by the current Unity toolchain.
- arm64-v8a mandatory for Android release artifacts.
- AAB primary future store artifact; APK valid for development/playtest/direct install.
- 16 KB memory-page compatibility remains mandatory for native Android artifacts.
- Gameplay/puzzle rules remain independent from billing, ads, analytics vendors, auth, cloud providers and stores.

## Engineering rules

- deterministic puzzle domain;
- data-driven levels with stable IDs/revisions;
- versioned save schema with validation/migration tests;
- localization keys for new player-facing P2 copy;
- no speculative DI framework/service locator/global event bus/content platform;
- narrow platform adapters only when active scope needs them;
- no claim that tests/builds/device checks passed unless they actually ran.

## KPI authority

Gate P2 criteria are in `18_PRODUCT_GATES.md`.

Soft-launch initial decision targets remain later-stage only:

- D1 >= 30%;
- D7 8–12%+;
- D30 4–7%+;
- crash/ANR-free >99.5%.

Do not import soft-launch KPIs into Gate P2.
