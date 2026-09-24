# Project 77 — Vertical Slice 0.1 Implementation Backlog

Status: **ACTIVE ordered backlog — Gate P2**.

Active contract: `37_VERTICAL_SLICE_01_SPEC.md`.

Gate authority: `18_PRODUCT_GATES.md`.

Prototype 0.1 backlog is closed and retained in `32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md`.

## Milestone I — P2 foundation

### P77-100 — Phase transition and P2 contract

DoD:

- P1 owner CONTINUE and evidence limitation recorded;
- repository status points to Gate P2;
- active Vertical Slice spec/backlog exist;
- no P1 quantitative pass is implied.

### P77-101 — Versioned local save v1

Work:

- define the minimum slice save schema;
- stable local player/profile ID;
- persist Energy Routing/meta/island/narrative state required by P2;
- atomic/failure-aware local persistence;
- validation and migration entry point;
- representative corruption/partial-write tests.

DoD:

- new save can round-trip exactly;
- reload restores the same player-visible slice state;
- malformed/corrupted save does not silently replace valid state;
- migration test harness exists;
- gameplay domain does not depend on a cloud/vendor SDK.

### P77-102 — Vertical Slice state shell

Work:

- replace prototype-only one-run assumptions with a small persistent slice state machine;
- preserve deterministic puzzle domain;
- connect puzzle completion to persistent island/narrative progression;
- resume from save into a valid player-facing state.

DoD:

- app can close/relaunch and continue the slice;
- completed one-time rewards/repairs do not duplicate;
- progression cannot skip required causal steps through ordinary UI.

### P77-103 — Player-facing text/localization baseline

Work:

- introduce localization keys for new P2 player-facing strings;
- retain sensible default language content;
- account for text expansion and readable mobile layout.

DoD:

- new Vertical Slice UI/narrative copy is not scattered as ad-hoc hardcoded strings;
- missing keys fail visibly/safely during development.

## Milestone J — Energy Routing slice polish

### P77-110 — Vertical Slice Energy Routing presentation

DoD:

- retains P1 first-use clarity;
- endpoint identity does not rely on color alone;
- blocked cells and invalid interactions remain obvious;
- presentation reaches the selected near-final slice visual language;
- input remains one-finger friendly.

### P77-111 — Vertical Slice level sequence

Work:

- start from the validated selected-core content;
- revise/add only enough levels to support the 20–30 minute experience;
- target range 15–25, governed by pacing evidence rather than quota.

DoD:

- stable IDs/revisions;
- validation/tests green;
- difficulty progression supports the narrative/meta beats;
- no filler levels added solely to increase count.

## Milestone K — Island + 77 + mystery beat

### P77-120 — First island sector visual language

DoD:

- one sector has coherent near-final environment/UI readability;
- damaged/restored states are visually distinct;
- one landmark/restoration target is memorable;
- performance is measurable on Android reference devices.

### P77-121 — 77 Vertical Slice presentation

DoD:

- recognizable mobile-scale silhouette;
- minimal expressive animation/state;
- first interaction is readable and short;
- no production cosmetic/companion system is required.

### P77-122 — First complete mystery beat

DoD:

- setup -> discovery/anomaly -> reaction -> new question;
- does not reveal the cosmic/ship scale;
- progression survives save/load;
- analytics can confirm the beat was reached.

## Milestone L — Evidence and performance

### P77-130 — Gate P2 analytics coverage

DoD:

- FTUE progression is observable;
- puzzle/meta/77/narrative milestones are observable;
- save/load/migration failures are observable;
- no unnecessary production analytics vendor is required.

### P77-131 — Reference Android device matrix

Work:

- nominate low/mid/high reference classes;
- retain the already-used Joy 4-class device as continuity evidence where useful;
- record exact model/API/resolution/orientation for each measured device.

DoD:

- at least one low/mid-class physical Android device is profiled before Gate P2 exit;
- device list and evidence are reproducible.

### P77-132 — Performance baseline

DoD:

- FPS/frame-time evidence for representative gameplay;
- cold start and ordinary transition timings;
- peak/steady memory observation;
- 16 KB artifact check retained;
- blockers are fixed or explicitly gate the P2 decision.

## Milestone M — Gate P2

### P77-140 — Freeze Vertical Slice test build

DoD:

- exact source/build/artifact identity recorded;
- save schema/content revisions recorded;
- device/orientation/test condition recorded.

### P77-141 — Run fresh Vertical Slice sessions

DoD:

- test the full 20–30 minute connected experience;
- preserve evidence this time: session telemetry plus concise observer notes;
- explicitly test loop comprehension, 77 recall, mystery motivation and lifecycle/save behavior.

### P77-142 — Gate P2 decision

DoD:

- decision is CONTINUE / ITERATE / PIVOT / STOP;
- evidence and limitations explicit;
- no percentages or sample facts reconstructed from memory;
- next phase opens only from the recorded decision.

## Explicitly deferred from first Vertical Slice 0.1

- mass content;
- planets;
- ship gameplay;
- production backend;
- full store/IAP/ads/subscription implementation;
- Season Pass;
- LiveOps production stack;
- social production stack.
