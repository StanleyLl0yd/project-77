# Project 77 — P1 External Playtest Plan

Status: **ACTIVE preregistered plan for Gate P1**.

This document fixes the test question and decision rule before the first protocol-grade P1 batch. Exact APK/build identity, orientation and device targets are frozen per batch by `Tools/p1_freeze_manifest.py`.

## 1. Hypothesis

The integrated island meta loop increases desire to continue after the selected Energy Routing core:

`Puzzle -> Reward -> Generator Repair -> Visible Island Change -> Area Unlock -> Discover 77 -> Voluntary Next Puzzle`

Primary question:

> After the first meaningful island reward/reveal, does the tester voluntarily choose another puzzle without moderator prompting?

## 2. Primary metric and decision target

Primary metric: **formal post-island voluntary continuation** as defined by `29_PROTOTYPE_PLAYTEST_PROTOCOL.md`.

Initial Gate P1 target from `18_PRODUCT_GATES.md`:

- **>= 50%** of eligible fresh testers voluntarily continue;
- observation window: **15 seconds** after the continuation affordance becomes available;
- moderator-prompted or otherwise excluded sessions do not count in the formal denominator.

Target sample for the first protocol-grade P1 batch: **10 fresh sessions**. Returning testers may be used for regression/device checks but remain separate from the fresh primary metric.

## 3. Guardrail observations

The batch must also establish whether:

- the tester understands what the received resources are for;
- the tester understands `puzzle -> reward`;
- the tester understands `reward -> repair -> visible world change`;
- the first visible island change occurs in the session;
- the unlocked area is noticed;
- robot 77 is noticed and remembered;
- 77 feels like a reward/reveal rather than another tutorial popup;
- puzzle and island feel like one loop rather than two unrelated games;
- no repeatable blocking input/runtime problem invalidates the experience.

Telemetry reach is supporting evidence, not a substitute for moderated comprehension/voluntary-continuation classification.

## 4. Build and content freeze

Before external sessions begin, the exact distributed APK must be bound to a P1 freeze manifest.

The manifest records and fingerprints:

- full Git commit SHA;
- build version;
- Unity and URP versions;
- event/metadata schema versions;
- selected core and all 10 level IDs/revisions/content hashes;
- P1 meta implementation hashes;
- governing contract hashes;
- exact APK SHA-256, ABI and 16 KB compatibility checks;
- batch orientation;
- device target list;
- help threshold;
- Gate P1 target/window.

Changing any frozen value requires a new batch/freeze.

## 5. Orientation

Orientation is still an OPEN production decision. Each P1 batch nevertheless uses one preregistered orientation so screen-layout differences do not silently change mid-batch.

The first external batch should normally use **portrait**, matching the already validated mobile prototype path. A landscape batch is allowed only when explicitly frozen as a separate batch/test condition.

## 6. Device matrix

At least one exact physical Android device/profile must be recorded before testing. Prefer using the already validated Android 10 / 1080x2340-class portrait device as the primary reference for continuity.

Additional physical devices are useful when available, especially a different screen class/API level, but they must be listed in the freeze before their sessions are included in that batch.

Each device-target string should identify enough to reproduce the environment, for example:

`Joy 4 | Android 10 / VOS 3.0 | 1080x2340 | portrait`

Session metadata still records the actual device/OS seen by the build.

## 7. Help rule

Default blocked threshold: **30 seconds with no meaningful progress after the tester has clearly attempted interaction**, matching the playtest protocol.

Before that threshold, the moderator must not explain:

- the puzzle rule or correct gesture;
- what resources are for;
- where/how to spend them;
- what the repair action will do;
- where 77 is;
- whether/where to continue.

Any help is recorded and the affected formal metric is excluded where required.

## 8. Moderator intro

Allowed neutral introduction:

> "Please play this prototype as you normally would. I am testing the game, not you. Say anything you naturally notice; I may stay quiet while you play."

No extra game-rule explanation is added before play.

## 9. Decision rule

After the frozen batch:

- **CONTINUE**: primary voluntary-continuation target is met and qualitative evidence does not show a systematic resource/repair/world-coherence failure;
- **ITERATE**: the loop shows clear interest but a specific UX/pacing/comprehension issue prevents a clean pass;
- **PIVOT**: the core remains playable but the island/meta layer does not increase desire to continue or feels disconnected;
- **STOP**: the tested experience shows no useful potential after limited, targeted iteration.

The final decision is recorded in the Decision Log and must cite the frozen batch/report rather than anecdotal recollection.
