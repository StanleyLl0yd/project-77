# Project 77 — Prototype 0.1 Implementation Backlog

Status: **ACTIVE ordered backlog — Milestone G / P1**.

Gate P0 has an owner-authorized **CONTINUE** decision and Energy Routing is the selected P1 core. Quantitative P0 evidence is incomplete and explicitly documented in `33_P0_GATE_DECISION.md`.

This backlog is executable scope for the current phase. `28_PROTOTYPE_01_SPEC.md` remains the implementation contract; `18_PRODUCT_GATES.md` remains the gate authority.

Do not pull later production work forward merely because an item is easy or interesting.

## Milestone A — Bootstrap

### P77-001 — Create Unity project

Work:

- Unity 6.3 LTS project;
- URP configured;
- Android build support selected;
- project committed with `Assets/`, `Packages/`, `ProjectSettings/`.

DoD:

- project opens without missing-package/compiler errors;
- a minimal scene runs in Editor;
- no generated `Library/Temp/Logs/UserSettings` content is tracked.

### P77-002 — Establish project-owned layout

Work:

- create `Assets/Project77/...` structure from `31_ENGINEERING_CONVENTIONS.md` as actually needed;
- set root namespace conventions;
- add minimal `.asmdef` boundaries only if useful for tests.

DoD:

- puzzle-domain tests can compile/run without scene dependencies;
- no speculative service architecture is introduced.

### P77-003 — Android prototype build baseline

Work:

- minSdk API 26;
- targetSdk 36 baseline;
- compileSdk >= target and compatible with the installed Unity toolchain;
- ARM64 enabled for the P1 external-device build;
- APK development/playtest output.

DoD:

- an Android build installs and launches on at least one representative device/emulator;
- exact Unity/editor/toolchain versions are recorded;
- no store/ads SDK is added.

### P77-004 — CI/build smoke check

Work:

- add the smallest practical GitHub Actions/Unity-compatible validation available in the environment;
- at minimum compile/test or document why licensed Unity CI cannot yet run.

DoD:

- repository has a repeatable documented validation path;
- no false claim of CI success if credentials/license are unavailable.

## Milestone B — Shared puzzle/test shell

### P77-010 — Define level envelope and validator

Work:

- implement schema version, stable ID, revision, variant, difficulty tag, variant payload;
- validate malformed/duplicate IDs/revisions.

DoD:

- invalid examples fail deterministic tests;
- valid examples load with exact ID/revision preserved for analytics.

### P77-011 — Define minimal puzzle runner boundary

Work:

- start/load attempt;
- apply actions;
- active/success/fail state;
- restart;
- no UI/business/platform dependencies.

DoD:

- at least one fake/test implementation proves the shell contract;
- no over-generalized base class is required.

### P77-012 — Input/presentation shell

Work:

- one-finger input translation;
- placeholder board/view;
- retry/next affordances;
- variant selector available only for test/debug builds.

DoD:

- shell can host all three variants without putting puzzle rules in UI scripts.

### P77-013 — Prototype analytics adapter

Work:

- implement `30_PROTOTYPE_ANALYTICS_CONTRACT.md` envelope/event types;
- use local/file/in-memory sink or smallest suitable provider adapter;
- session/playtest/build/variant/level metadata.

DoD:

- happy-path test session produces schema-valid events;
- no PII or production analytics platform is required.

## Milestone C — Prototype A: Energy Routing

### P77-020 — Energy Routing deterministic domain

DoD:

- core connection/path rules are deterministic/tested;
- success/fail/state can run without rendering;
- invalid interactions can be classified for telemetry.

### P77-021 — Energy Routing presentation

DoD:

- playable with one finger on Android/Editor using placeholder art;
- feedback makes valid/invalid path actions visible.

### P77-022 — Energy Routing test set

DoD:

- minimum **10** validated levels, including onboarding and at least a few non-trivial cases;
- stable IDs/revisions;
- no production art.

## Milestone D — Prototype B: Path / Expedition Routing

