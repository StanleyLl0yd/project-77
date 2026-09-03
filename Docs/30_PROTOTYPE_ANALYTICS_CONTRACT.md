# Project 77 — Prototype Analytics Contract

Status: **ACTIVE for Prototype 0.1 / Gate P0 + P1**.

This is the minimum strict event contract required to compare prototype variants and measure the core/meta loop. It is intentionally smaller than a production analytics platform.

Long-term KPI concepts remain in `09_ANALYTICS_KPI.md`. Gate thresholds remain in `18_PRODUCT_GATES.md`.

## 1. Contract rules

- Canonical event names and property meanings are versioned.
- Event emission is centralized behind a small prototype analytics interface; puzzle/domain logic must not depend on a vendor SDK.
- No unnecessary personal data, advertising IDs, contacts, email, name, precise location, or free-form user text.
- `playtest_id` is an anonymous test/cohort identifier, not a real-world identity.
- Timestamps are UTC Unix milliseconds.
- Durations are integer milliseconds unless explicitly stated otherwise.
- Enum values are lowercase snake_case.
- Unknown enum values are rejected in tests/validation rather than silently normalized.
- Event schema version for this document: **1**.

## 2. Common event envelope

Every event contains these fields. Fields marked nullable are still present with `null` when not applicable.

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `event_schema_version` | int | yes | `1` for this contract |
| `event_name` | string | yes | canonical event name |
| `timestamp_utc_ms` | int64 | yes | Unix epoch milliseconds |
| `session_id` | string | yes | random session UUID/string |
| `playtest_id` | string | yes | anonymous batch/test/cohort ID |
| `build_version` | string | yes | app build/version or commit-derived build ID |
| `prototype_variant` | enum | yes | variant value below |
| `level_id` | string/null | yes | stable level ID, null outside a level |
| `level_revision` | int/null | yes | content revision, null outside a level |
| `device_tier` | enum/null | yes | `low`, `mid`, `high`, `unknown` |
| `screen_orientation` | enum/null | yes | `portrait`, `landscape`, `unknown` |

Allowed `prototype_variant` values:

- `energy_routing`
- `path_expedition_routing`
- `flow_network_restoration`
- `selected_meta`
- `unknown`

For the meta-integrated winner, use `selected_meta` plus `core_variant` property where required so the winning mechanic remains identifiable.

## 3. Canonical events

### `prototype_start`

Emit exactly once when a prototype session reaches the first usable prototype screen.

Required properties:

| Property | Type | Allowed/Unit |
|---|---|---|
| `entry_point` | enum | `fresh_launch`, `restart`, `variant_select` |
| `core_variant` | enum/null | same core variant values; null if not selected |

### `tutorial_exposed`

Emit when the build displays an intended tutorial instruction/gesture cue.

Required:

- `tutorial_step_id`: string;
- `exposure_index`: int >= 1;
- `presentation_type`: enum `text`, `gesture`, `highlight`, `animation`, `other`.

Optional:

- `auto_advance_ms`: int/null.

### `level_start`

Emit once when a level becomes interactive for an attempt.

Required:

- `attempt_index`: int >= 1;
- `core_variant`: enum;
- `level_sequence_index`: int >= 1.

### `level_complete`

Emit once when the success condition becomes authoritative for that attempt.

Required:

- `attempt_index`: int;
- `duration_ms`: int >= 0;
- `valid_interaction_count`: int >= 0;
- `invalid_interaction_count`: int >= 0;
- `core_variant`: enum.

Optional:

- `move_count`: int/null;
- `path_action_count`: int/null.

### `level_fail`

Emit when the level enters a defined fail state; do not use for a simple invalid gesture.

Required:

- `attempt_index`: int;
- `duration_ms`: int >= 0;
- `fail_reason`: enum defined by the variant, with shared fallback `other`;
- `core_variant`: enum.

### `level_retry`

Emit when the player explicitly begins another attempt after fail/restart.

Required:

- `previous_attempt_index`: int;
- `new_attempt_index`: int;
- `retry_source`: enum `fail_screen`, `manual_restart`, `other`.

### `level_quit`

Emit when a started level is left without completion and without immediately starting a retry.

Required:

- `attempt_index`: int;
- `duration_ms`: int;
- `quit_destination`: enum `prototype_menu`, `app_exit`, `island`, `other`.

### `invalid_interaction`

Emit for a meaningful wrong/invalid interaction when practical. Do not emit for raw touch noise.

Required:

