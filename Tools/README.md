# Tools

Project-specific development, content validation, build-support and data-generation tools live here.

Prototype Phase rule: add tooling only when it directly supports Prototype 0.1, repeatable builds, testing, telemetry, or validation. Avoid building a large internal platform before the core/meta loop is proven.

`DomainSmoke` compiles and executes the Unity-independent puzzle/analytics tests under .NET.

`UnityScriptSmoke` compiles the Unity-facing runtime scripts, including the Core bootstrap entry point, against a deliberately tiny UnityEngine API stub. It catches ordinary C# compile/symbol regressions while Unity Build Automation is unavailable, but it is not a substitute for a real Unity Editor/cloud compile because the stub only covers the API surface Project 77 currently uses.

`EditorScriptSmoke` extends the same idea to `Assets/Project77/Editor`, including the cloud Pre-Export bootstrap hook. It compiles against small UnityEditor/URP stubs and therefore checks source-level regressions only; actual Unity API behavior, asset serialization and Android build output still require a real Unity build when cloud credits are available.

`verify_android_artifact.py` performs repeatable APK/AAB acceptance checks after a real Android build exists. It validates ZIP integrity, the Project 77 ABI baseline, every ARM64 ELF `PT_LOAD` alignment for 16 KB page compatibility, and 16 KB ZIP data alignment for uncompressed APK native libraries. It also requires the expected APK/AAB signature structure marker by default, but that marker is not cryptographic certificate verification; use `apksigner`/`jarsigner` for certificate identity and signature validity.

Example:

```bash
python Tools/verify_android_artifact.py Project77.apk --json PlaytestData/artifacts/apk.json
python Tools/verify_android_artifact.py Project77.aab --json PlaytestData/artifacts/aab.json
```

`p0_order_plan.py` creates anonymous P0 playtest IDs and a counterbalanced A/B/C order. A complete six-participant block uses every permutation once, balancing all three variants across first/second/third position and all directed carryover pairs. Partial blocks keep each position count within one. Use the same generated `playtest_id` when one participant launches each assigned variant.

Example:

```bash
python Tools/p0_order_plan.py \
  --participants 12 \
  --prefix P0-001 \
  --output PlaytestData/P0-001/orders.csv \
  --summary-json PlaytestData/P0-001/orders.json
```

`p0_order_audit.py` cross-checks exported sessions and moderator `variant_order_index` values against that generated order plan. Wrong variant/position assignments, duplicate slots and unknown `playtest_id` values are hard errors; incomplete crossover or missing moderation positions remain explicit warnings rather than being silently invented.

`P0_MODERATION_TEMPLATE.csv` is the compact structured join table consumed by the reporting tools. `P0_SESSION_NOTES_TEMPLATE.md` is the companion per-session observation sheet for protocol details that should not be squeezed into the CSV: first gesture/timing, help interventions, hesitation/frustration, voluntary quit, verbatim comments and the post-session open questions. Keep observations separate from interpretation and keep both files anonymous.

`p0_freeze_manifest.py` creates the immutable batch-side freeze record required before external P0 testing. It binds one clean Git commit and build version to Unity/URP versions, event/metadata schemas, all 30 level IDs/revisions/content hashes, the governing P0 contracts, the preregistered quantitative gate plan, and—when using crossover testing—the exact order-plan file hash. Generated playtest data belongs under the ignored `PlaytestData/` folder, not in Git.

Example freeze flow after a real test APK exists for the current commit:

```bash
python Tools/p0_freeze_manifest.py generate \
  --batch-id P0-001 \
  --order-plan PlaytestData/P0-001/orders.csv \
  --output PlaytestData/P0-001/freeze.json

python Tools/p0_freeze_manifest.py verify PlaytestData/P0-001/freeze.json \
  --order-plan PlaytestData/P0-001/orders.csv
```

`p0_batch_report.py` validates exported P0 `*_metadata.json` + `*_events.jsonl` session pairs, enforces a single build/commit/schema and stable per-variant level revisions, joins optional moderator records from `P0_MODERATION_TEMPLATE.csv`, and produces a gate-ready Markdown/JSON summary. Telemetry next-clicks are kept separate from the formal voluntary-continuation metric, which requires moderator/exclusion data from the playtest protocol.

`p0_event_schema.py` mirrors the Prototype Analytics Contract in a provider-independent Python validator. Before gate metrics are accepted it re-validates every exported JSONL event envelope, required event-specific fields, integer bounds and canonical enum values, so malformed telemetry cannot quietly enter the report even if the producer-side C# validation regresses. CI also checks that its canonical event names, schema version and enum sets stay in lockstep with `PrototypeAnalytics.cs` and the analytics contract document.

`p0_event_audit.py` is the event-order/state-machine audit used before final P0 metrics are accepted. It detects impossible sequences such as overlapping attempts, bad retry indices, retry after completion, terminal events for the wrong attempt, continuation clicks without a matching offer, and a next level that does not match the clicked offer. Incomplete/crash-like endings are reported as warnings rather than silently converted into product failures.

`p0_gate_report.py` is the preferred final P0 batch command. It first rejects telemetry that does not match the freeze manifest's build, commit, Unity version, schema or level revisions, then validates the strict event schema, runs the event-sequence audit, verifies the frozen counterbalanced assignment plan when present, and delegates the formal metrics to `p0_batch_report.py`. It also reports operational diagnostics such as fresh-exposure sample sufficiency, first-level/full-set completion, continuation-offer reach, terminal/restarted/open attempts, attempt outcome rates and invalid interactions per attempt. These diagnostics do not automatically make the gate decision.

Example:

```bash
python Tools/p0_gate_report.py PlaytestData/P0-001 \
  --freeze PlaytestData/P0-001/freeze.json \
  --moderation PlaytestData/P0-001/moderation.csv \
  --order-plan PlaytestData/P0-001/orders.csv \
  --output PlaytestData/P0-001/report.md \
  --summary-json PlaytestData/P0-001/summary.json
```
