# PROJECT 77 — CONTEXT FOR CHATGPT / CODEX

Use this file as authoritative compact context when continuing Project 77 in another AI session.


## Current phase

**Pre-production is complete. Active phase: Prototype 0.1.**

The active implementation contract is `28_PROTOTYPE_01_SPEC.md`.

Do not expand into production art, large backend, store/monetization, Season Pass, other planets, ship systems or mass content production until Gate P0 + P1 return CONTINUE.

The prototype is not successful merely because it runs. The critical product outcome is that an external player understands `puzzle -> reward -> island change/discovery` and voluntarily wants to continue.

Questions such as the winning core mechanic, orientation, exact difficulty, reveal pacing, prices, real retention and content velocity are intentionally unresolved until tests/data exist.

## Product

Project 77 is a mobile-first F2P hybrid-casual puzzle adventure with a persistent customizable home base, narrative mystery, collecting and LiveOps.

## Core fantasy

The player arrives on an abandoned island, restores it and finds a damaged robot marked 77. What initially looks like a small island restoration mystery gradually reveals an underground research complex, an ancient non-human vessel, other planets and finally a network of worlds.

The defining principle is:

> Every time the player thinks they understand the scale of the game, reveal that the world is much larger.

## Locked decisions

- Project 77 is the internal codename, NOT a cleared commercial title.
- Companion/mascot: robot 77.
- Macro arc: abandoned island -> underground mystery -> ancient ship -> space -> planets -> world network.
- Do not advertise the cosmic scale immediately; early marketing sells the island mystery.
- The island remains the player's permanent home forever.
- Main story is free.
- Monetization: Season Pass, optional subscription, cosmetics, convenience purchases, rewarded ads.
- No pay-to-win core, no mandatory gacha, no paid story ending.
- Core play session should be one-finger and ~30–90 seconds.
- Social is asynchronous first; no realtime multiplayer requirement for MVP.
- LiveOps/remote config are first-class architecture concerns.
- Intended audience direction: teens/adults (13+ product positioning), not child-directed.
- Launch social has no free-form chat, DM, comments or user-uploaded UGC.
- Product stages use explicit CONTINUE/ITERATE/PIVOT/STOP gates; sunk cost is not a reason to continue.
- Paid randomized loot boxes are not part of launch monetization.
- Pre-production is complete; current work is Prototype 0.1.
- Data beats speculative design for questions that can be cheaply tested.
- Prototype scope is frozen until P0/P1 CONTINUE.

## Current core gameplay hypothesis

Prototype A: Energy Routing.

Player connects sources and destinations on a compact board while respecting obstacles/constraints. The same underlying mechanic is themed as restoring island power, radios, labs, ship systems and ancient gates.

This mechanic is NOT locked until tested against at least two alternatives.

## Three loops

Micro: puzzle -> reward.
Session: several puzzles -> resources -> visible island/story action.
Long-term: restore -> discover -> expand -> collect -> reveal a larger world.

## Key narrative rule

Avoid exposition dumps. Deliver story through environment, short dialogue, strange items, logs, signals and glitches in 77.

Robot 77 begins as a damaged service robot but is likely tied to the ancient network. This origin is provisional, not fully locked.

## Island

Persistent home and visual history of the account. Important achievements return as physical trophies, creatures, plants or structures on the island.

## Monetization rules

Free users can reach all main story/world content. Paid value is beauty, collection, convenience, pace and extra seasonal rewards.

Rewarded ads are preferred. Forced ads, if used, must be light, capped, never interrupt major narrative beats and removable.

## Tech

Locked production baseline: Unity 6.3 LTS, C#, URP, Android-first, GitHub repository/CI. Core code must remain cross-platform and platform-neutral. Store billing, ads, analytics, cloud save, authentication and notifications sit behind provider interfaces/adapters. Android uses separate Google Play and RuStore implementations as required. Main Android release artifact is AAB; APK is for testing/direct install. Initial product baseline is API 26+ and arm64-v8a required. Content, economy, events and offers should be data-driven/remote configurable.

First milestone is Project 77 Prototype 0.1: 10–20 short puzzle levels -> resources -> repair generator -> unlock island area -> discover robot 77. Do not build production art, a large backend or monetization stack before the core/meta loop is proven.

## Source of truth

When a conflict exists, consult `13_DECISION_LOG.md`, then the specialized documents in this package. Ideas in `11_BACKLOG_IDEAS.md` are not committed features.


## Production readiness rules

- Guest-first account, stable Player ID, optional binding, versioned saves and migrations.
- Premium currency, purchases, subscriptions, valuable event rewards/social state and meaningful timers are server-authoritative or server-validated.
- Core gameplay may work offline; server-authoritative actions reconcile online. Device clock is not trusted for economic timers.
- Remote Config has built-in defaults, Development/Staging/Production environments, versioning, rollback and kill switches.
- As of 2026-09-02 Google Play target baseline is API 36+; 64-bit native stack must support 16 KB page sizes.
- Performance/stability have numeric budgets and low/mid/high reference-device testing.
- LiveOps cadence is set only after measured content velocity including QA/localization/maintenance.
- Incident recovery (save/purchase/config/event/backend) must exist before soft launch.
- Localization keys and accessibility baseline start before production scaling.

See documents 18–27 for operational specifications.