- `interaction_type`: enum `tap`, `drag`, `path_start`, `path_end`, `other`;
- `invalid_reason`: enum `out_of_bounds`, `blocked`, `wrong_target`, `rule_violation`, `no_effect`, `other`;
- `attempt_index`: int.

Optional:

- `board_element_id`: string/null.

### `reward_shown`

Emit when the post-level reward is visible and understandable to the player.

Required:

- `reward_id`: string;
- `reward_source`: enum `level_complete`, `meta_step`, `other`;
- `scrap_amount`: int >= 0;
- `energy_amount`: int >= 0.

### `reward_claimed`

Emit when the player explicitly claims/accepts the shown reward, or when an unavoidable auto-grant completes.

Required:

- `reward_id`: string;
- `claim_mode`: enum `explicit`, `auto`;
- `scrap_amount`: int >= 0;
- `energy_amount`: int >= 0.

### `resource_spend`

Emit when a prototype resource is actually deducted for a meta action.

Required:

- `resource_type`: enum `scrap`, `energy`, `other`;
- `amount`: int > 0;
- `sink_id`: string;
- `balance_after`: int >= 0.

### `generator_repair`

Emit once when the generator-repair action completes and its resulting state becomes visible/authoritative.

Required:

- `repair_stage`: int >= 1;
- `scrap_spent`: int >= 0;
- `energy_spent`: int >= 0;
- `time_since_session_start_ms`: int >= 0.

### `island_change`

Emit when a visible, test-relevant island state change is presented.

Required:

- `change_id`: string;
- `change_type`: enum `repair`, `power_on`, `reveal`, `unlock_visual`, `other`;
- `caused_by`: enum `generator_repair`, `area_unlock`, `story_step`, `other`.

### `area_unlock`

Emit once when the first blocked island area becomes available.

Required:

- `area_id`: string;
- `unlock_source`: enum `generator_repair`, `story_step`, `other`.

### `robot_77_discovered`

Emit once when the player reaches the intended 77 discovery reveal.

Required:

- `discovery_id`: string;
- `time_since_session_start_ms`: int >= 0;
- `levels_completed_before_discovery`: int >= 0.

### `next_puzzle_offered`

Emit when the player has a genuine, visible choice to start the next puzzle.

Required:

- `offer_context`: enum `post_level`, `post_reward`, `post_island_change`, `post_77_discovery`;
- `next_level_id`: string;
- `offer_sequence_index`: int >= 1.

### `next_puzzle_clicked`

Emit on the player's own activation of the continuation control.

Required:

- `offer_context`: same enum as above;
- `next_level_id`: string;
- `ms_since_offer`: int >= 0;
- `offer_sequence_index`: int.

`next_puzzle_clicked` is necessary but not by itself sufficient to label a playtest action "voluntary". Moderator prompting is recorded by the playtest protocol/report and can invalidate the voluntary classification.

### `session_end`

Emit on a normal prototype session end when possible. Do not fabricate it after an unobserved crash/kill.

Required:

- `duration_ms`: int >= 0;
- `levels_started`: int >= 0;
- `levels_completed`: int >= 0;
- `end_reason`: enum `user_exit`, `prototype_complete`, `moderator_end`, `app_background_timeout`, `other`.

Optional:

- `last_visible_step`: string/null.

## 4. Derived P0/P1 metrics

Derive rather than emit when possible:

- unaided first-level completion;
- time to first gesture;
- median level duration;
- fail/retry/quit rates;
- invalid interactions per attempt;
- next-puzzle click rate;
- `ms_since_offer` distribution;
- reward-to-spend conversion;
- repair completion rate;
- area unlock rate;
- 77 discovery rate.

`voluntary continuation rate` is finalized only after joining telemetry with playtest moderation records/exclusions from `29_PROTOTYPE_PLAYTEST_PROTOCOL.md`.

## 5. Validation requirements

Before an external playtest build:

- schema version is fixed and reported in the batch plan;
- all required fields are present;
- event names match this document exactly;
- enums validate;
- timestamps and durations use documented units;
- `level_id` + `level_revision` identify the actual content shown;
- one deterministic smoke test or event-contract test covers the main happy path;
- duplicate terminal events (`level_complete` twice, etc.) are prevented.

## 6. Storage/provider rule

Prototype 0.1 may use a simple local/in-memory/file-backed test sink or a lightweight analytics provider. Do not add production analytics infrastructure solely for the prototype.

The game/domain code sends typed events to a project-owned interface; provider-specific SDK calls stay in the adapter layer.
