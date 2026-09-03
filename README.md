# Project 77

**Project 77** is the internal codename for a narrative-driven mobile puzzle adventure built with **Unity 6.3 LTS + C# + URP**.

The game begins as a small mystery about restoring an abandoned island. Over time the player uncovers an underground research complex, an ancient vessel, other planets, and eventually a network of worlds.

> Each time the player thinks they understand the scale of the game, the world becomes larger.

## Current status

**Prototype Phase — Prototype 0.1**

Pre-production is complete. The current goal is to validate the core + meta loop before committing to production art, monetization systems, a large backend, or large-scale content production.

Prototype loop:

`Puzzle → Reward → Island Action → Discovery → Next Puzzle`

Prototype 0.1 must include 10–20 short puzzle levels, basic resources, repairing the generator, unlocking part of the island, and discovering robot **77**.

## Technology baseline

- Unity 6.3 LTS
- C#
- URP
- Android-first, cross-platform architecture
- Android API 26 minimum
- Google Play target API 36+ at release baseline
- arm64-v8a required
- AAB as primary Android release artifact
- 16 KB memory-page compatibility required for the final native stack

## Documentation

The full project documentation lives in [`/Docs`](Docs/README.md).

Primary sources of truth:

- [`Docs/13_DECISION_LOG.md`](Docs/13_DECISION_LOG.md) — locked, provisional, open, and rejected decisions
- [`Docs/28_PROTOTYPE_01_SPEC.md`](Docs/28_PROTOTYPE_01_SPEC.md) — active implementation contract
- [`Docs/07_ROADMAP.md`](Docs/07_ROADMAP.md) — development roadmap and gates
- [`Docs/08_TECH_ARCHITECTURE.md`](Docs/08_TECH_ARCHITECTURE.md) — technical architecture
- [`Docs/PROJECT_CONTEXT_FOR_AI.md`](Docs/PROJECT_CONTEXT_FOR_AI.md) — compact context for ChatGPT/Codex

## Commercial name

`Project 77` is a **codename**, not a cleared public product name. Final naming will be selected later through store, trademark, domain, social-handle, and IP clearance.

## Repository phase rule

Until Prototype 0.1 passes its gate, do not prioritize production art, a large backend, IAP/store implementation, Season Pass, subscriptions, planets, or mass content production. The prototype must first prove that players understand the loop and voluntarily want to continue.
