# Project 77 — Vertical Slice 0.1 Specification

Status: **ACTIVE IMPLEMENTATION CONTRACT — Gate P2**.

This document defines the smallest coherent Vertical Slice needed to answer Gate P2:

> Does Project 77 work as one connected product experience, not merely as a sequence of prototypes?

Gate P2 follows the owner-authorized P0 + P1 CONTINUE decisions. P1 evidence limitation is documented in `36_P1_GATE_DECISION.md`.

## 1. Target experience

Build one **20–30 minute** connected Android-first slice.

Intended player flow:

```text
Arrival / island context
↓
Energy Routing onboarding
↓
Reward
↓
Repair / visible island change
↓
Meet and retain 77 as companion
↓
Short exploration / restoration beat
↓
Anomaly or mystery clue
↓
Another puzzle / island action
↓
Clear next objective and desire to continue
```

The exact pacing may change during Gate P2 testing, but the slice must feel like one game rather than puzzle screens plus an unrelated meta layer.

## 2. Required P2 content

### Selected core

Energy Routing remains the selected core.

- preserve deterministic rule/domain tests;
- replace prototype-only presentation where it prevents near-final readability;
- target **15–25 polished levels only if needed** to fill the 20–30 minute slice;
- do not manufacture levels merely to hit a count;
- stable level IDs/revisions remain mandatory.

### Island sector

One coherent island sector with near-final visual language.

It must contain enough state change to demonstrate:

- damaged/restored condition;
- at least one meaningful landmark or restoration target;
- a blocked/hidden destination or mystery affordance;
- visible consequence from puzzle-driven progress.

The exact landmark (camp, pier, lighthouse, or equivalent) is not locked by this document.

### Robot 77

77 must function as a real product element rather than a prototype popup.

Required:

- recognizable silhouette at mobile scale;
- minimal expressive state/animation;
- a clear reason for the player to remember 77;
- one small interaction or narrative response after discovery.

Final cosmetic system and full companion progression are out of scope.

### Narrative beat

One complete mystery beat is required:

- setup;
- discovery/anomaly;
- reaction involving the player and/or 77;
- a new question that motivates continuation.

Do not reveal the ship/cosmic scale.

## 3. Save/load baseline

Gate P2 requires **basic versioned local save/load**.

Minimum:

- explicit `schemaVersion`;
- stable local player/profile identifier;
- progression state required by this slice;
- island restoration state required by this slice;
- current/finished story beat state;
- selected settings needed for continuity;
- atomic or otherwise failure-aware writes;
- validation on load;
- migration entry point and tests, even if only v1 exists initially;
- corrupted/partial save must fail safely without silently overwriting the last known valid state.

Cloud provider integration is not required for the first Vertical Slice 0.1. The save model must not block later cloud binding.

## 4. UI / onboarding / accessibility

Use the P1 findings as baseline:

- first action understandable without oral explanation;
- no color-only puzzle semantics;
- clear primary CTA;
- readable text and touch targets on the reference phone class;
- player understands reward -> repair -> world change causality;
- 77/mystery is presented as discovery, not tutorial overhead.

User-facing production text introduced for the slice should move toward localization keys rather than new hardcoded copy.

Final orientation remains OPEN and must be tested during Gate P2.

## 5. Analytics

Gate P2 retains prototype event continuity where useful and adds only what is required to understand the slice.

Minimum evidence must cover:

- FTUE progression;
- Energy Routing starts/completes/fails/retries;
- reward and island actions;
- 77 discovery/interaction;
- narrative beat reached;
- save/load success/failure/migration;
- session completion/exit;
- next-objective/continuation behavior.

Do not add a production analytics vendor merely to satisfy this contract.

## 6. Performance / Android

Baseline remains:

- Unity 6.3 LTS;
- C#;
- URP;
- Android-first;
- minSdk 26;
- targetSdk 36 baseline;
- arm64-v8a;
- 16 KB native page compatibility.

Before Gate P2 exit, capture evidence on reference device classes.

Initial targets from the current project baseline:

- mid-tier gameplay target 60 FPS;
- low-tier gameplay >=30 FPS;
- cold start target <5 s;
- ordinary scene transition target <2 s;
- no known save-corruption blocker.

Exact memory budgets are set from Vertical Slice profiling, not guessed in advance.

## 7. Explicitly out of scope for Vertical Slice 0.1

Unless a specific P2 question objectively requires a sandbox, do not build:

- full production backend;
- full cloud account system;
- real store/IAP catalog;
- ads mediation;
- subscription;
- Season Pass;
- LiveOps production platform;
- social production stack;
- other planets;
- ship gameplay;
- 100+ levels;
- mass narrative/localization/content;
- production economy balancing.

A tiny platform-service sandbox may be separately approved after the slice itself is coherent; it is not a prerequisite for the first engineering workstream.

## 8. Gate P2 exit

Gate authority remains `18_PRODUCT_GATES.md`.

The slice must demonstrate:

- one connected 20–30 minute experience;
- selected puzzle + island progression feel causally connected;
- 77 is noticed and remembered;
- the mystery/narrative beat creates a clear question;
- save/load survives ordinary lifecycle use;
- FTUE/level evidence is observable;
- performance is acceptable on the recorded reference devices;
- fresh players understand what to do and want to know what happens next.

Decision: **CONTINUE / ITERATE / PIVOT / STOP**.

## 9. Open decisions intentionally left to P2 tests

- final portrait vs landscape/hybrid orientation;
- exact protagonist/avatar presentation;
- exact first-sector landmark;
- final production timing of the 77 reveal;
- final early difficulty curve;
- final art/audio polish level beyond what is required to judge the slice.

These are test questions, not permission for speculative systems.
