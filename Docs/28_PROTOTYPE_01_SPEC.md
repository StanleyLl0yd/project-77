# Project 77 — Prototype 0.1 Specification

Status: **ACTIVE IMPLEMENTATION CONTRACT**.

This document defines the nearest mandatory scope after conceptual pre-production. It exists to answer whether the fundamental Project 77 experience works, not to prove final art, monetization, backend, or the size of the future universe.

Related operational documents:

- `18_PRODUCT_GATES.md` — P0/P1 decision gates and initial thresholds;
- `29_PROTOTYPE_PLAYTEST_PROTOCOL.md` — how external tests are run;
- `30_PROTOTYPE_ANALYTICS_CONTRACT.md` — exact Prototype 0.1 telemetry schema;
- `31_ENGINEERING_CONVENTIONS.md` — minimal Unity/C# implementation conventions;
- `32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md` — ordered executable backlog.

## 1. Phase status

**Pre-production is complete. Project 77 is in Prototype Phase.**

Until both P0 and P1 return **CONTINUE**, the project does not expand into production scope.

## 2. Main product hypothesis

The player should understand and enjoy:

```text
Launch
↓
Very short intro
↓
Puzzle
↓
Reward
↓
Island
↓
Repair / Visible Change
↓
Discovery
↓
Next Puzzle
```

Key question:

> Does the player choose to continue after puzzle success becomes visible island progress/discovery?

## 3. What Prototype 0.1 must prove

Two sequential hypotheses:

### P0 — Core

At least one 30–90 second one-finger puzzle variant is understandable, repeatable, and produces genuine "one more level" behavior without production art/story carrying it.

### P1 — Core + Meta

For the P0 winner, rewards and visible island change make the player more willing to continue, and the discovery of 77 feels like a reward/reveal rather than tutorial overhead.

A technically functional build is not a product success if these hypotheses fail.

## 4. Technical baseline for the prototype

- Unity 6.3 LTS.
- C#.
- URP.
- Android-first; gameplay/puzzle domain remains platform-neutral.
- `minSdk`: API 26.
- Prototype Android target baseline: `targetSdk 36`; `compileSdk >= 36` and supported by the installed Unity/Android toolchain.
- APK is acceptable/preferred for local and external prototype distribution.
- AAB remains the primary future store-release format; producing an AAB is not a P0 requirement.
- P1 external Android build must exercise an ARM64-capable path; final release remains `arm64-v8a` mandatory.
- No additional native SDKs should be added for analytics/store/ads solely for P0/P1.
- Placeholder art/UI is expected.
- Prototype telemetry is mandatory before external P0/P1 sessions.
- Repository: `StanleyLl0yd/project-77`.

Development iteration may use the fastest Unity-supported scripting/build configuration. Before P1 gate evidence is accepted, at least one representative Android device/emulator build must install and run successfully; any untested final-release-specific property must be reported as a limitation, not assumed.

## 5. Exact P0 prototype variants

All three are **PROVISIONAL test hypotheses**, not LOCKED game design.

### A — Energy Routing

Current favorite. Connect/route energy/signal between required nodes under board constraints.

### B — Path / Expedition Routing

Plan routes for explorers/drones to reach goals while respecting collisions/constraints.

### C — Flow / Network Restoration

Restore a network/flow state by activating or redirecting relationships between nodes so required parts of the network become functional. C must test network/flow state and restoration order strongly enough that it is not merely Energy Routing with another skin.

`Signal Sequence` is retired as the current Prototype C definition as of documentation v0.5.

## 6. Minimum P0 content

For the first comparison:

- minimum **10 validated levels per variant**;
- stable level IDs and revisions;
- onboarding examples plus enough non-trivial cases to expose the mechanic's real interaction loop;
- add more than 10 only when a pre-registered question cannot be answered with the existing set.

Do not create 20 levels by habit if 10 already reveal that a mechanic is weak. Do not create 100 levels to avoid making a P0 decision.

## 7. What is shared between variants

Share only what materially improves fair comparison and testability:

- prototype/session shell;
- level envelope (`schemaVersion`, `id`, `revision`, `variant`, payload);
- input handoff where practical;
- retry/next affordances;
- analytics interface/event envelope;
- build/version metadata;
- common test/reset tooling;
- deterministic domain/test conventions.

Do **not** force unrelated puzzle rules into one inheritance tree simply to maximize code reuse.

## 8. Throwaway vs survivor code

Expected/allowed to be throwaway:

- greybox visuals;
- temporary variant-selection UI;
- presentation code for rejected variants;
- moderator/debug controls;
- prototype-only local telemetry viewers/export helpers.

Expected to survive only if proven useful:

- winning deterministic puzzle-domain logic;
- stable level ID/revision/config semantics;
- validators and meaningful tests;
- project-owned analytics interface/event contract;
- minimal meta domain after P1 validates it.

Do not polish a "survivor" merely because it might survive.

## 9. Deterministic puzzle-domain requirement

For a given:

- level definition;
- initial state/seed (if randomness is truly necessary);
- ordered player actions;

the puzzle-domain outcome must be reproducible.

Success/fail/game rules should be testable independently of MonoBehaviour, rendering, animation timing, and UI as far as practical.

Randomness should be avoided in P0 unless it is itself part of the mechanic under test. If used, the seed is explicit and recorded.

