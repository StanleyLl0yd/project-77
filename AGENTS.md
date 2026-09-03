# Project 77 — Agent Operating Contract

This file is the repository-level operating contract for ChatGPT, Codex, and other coding agents working on Project 77. Keep it high-signal. Do not copy the whole GDD here.

## 1. Source of truth

When instructions conflict, use this order:

1. the project owner's current explicit instruction;
2. this root `AGENTS.md`;
3. `Docs/13_DECISION_LOG.md` for product/high-level decision state;
4. `Docs/28_PROTOTYPE_01_SPEC.md` for the active Prototype 0.1 implementation contract;
5. the relevant specialized document in `Docs/`;
6. `Docs/11_BACKLOG_IDEAS.md` only as uncommitted ideas.

`Docs/11_BACKLOG_IDEAS.md` is **not committed scope**. Never promote an idea to implementation merely because it appears there.

If a required decision is genuinely unresolved, keep it OPEN/PROVISIONAL and implement only what is necessary to test it. Do not silently invent a LOCKED answer.

## 2. Current phase

- Pre-production: **COMPLETE**.
- Active phase: **Prototype 0.1**.
- Working principle: **data over speculation**.
- Production scope may expand only after both Gate P0 and Gate P1 return **CONTINUE**.
- Active loop under test: `Puzzle -> Reward -> Island Change / Discovery -> Next Puzzle`.

Before coding, read `Docs/28_PROTOTYPE_01_SPEC.md`, `Docs/29_PROTOTYPE_PLAYTEST_PROTOCOL.md`, and `Docs/32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md`.

## 3. Locked product invariants

Do not change without an objective reason and an explicit Decision Log update:

- codename Project 77 and robot 77;
- macro arc: abandoned island -> underground complex -> ancient vessel -> space -> other planets -> world network;
- persistent island home;
- main story remains free;
- Android-first strategy with cross-platform core;
- Unity 6.3 LTS + C# + URP;
- prototype-first rule before production expansion.

## 4. Technical baseline

- Unity 6.3 LTS.
- C#.
- URP.
- Android-first; core logic remains platform-neutral.
- Minimum Android baseline: API 26.
- `targetSdk >= 36` according to the current release requirement documented by the project; re-check store requirements before release.
- `compileSdk` must be >= target and supported by the current Unity/Android toolchain.
- `arm64-v8a` is mandatory for Android release artifacts.
- AAB is the primary store release format.
- APK is allowed for development, playtests, and direct distribution where appropriate.
- All native dependencies and final 64-bit Android artifacts must be compatible with 16 KB memory pages.

## 5. Architecture boundaries

Gameplay/puzzle rules must not depend on:

- billing SDKs;
- ads SDKs;
- analytics providers;
- authentication providers;
- cloud-save providers;
- notifications;
- a specific app store.

Platform services live behind narrow interfaces/adapters only when the current scope needs them. Do not build production implementations for out-of-scope services during Prototype 0.1.

Do not add dependency-injection frameworks, service locators, global event buses, repository layers, generic plugin systems, or speculative abstractions merely for hypothetical future use.

## 6. Prototype discipline

Until P0 + P1 return CONTINUE:

- use placeholder graphics/UI freely;
- do not build production backend, IAP/store, ads, subscription, Season Pass, planet/ship gameplay, mass art, or mass content;
- build only what is required to test the active hypotheses;
- prefer small throwaway presentation code over premature production architecture;
- preserve reusable deterministic domain logic, validation, telemetry contracts, and tests where useful.

The build running is not sufficient. The product criterion is that an external tester understands the loop and voluntarily wants to continue.

## 7. Data-driven prototype content

Puzzle levels/configuration should be data-driven where this directly improves iteration, validation, and playtesting.

Use a small explicit schema with version, stable level ID, revision, prototype variant, and variant-specific payload. Do not build a generic content platform before it is needed.

## 8. Engineering conventions

Follow `Docs/31_ENGINEERING_CONVENTIONS.md`.

Default namespace: `Project77.*`.

Target project layout after Unity bootstrap:

```text
Assets/Project77/
  Core/
  Puzzle/
  Meta/
  UI/
  Content/
  Analytics/
  Platform/
  Tests/
```

Core puzzle rules should be deterministic and testable independently from MonoBehaviour, scenes, rendering, and UI as far as is practical. Do not place gameplay rules inside UI scripts.

Use `.asmdef` boundaries only when they materially improve isolation/testability; do not create one assembly per folder by default.

## 9. Testing and verification

During Prototype 0.1 prioritize:

- deterministic puzzle-rule tests;
- level/config validation;
- regression tests for fixed bugs;
- analytics-contract validation;
- Android build smoke tests.

Save/economy/purchase tests belong only when those systems enter the active scope.

Never claim a test, build, lint, static-analysis, or device check passed unless it actually ran.

For risky refactors without coverage, add the smallest useful regression test first.

## 10. Repository-wide audit/refactor rule

When asked for full audit, cleanup, optimization, simplification, or deep refactoring:

- inspect production code, tests, assets/resources, configs, build/CI, dependencies, docs, and platform integration before deleting anything;
- preserve externally observable behavior unless a bug fix is explicitly requested;
- optimize for **minimum necessary complexity**, not minimum line count;
- delete proven dead/obsolete/duplicated code;
- simplify objectively redundant control flow/state/validation;
- remove abstractions that have no demonstrated value;
- verify indirect/framework/reflection/serialization/config/build uses before calling code unused;
- if benefit or safety is uncertain, preserve the working behavior;
- make changes in coherent groups and verify after meaningful groups;
- perform a second cleanup pass after the first refactor pass;
- do not mix unrelated feature work into a refactor.

No code golf and no broad rewrite merely because another architecture is fashionable.

## 11. Security and secrets

Never commit:

- API keys;
- passwords;
- tokens;
- signing keys;
- keystores;
- private keys/certificates;
- production credentials;
- personal data;
- secrets in config or sample logs.

Treat anything shipped in a client build as discoverable by a user.

## 12. Comments

Code comments must be:

- minimal;
- necessary;
- current;
- written in English only;
- focused on non-obvious intent/invariants, not narrating obvious code.

Do not keep commented-out legacy code. Delete it; Git is the history.

## 13. Canonical app-icon artwork invariant

When the project owner provides a new PNG and explicitly identifies it as the replacement app icon, that exact PNG becomes the canonical source artwork.

The canonical source remains the original raster PNG. Without an explicit owner instruction, do **not** vectorize, trace, redraw, recreate, restyle, convert it to SVG/vector PDF/Android VectorDrawable/SF Symbol, create another vector master, or replace it with a redrawn version.

Do not overwrite the canonical PNG through automatic recompression/optimization.

Only necessary platform derivatives may be generated from it, such as resized PNGs, ICO, ICNS, or required raster/container outputs. Unless explicitly requested, derivatives must preserve the visible design: no crop, extra padding, color change, detail removal, restyling, or composition change.

A previously canonical icon remains canonical until the owner explicitly supplies a replacement PNG as the new app icon.

## 14. Documentation hygiene

- Update `Docs/13_DECISION_LOG.md` when a decision status changes.
- Keep `Docs/28_PROTOTYPE_01_SPEC.md` focused on the active phase.
- Keep specialized details in specialized documents; prefer links over duplicated long tables.
- `Docs/PROJECT_77_MASTER.md` is an index, not a generated duplicate of all documentation.
- Update `Docs/CHANGELOG.md` and `Docs/project77_manifest.json` for material documentation-baseline changes.
