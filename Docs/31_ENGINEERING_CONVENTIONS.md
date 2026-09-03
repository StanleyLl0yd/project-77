# Project 77 — Prototype Engineering Conventions

Status: **ACTIVE for Prototype 0.1**.

Purpose: give Codex/Unity a small set of implementation rules without turning the prototype into an architecture exercise.

## 1. Unity project layout

After bootstrap, use this default project-owned tree:

```text
Assets/Project77/
├── Core/
├── Puzzle/
├── Meta/
├── UI/
├── Content/
├── Analytics/
├── Platform/
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

Unity-generated `Assets`, `Packages`, and `ProjectSettings` belong in Git. Generated caches/build outputs do not.

Do not create empty architectural layers merely to match this diagram. A folder/module should exist because current code/content needs it.

## 2. Namespaces

Root namespace: `Project77`.

Examples:

- `Project77.Core`
- `Project77.Puzzle`
- `Project77.Puzzle.EnergyRouting`
- `Project77.Puzzle.PathExpeditionRouting`
- `Project77.Puzzle.FlowNetworkRestoration`
- `Project77.Meta`
- `Project77.Analytics`
- `Project77.Platform`

Keep namespaces aligned with responsibilities, not scene names.

## 3. Assembly-definition policy

Use `.asmdef` only where it improves compile/test isolation.

Prototype default may start with a small set such as:

- `Project77.Core` — deterministic/shared domain types with minimal Unity coupling;
- `Project77.Game` or equivalent — Unity presentation/orchestration;
- `Project77.Tests.EditMode`;
- `Project77.Tests.PlayMode` when needed.

Split variant assemblies only if dependencies/testability justify it. Do not create one assembly per folder.

## 4. Puzzle-domain rule

Puzzle rules should be deterministic for a given:

- level definition;
- initial state/seed if randomness exists;
- ordered input/actions.

Where practical, core puzzle-domain code should not depend on:

- MonoBehaviour lifecycle;
- scenes;
- rendering;
- animation;
- UI hierarchy;
- analytics SDKs.

UI/controllers translate player input into domain actions and render the resulting state. Do not place success/fail/game-rule logic directly in buttons, views, or animation callbacks unless it is strictly presentation logic.

## 5. Shared prototype contract

All three P0 variants should expose the smallest common runner contract needed by the shell, for example conceptually:

```text
load level definition
start attempt
apply player action
query current state
success/fail/active result
restart
```

Do not force unrelated variant mechanics into an over-generalized inheritance hierarchy. Prefer a small common interface plus variant-specific domain types.

## 6. Level/config format

Prototype levels must be data-driven and diffable.

Minimum shared envelope:

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

Rules:

- stable `id` never changes meaning silently;
- change `revision` when gameplay-affecting content changes;
- `variant` uses analytics canonical enum values;
- variant-specific `payload` is validated before play;
- invalid level definitions fail clearly in development/tests rather than being silently repaired;
- no generic remote-content platform is required for Prototype 0.1.

JSON is the default prototype interchange/authoring format unless an implementation spike proves a simpler Unity-native representation materially improves iteration. Any change must preserve stable ID/revision/variant semantics and analytics traceability.

## 7. Randomness

Avoid randomness unless it is part of the tested mechanic. If used, inject/store an explicit seed so a failed test/session can be reproduced.

## 8. Analytics boundary

Use the canonical contract in `30_PROTOTYPE_ANALYTICS_CONTRACT.md`.

Game/domain logic emits meaningful project events through a small project-owned interface. Provider/local-file details stay outside puzzle rules.

## 9. Prototype persistence

Do not implement production cloud save/economy persistence for P0. P1 may use the minimum local prototype state required to move from puzzle rewards to generator repair and 77 discovery.

It is acceptable to reset prototype progression between test sessions if the test build clearly supports that workflow.

## 10. No speculative frameworks

Do not add without a current demonstrated need:

- dependency-injection frameworks;
- service locator;
- global event bus;
- generic command framework;
- generic repository/data-access layer;
- custom ECS;
- custom serialization framework;
- production backend client;
- feature-flag platform;
- generic content CMS.

Simple constructor/reference wiring and explicit calls are preferred for the prototype.

## 11. Error handling

- Validate level/config at load/development time.
- Do not wrap every call in defensive `try/catch`.
- Catch where there is a concrete recovery action or boundary.
- Development failures should be loud and diagnosable.
- Do not hide invariant violations behind fallback behavior that corrupts test data.

## 12. Tests

P0 minimum:

- domain rule tests for each variant;
- deterministic/replayable behavior where applicable;
- level-definition validation tests;
- a happy-path and failure-path test per implemented core rule;
- analytics event-contract smoke test;
- Android build smoke test before external P1 playtest.

Add regression tests for discovered bugs before/with risky fixes when practical.

## 13. Code quality

- Optimize for minimum necessary complexity, not line count.
- Prefer explicit state over hidden global state.
- Minimize mutable shared state.
- Do not create wrappers/helpers that merely rename one call without useful semantics.
- Consolidate duplicated game rules, but do not abstract unrelated code solely for DRY.
- Comments are minimal, necessary, current, and English-only.
- Remove commented-out/dead code.

## 14. Prototype throwaway vs survivor code

Expected to be throwaway/reworkable:

- greybox visuals;
- temporary layout/presentation for rejected variants;
- variant-selection debug UI;
- moderator/debug controls;
- prototype-only local telemetry viewer.

Expected to survive if proven useful:

- deterministic winning puzzle-domain logic;
- level ID/revision/config semantics;
- validation/tests;
- analytics event contract/interface;
- minimal meta-loop domain model only after P1 succeeds.

Do not prematurely polish survivor candidates before the relevant gate passes.