## 10. Minimum data/config contract

Prototype levels are data-driven and validated.

Shared minimum envelope:

```json
{
  "schemaVersion": 1,
  "id": "A-001",
  "revision": 1,
  "variant": "energy_routing",
  "difficultyTag": "intro",
  "payload": {}
}
```

The exact variant payload may differ by mechanic. Do not build a generic CMS/content pipeline. Detailed conventions are in `31_ENGINEERING_CONVENTIONS.md`.

## 11. Minimum prototype telemetry

Implement only the events required by `30_PROTOTYPE_ANALYTICS_CONTRACT.md`, covering at minimum:

- prototype/session start/end;
- tutorial exposure;
- level start/complete/fail/retry/quit;
- meaningful invalid interaction where practical;
- reward shown/claimed;
- resource spend;
- generator repair;
- visible island change;
- area unlock;
- 77 discovery;
- next-puzzle offered/clicked.

No PII or production attribution/monetization stack is required.

## 12. P0 testing and selection

External playtests use `29_PROTOTYPE_PLAYTEST_PROTOCOL.md`.

Initial comparison intent:

- aim for at least 10 fresh exposures per variant across batches unless a severe repeated failure justifies early stopping;
- record help/exclusions;
- do not count moderator-prompted continuation as voluntary;
- compare variants using the same P0 gate definitions.

The core is selected by observed evidence, not by concept-art appeal or author's preference.

## 13. P1 integrated Prototype 0.1 content

Only after P0 CONTINUE, integrate the selected mechanic into the meta prototype.

The resulting P1 build contains:

- very short intro;
- **10–20** short selected-core puzzle levels for the integrated path;
- simple reward screen;
- two prototype resources, currently Scrap and Energy or explicitly approved equivalents;
- placeholder island/map;
- damaged generator;
- one blocked island area;
- generator repair using earned resources;
- obvious visible world change after repair;
- area unlock;
- discovery of damaged robot **77**;
- genuine player-controlled continuation to another puzzle;
- Prototype Analytics Contract events.

The resource values are test fixtures, not a production economy.

## 14. Definition of Done

### Engineering DoD

- project opens/builds in the documented environment;
- three P0 variants are playable and validated at the required content minimum;
- deterministic rule tests exist for implemented core rules;
- level/config validation exists;
- telemetry contract can be validated on the intended paths;
- selected-core P1 path runs through reward -> repair -> island change -> unlock -> 77 -> next puzzle;
- at least one representative Android prototype build installs/runs before P1 evidence is accepted.

### Player-experience DoD

A fresh player without oral rule explanation can:

1. identify the first puzzle goal;
2. make meaningful input;
3. complete the first level or demonstrate clear rule understanding;
4. understand they received a reward;
5. understand at least one resource use;
6. spend it on generator repair;
7. perceive `puzzle -> reward -> repair -> world change` causality;
8. unlock an area;
9. discover 77;
10. understand that another puzzle is available.

### Product DoD

> The player voluntarily chooses to continue under the definition in `29_PROTOTYPE_PLAYTEST_PROTOCOL.md`.

If this behavior is not supported by P0/P1 evidence, the result is ITERATE/PIVOT/STOP rather than automatic CONTINUE.

## 15. Gate metrics

Authoritative initial thresholds are in `18_PRODUCT_GATES.md`.

Prototype reports must include enough evidence to interpret:

- unaided comprehension;
- voluntary next-puzzle rate;
- level duration;
- fail/retry/quit;
- invalid/wrong interaction patterns;
- help required;
- resource/repair/world-change comprehension;
- post-island voluntary continuation;
- 77 discovery comprehension/reaction.

Do not import D1/D7/D30 soft-launch metrics into P0/P1 as if they were prototype-session metrics.

## 16. Explicitly OUT of scope before P0 + P1 CONTINUE

Do not implement as production systems:

- IAP/store;
- real price points;
- rewarded/interstitial ads or mediation;
- Season Pass;
- Explorer Club subscription;
- production backend/microservices;
- production cloud save/auth/social;
- remote LiveOps platform;
- realtime multiplayer;
- large island map;
- dozens of characters;
- hundreds of props/decorations;
- production cinematics;
- other planets;
- ship gameplay layer;
- production-quality VFX/audio/content;
- large narrative pack;
- mass localization;
- expensive marketing production;
- speculative architecture for any of the above.

Minimal mocks/stubs are allowed only when they directly unblock P0/P1 testing.

## 17. Deliberately unresolved

These are not documentation defects:

- winning core mechanic;
- exact early difficulty curve;
- portrait vs landscape;
- exact mystery-reveal pacing;
- levels per story beat;
- real IAP prices;
- actual D1/D7/D30 results and validated market thresholds;
- production content velocity;
- optimal season cadence;
- final commercial title;
- multi-year content specifics.

Resolve cheap-to-test questions by experiment, then update Decision Log/specialized docs.

## 18. After successful P0 + P1

Only then begin Vertical Slice work such as:

- one production-quality island sector;
- finalizing UI language;
- 77 silhouette/animation;
- polished selected-core levels;
- one finished narrative beat/mystery clue;
- save/load prototype;
- reference-device profiling;
- carefully scoped platform-service sandboxes where the Roadmap requires them.

The next documentation baseline must be updated from actual playtest findings rather than additional speculative design.
