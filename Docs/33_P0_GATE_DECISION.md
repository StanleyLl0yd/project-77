# Project 77 — Gate P0 Decision

Status: **CONTINUE — owner-authorized transition to P1**.

Decision date: **2026-09-08**.

## Evidence basis

Validated engineering build:

- Android APK build target: `project77-android-apk`, build #9;
- source commit: `79a7185afa0708cdfd90de1d70144cae317f5107`;
- Unity: `6000.3.22f1`;
- ARM64 Android path;
- responsive P0 UI validated in repository CI and then on a physical Joy 4-class Android device.

Observed playtest evidence reported by the project owner:

- owner device smoke completed successfully;
- multiple levels were played without observed blocking gameplay defects;
- several additional external people tested the prototype and reported the build/mechanics as working acceptably;
- the responsive UI change resolved the previously observed readability blocker.

## Evidence limitation

The informal external sessions were not retained as a complete protocol-grade P0 batch.

The repository does **not** have enough retained evidence to claim exact values for:

- fresh exposure count per A/B/C variant;
- counterbalanced variant order;
- unaided-comprehension percentage;
- voluntary-continuation percentage;
- median level duration;
- help-required rate;
- comparative quantitative ranking of A/B/C.

Therefore this decision must not be cited as proof that every initial Gate P0 numeric target was met.

## Decision

**CONTINUE to P1.**

Selected core for Prototype 0.1 P1: **Energy Routing**.

Rationale:

- Energy Routing was the pre-registered/current favorite before P0;
- implementation is deterministic, validated, mobile-playable, and externally smoke-tested;
- no blocking issue was reported in the additional external tests;
- the project owner explicitly authorized continued development after those tests.

This is a pragmatic prototype decision under incomplete retained P0 metrics, not a statistical claim that Energy Routing proved superior to B/C.

Path / Expedition Routing and Flow / Network Restoration remain available as P0/debug code until removal is useful and safe.

## P1 requirement

P1 must restore stricter evidence collection. The integrated build must measure:

`Puzzle -> Reward -> Repair -> Visible Island Change -> Area Unlock -> 77 Discovery -> Voluntary Next Puzzle`

The P1 batch should retain anonymous telemetry and moderation notes sufficient to evaluate the Gate P1 criteria in `18_PRODUCT_GATES.md`.

Next active milestone: **Milestone G — Selected core + meta loop**.
