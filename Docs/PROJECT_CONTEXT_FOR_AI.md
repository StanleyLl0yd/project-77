# PROJECT 77 — CONTEXT FOR CHATGPT / CODEX

Compact context only. For coding-agent behavior, root `../AGENTS.md` is the operating contract.

## Source hierarchy

When instructions conflict:

1. project owner's current explicit instruction;
2. root `AGENTS.md`;
3. `13_DECISION_LOG.md` for product/high-level decision state;
4. `28_PROTOTYPE_01_SPEC.md` for active Prototype 0.1 scope;
5. relevant specialized docs;
6. `11_BACKLOG_IDEAS.md` only as uncommitted ideas.

Do not treat backlog ideas as committed features.

## Current phase

**Pre-production is complete. Active phase: Prototype 0.1.**

Do not expand into production art, large backend, store/monetization, ads, subscription, Season Pass, other planets, ship gameplay, or mass content production until Gate P0 + P1 return CONTINUE.

The prototype is not successful merely because it runs. The critical outcome is that an external player understands `puzzle -> reward -> island change/discovery` and voluntarily wants to continue.

Questions that can be cheaply tested remain OPEN/PROVISIONAL until evidence exists.

## Product invariants

- Project 77 is the internal codename, not a cleared commercial title.
- Companion/mascot: robot 77.
- Macro arc: abandoned island -> underground complex -> ancient vessel -> space -> other planets -> world network.
- Cosmic scale is deliberately hidden early; early marketing sells the island mystery.
- The island remains the player's permanent home.
- Main story is free.
- Monetization direction: Season Pass, optional subscription, cosmetics, convenience IAP, rewarded ads.
- No pay-to-win core, no paid story ending, no paid-loot-box launch dependency.
- Core session target is one-finger, roughly 30–90 seconds.
- Social is asynchronous first; launch excludes free-form chat/DM/comments/user-uploaded UGC.
- Intended product positioning: teens/adults (13+ direction), not child-directed.
- Product stages use CONTINUE / ITERATE / PIVOT / STOP gates; sunk cost is not a reason to continue.

## Current P0 mechanic hypotheses

All are PROVISIONAL until Gate P0:

- A — **Energy Routing** (current favorite).
- B — **Path / Expedition Routing**.
- C — **Flow / Network Restoration**.

`Signal Sequence` is retired as the current Prototype C definition in v0.5.

Initial P0 content rule: minimum 10 validated levels per variant; add more only when needed to answer a defined test question.

## Active Prototype 0.1 flow

P0:

`A/B/C greybox -> standardized fresh-tester playtests -> Gate P0 -> select/iterate/pivot/stop`

P1 after P0 CONTINUE:

`selected puzzle (10–20 integrated levels) -> reward -> Scrap/Energy test resources -> repair generator -> visible island change -> unlock area -> discover 77 -> genuine next-puzzle choice -> Gate P1`

Operational docs:

- `28_PROTOTYPE_01_SPEC.md` — implementation contract;
- `29_PROTOTYPE_PLAYTEST_PROTOCOL.md` — playtest procedure and voluntary-continue definition;
- `30_PROTOTYPE_ANALYTICS_CONTRACT.md` — canonical prototype event schema;
- `31_ENGINEERING_CONVENTIONS.md` — Unity/C# conventions;
- `32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md` — ordered tasks/DoD.

## Tech baseline

- Unity 6.3 LTS + C# + URP.
- Android-first; core remains platform-neutral.
- min Android API 26.
- targetSdk 36 baseline; compileSdk >= target and supported by current Unity toolchain.
- arm64-v8a mandatory for release; representative ARM64 Android path before accepting P1 evidence.
- AAB primary future store artifact; APK valid for prototype testing/direct install.
- 16 KB memory-page compatibility required for final native dependencies/artifacts.
- Gameplay/puzzle rules do not depend on store, ads, analytics vendor, auth, cloud save, notifications, or a specific store.
- Do not create production platform-service implementations in Prototype 0.1.

## Prototype engineering rules

- deterministic puzzle domain for level + seed(if any) + ordered actions;
- test puzzle rules independently from MonoBehaviour/rendering where practical;
- data-driven levels with stable ID + revision + variant + validated payload;
- namespace convention `Project77.*`;
- target project-owned tree under `Assets/Project77/`;
- no speculative DI framework/service locator/global event bus/content platform;
- placeholder UI/art is expected;
- minimal analytics only; no unnecessary PII.

## KPI authority

Prototype P0/P1 thresholds are in `18_PRODUCT_GATES.md`.

Soft-launch initial decision targets from Decision Log P-007 / Gate P4:

- D1 >= 30%;
- D7 8–12%+;
- D30 4–7%+;
- crash/ANR-free >99.5%.

The older 35% / 12–15% / 5% set is retired as an authoritative second target set.

## Long-term direction, not current implementation scope

Guest-first account, versioned saves, server validation for value/time, Remote Config rollback/killswitches, LiveOps, store providers, cloud/social, incident recovery, content velocity, localization, accessibility, and production performance budgets are documented constraints for later stages. Do not build them now unless the active gate requires a minimal stub.
