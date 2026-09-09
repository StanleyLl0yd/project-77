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

## Current core decision

Gate P0 has an owner-authorized **CONTINUE** decision. **Energy Routing** is the selected P1 core. The retained quantitative P0 evidence limitation is documented in `33_P0_GATE_DECISION.md`.

Path / Expedition Routing and Flow / Network Restoration remain available as P0/debug implementations; they are not the active P1 path.

## Active Prototype 0.1 flow

Current Gate P1 path:

`Energy Routing -> reward -> Scrap/Energy -> repair generator -> visible island change -> unlock area -> discover 77 -> genuine next-puzzle choice -> Gate P1`

The integrated P1 engineering loop is implemented. Active work is the frozen external P1 test and evidence collection; production scope remains blocked until Gate P1 CONTINUE.

Operational docs:

- `28_PROTOTYPE_01_SPEC.md` — implementation contract;
- `29_PROTOTYPE_PLAYTEST_PROTOCOL.md` — playtest procedure and voluntary-continue definition;
- `30_PROTOTYPE_ANALYTICS_CONTRACT.md` — canonical prototype event schema;
- `31_ENGINEERING_CONVENTIONS.md` — Unity/C# conventions;
- `32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md` — ordered tasks/DoD;
- `34_P1_EXTERNAL_PLAYTEST_PLAN.md` — preregistered Gate P1 hypothesis/environment/decision rule.

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