### P77-030 — Path/Expedition deterministic domain

DoD:

- planned routes and collision/outcome rules are deterministic/tested;
- simulation result is independent of animation timing.

### P77-031 — Path/Expedition presentation

DoD:

- one-finger planning/commit flow is playable with placeholders;
- failure/collision feedback is understandable.

### P77-032 — Path/Expedition test set

DoD:

- minimum **10** validated levels with stable IDs/revisions.

## Milestone E — Prototype C: Flow / Network Restoration

### P77-040 — Flow/Network deterministic domain

DoD:

- network/flow rules are explicit, deterministic, and tested;
- success state can be computed without presentation timing.

### P77-041 — Flow/Network presentation

DoD:

- one-finger interaction is playable with placeholder nodes/links/flows;
- the player can distinguish inactive, connected, and correctly restored network state.

### P77-042 — Flow/Network test set

DoD:

- minimum **10** validated levels with stable IDs/revisions.

## Milestone F — P0 instrumentation and playtest — DECISION RECORDED

### P77-050 — P0 build/revision freeze

DoD:

- variant content revisions fixed for the batch;
- event schema version fixed;
- build ID/commit recorded;
- test instructions use `29_PROTOTYPE_PLAYTEST_PROTOCOL.md`.

### P77-051 — Run fresh-tester P0 batches

DoD:

- target at least 10 fresh exposures/variant unless early-stop rationale is documented;
- device/order/help/exclusions recorded;
- telemetry export and moderator notes retained anonymously.

### P77-052 — Gate P0 report

DoD:

- compare A/B/C on the same gate definitions;
- name CONTINUE/ITERATE/PIVOT/STOP;
- if CONTINUE, record winning mechanic decision in `13_DECISION_LOG.md`;
- rejected variant presentation code may then be removed if no longer useful.

## Milestone G — Selected core + meta loop — ACTIVE

Only starts after P0 CONTINUE.

### P77-060 — Expand selected core to Prototype 0.1 set

DoD:

- **10–20** validated levels for the selected core/meta flow;
- revisions frozen per P1 batch;
- difficulty remains test-oriented, not production-balanced.

### P77-061 — Prototype resources/reward

Work:

- Scrap and Energy, or explicitly approved equivalents;
- level reward;
- simple balances for the test path only.

DoD:

- player can understand at least one resource use;
- analytics records reward and spend;
- no production economy/shop exists.

### P77-062 — Placeholder island state

DoD:

- damaged generator visible;
- one blocked area visible;
- minimal island state can change without production art.

### P77-063 — Generator repair and visible change

DoD:

- earned resource can repair generator;
- repair creates obvious visible world change;
- analytics emits spend/repair/island-change events exactly once.

### P77-064 — Area unlock and 77 discovery

DoD:

- repair/progression unlocks one area;
- damaged robot 77 is discovered;
- next intended action is understandable;
- analytics emits area unlock + 77 discovery.

### P77-065 — Genuine continuation choice

DoD:

- after reward/meta/discovery, player gets a real next-puzzle choice;
- no auto-advance makes voluntary continuation unmeasurable;
- offer/click timing is logged.

## Milestone H — P1 external playtest

### P77-070 — P1 test build freeze

DoD:

- build, levels, telemetry schema, orientation, device matrix and help rules recorded before test.

### P77-071 — Run P1 fresh-tester batch

DoD:

- protocol followed;
- puzzle/resource/repair/world-change/77 comprehension observed;
- voluntary continuation measured without moderator prompting.

### P77-072 — Gate P1 report

DoD:

- report follows the standard template;
- decision is CONTINUE / ITERATE / PIVOT / STOP;
- evidence and exclusions are explicit;
- documentation is updated from results, not speculation.

## Explicitly not in this backlog before P0 + P1 CONTINUE

- production backend;
- store/IAP;
- ads/mediation;
- subscription;
- Season Pass;
- cloud/social production stack;
- production island art/content;
- planets;
- ship gameplay;
- mass narrative/localization/content production.
