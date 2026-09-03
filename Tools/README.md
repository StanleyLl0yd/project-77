# Tools

Project-specific development, content validation, build-support and data-generation tools live here.

Prototype Phase rule: add tooling only when it directly supports Prototype 0.1, repeatable builds, testing, telemetry, or validation. Avoid building a large internal platform before the core/meta loop is proven.

`DomainSmoke` compiles and executes the Unity-independent puzzle/analytics tests under .NET.

`UnityScriptSmoke` compiles the Unity-facing runtime scripts against a deliberately tiny API stub. It catches ordinary C# compile/symbol regressions while Unity Build Automation is unavailable, but it is not a substitute for a real Unity Editor/cloud compile because the stub only covers the API surface Project 77 currently uses.

`p0_batch_report.py` validates exported P0 `*_metadata.json` + `*_events.jsonl` session pairs, enforces a single frozen build/commit/schema and stable per-variant level revisions, joins optional moderator records from `P0_MODERATION_TEMPLATE.csv`, and produces a gate-ready Markdown/JSON summary. Telemetry next-clicks are kept separate from the formal voluntary-continuation metric, which requires moderator/exclusion data from the playtest protocol.

Example:

```bash
python Tools/p0_batch_report.py PlaytestData/P0-001 \
  --moderation PlaytestData/P0-001/moderation.csv \
  --output PlaytestData/P0-001/report.md \
  --summary-json PlaytestData/P0-001/summary.json
```
