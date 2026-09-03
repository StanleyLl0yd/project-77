# Tools

Project-specific development, content validation, build-support and data-generation tools live here.

Prototype Phase rule: add tooling only when it directly supports Prototype 0.1, repeatable builds, testing, telemetry, or validation. Avoid building a large internal platform before the core/meta loop is proven.

`DomainSmoke` compiles and executes the Unity-independent puzzle/analytics tests under .NET.

`UnityScriptSmoke` compiles the Unity-facing runtime scripts, including the Core bootstrap entry point, against a deliberately tiny UnityEngine API stub. It catches ordinary C# compile/symbol regressions while Unity Build Automation is unavailable, but it is not a substitute for a real Unity Editor/cloud compile because the stub only covers the API surface Project 77 currently uses.

`EditorScriptSmoke` extends the same idea to `Assets/Project77/Editor`, including the cloud Pre-Export bootstrap hook. It compiles against small UnityEditor/URP stubs and therefore checks source-level regressions only; actual Unity API behavior, asset serialization and Android build output still require a real Unity build when cloud credits are available.

`p0_freeze_manifest.py` creates the immutable batch-side freeze record required before external P0 testing. It binds one clean Git commit and build version to Unity/URP versions, event/metadata schemas, all 30 level IDs/revisions/content hashes, and the governing P0 contracts. Generated playtest data belongs under the ignored `PlaytestData/` folder, not in Git.

Example freeze flow after a real test APK exists for the current commit:

```bash
python Tools/p0_freeze_manifest.py generate \
  --batch-id P0-001 \
  --output PlaytestData/P0-001/freeze.json

python Tools/p0_freeze_manifest.py verify PlaytestData/P0-001/freeze.json
```

`p0_batch_report.py` validates exported P0 `*_metadata.json` + `*_events.jsonl` session pairs, enforces a single build/commit/schema and stable per-variant level revisions, joins optional moderator records from `P0_MODERATION_TEMPLATE.csv`, and produces a gate-ready Markdown/JSON summary. Telemetry next-clicks are kept separate from the formal voluntary-continuation metric, which requires moderator/exclusion data from the playtest protocol.

`p0_gate_report.py` is the preferred final P0 batch command. It first rejects telemetry that does not match the freeze manifest's build, commit, Unity version, schema or level revisions, then delegates metric/report generation to `p0_batch_report.py`.

Example:

```bash
python Tools/p0_gate_report.py PlaytestData/P0-001 \
  --freeze PlaytestData/P0-001/freeze.json \
  --moderation PlaytestData/P0-001/moderation.csv \
  --output PlaytestData/P0-001/report.md \
  --summary-json PlaytestData/P0-001/summary.json
```
