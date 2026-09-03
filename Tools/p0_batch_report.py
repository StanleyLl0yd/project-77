#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import json
import statistics
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Iterable

VARIANTS = (
    "energy_routing",
    "path_expedition_routing",
    "flow_network_restoration",
)
EVENT_SCHEMA_VERSION = 1
METADATA_SCHEMA_VERSION = 1
DEFAULT_CONTINUE_WINDOW_MS = 15_000

BOOL_TRUE = {"1", "true", "yes", "y"}
BOOL_FALSE = {"0", "false", "no", "n"}
UNKNOWN = {"", "unknown", "na", "n/a", "null"}

MODERATION_COLUMNS = (
    "session_id",
    "cohort",
    "variant_order_index",
    "tutorial_completed",
    "unaided_comprehension",
    "help_required",
    "voluntary_continuation",
    "exclude_tutorial",
    "exclude_unaided",
    "exclude_voluntary",
    "exclusion_reason",
    "moderator_id",
)


class BatchError(ValueError):
    pass


@dataclass(frozen=True)
class SessionData:
    metadata_path: Path
    events_path: Path
    metadata: dict[str, Any]
    events: list[dict[str, Any]]

    @property
    def session_id(self) -> str:
        return str(self.metadata["session_id"])

    @property
    def variant(self) -> str:
        return str(self.metadata["prototype_variant"])


@dataclass(frozen=True)
class ModerationRecord:
    session_id: str
    cohort: str
    variant_order_index: int | None
    tutorial_completed: bool | None
    unaided_comprehension: bool | None
    help_required: bool | None
    voluntary_continuation: bool | None
    exclude_tutorial: bool
    exclude_unaided: bool
    exclude_voluntary: bool
    exclusion_reason: str
    moderator_id: str


def _bool_or_none(raw: str, field: str) -> bool | None:
    value = (raw or "").strip().lower()
    if value in BOOL_TRUE:
        return True
    if value in BOOL_FALSE:
        return False
    if value in UNKNOWN:
        return None
    raise BatchError(f"{field} must be yes/no/unknown, got {raw!r}")


def _required_bool(raw: str, field: str) -> bool:
    value = _bool_or_none(raw, field)
    if value is None:
        raise BatchError(f"{field} must be yes/no")
    return value


def load_moderation(path: Path | None) -> dict[str, ModerationRecord]:
    if path is None:
        return {}
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        if reader.fieldnames is None:
            raise BatchError("moderation CSV has no header")
        missing = [name for name in MODERATION_COLUMNS if name not in reader.fieldnames]
        if missing:
            raise BatchError("moderation CSV missing columns: " + ", ".join(missing))

        records: dict[str, ModerationRecord] = {}
        for line_no, row in enumerate(reader, start=2):
            session_id = (row["session_id"] or "").strip()
            if not session_id:
                raise BatchError(f"moderation row {line_no}: session_id is required")
            if session_id in records:
                raise BatchError(f"moderation row {line_no}: duplicate session_id {session_id!r}")

            cohort = (row["cohort"] or "").strip().lower()
            if cohort not in {"fresh", "returning"}:
                raise BatchError(f"moderation row {line_no}: cohort must be fresh or returning")

            order_text = (row["variant_order_index"] or "").strip()
            order = None
            if order_text:
                try:
                    order = int(order_text)
                except ValueError as exc:
                    raise BatchError(
                        f"moderation row {line_no}: variant_order_index must be an integer"
                    ) from exc
                if order < 1:
                    raise BatchError(
                        f"moderation row {line_no}: variant_order_index must be >= 1"
                    )

            records[session_id] = ModerationRecord(
                session_id=session_id,
                cohort=cohort,
                variant_order_index=order,
                tutorial_completed=_bool_or_none(row["tutorial_completed"], "tutorial_completed"),
                unaided_comprehension=_bool_or_none(row["unaided_comprehension"], "unaided_comprehension"),
                help_required=_bool_or_none(row["help_required"], "help_required"),
                voluntary_continuation=_bool_or_none(row["voluntary_continuation"], "voluntary_continuation"),
                exclude_tutorial=_required_bool(row["exclude_tutorial"], "exclude_tutorial"),
                exclude_unaided=_required_bool(row["exclude_unaided"], "exclude_unaided"),
                exclude_voluntary=_required_bool(row["exclude_voluntary"], "exclude_voluntary"),
                exclusion_reason=(row["exclusion_reason"] or "").strip(),
                moderator_id=(row["moderator_id"] or "").strip(),
            )
        return records


def _load_json(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise BatchError(f"cannot parse JSON {path}: {exc}") from exc
    if not isinstance(value, dict):
        raise BatchError(f"{path} must contain one JSON object")
    return value


def _load_jsonl(path: Path) -> list[dict[str, Any]]:
    events: list[dict[str, Any]] = []
    try:
        lines = path.read_text(encoding="utf-8-sig").splitlines()
    except OSError as exc:
        raise BatchError(f"cannot read {path}: {exc}") from exc
    for line_no, line in enumerate(lines, start=1):
        if not line.strip():
            continue
        try:
            value = json.loads(line)
        except json.JSONDecodeError as exc:
            raise BatchError(f"{path}:{line_no}: invalid JSON: {exc}") from exc
        if not isinstance(value, dict):
            raise BatchError(f"{path}:{line_no}: event must be an object")
        events.append(value)
    if not events:
        raise BatchError(f"{path}: no events")
    return events


def discover_sessions(data_dir: Path) -> list[SessionData]:
    metadata_paths = sorted(data_dir.rglob("*_metadata.json"))
    if not metadata_paths:
        raise BatchError(f"no *_metadata.json files under {data_dir}")

    sessions: list[SessionData] = []
    seen_session_ids: set[str] = set()
    for metadata_path in metadata_paths:
        stem = metadata_path.name[:-len("_metadata.json")]
        events_path = metadata_path.with_name(stem + "_events.jsonl")
        if not events_path.is_file():
            raise BatchError(f"missing matching events file for {metadata_path.name}")
        metadata = _load_json(metadata_path)
        events = _load_jsonl(events_path)
        session = SessionData(metadata_path, events_path, metadata, events)
        if session.session_id in seen_session_ids:
            raise BatchError(f"duplicate session_id across metadata: {session.session_id}")
        seen_session_ids.add(session.session_id)
        validate_session(session)
        sessions.append(session)

    validate_batch_freeze(sessions)
    return sessions


def _require(metadata: dict[str, Any], name: str, path: Path) -> Any:
    if name not in metadata:
        raise BatchError(f"{path}: missing {name}")
    return metadata[name]


def validate_session(session: SessionData) -> None:
    m = session.metadata
    path = session.metadata_path

    if _require(m, "metadata_schema_version", path) != METADATA_SCHEMA_VERSION:
        raise BatchError(f"{path}: unsupported metadata_schema_version")
    if _require(m, "event_schema_version", path) != EVENT_SCHEMA_VERSION:
        raise BatchError(f"{path}: unsupported event_schema_version")
    for name in (
        "session_id", "playtest_id", "build_version", "commit_sha",
        "app_version", "unity_version", "prototype_variant",
        "device_model", "operating_system", "screen_orientation",
        "session_started_utc", "levels",
    ):
        _require(m, name, path)

    if not isinstance(m["session_id"], str) or not m["session_id"].strip():
        raise BatchError(f"{path}: session_id is required")
    if m["prototype_variant"] not in VARIANTS:
        raise BatchError(f"{path}: unsupported prototype_variant {m['prototype_variant']!r}")
    if not isinstance(m["levels"], list) or len(m["levels"]) != 10:
        raise BatchError(f"{path}: P0 metadata must contain exactly 10 level revisions")

    level_map: dict[str, int] = {}
    for record in m["levels"]:
        if not isinstance(record, dict):
            raise BatchError(f"{path}: level manifest entries must be objects")
        level_id = record.get("id")
        revision = record.get("revision")
        if not isinstance(level_id, str) or not level_id:
            raise BatchError(f"{path}: level manifest id is required")
        if not isinstance(revision, int) or isinstance(revision, bool) or revision < 1:
            raise BatchError(f"{path}: level revision for {level_id!r} must be >= 1")
        if level_id in level_map:
            raise BatchError(f"{path}: duplicate level id {level_id!r}")
        level_map[level_id] = revision

    prototype_start_count = 0
    terminal_attempts: set[tuple[str, int, int]] = set()
    last_timestamp = -1
    for index, event in enumerate(session.events, start=1):
        prefix = f"{session.events_path}:{index}"
        if event.get("event_schema_version") != EVENT_SCHEMA_VERSION:
            raise BatchError(f"{prefix}: unsupported event_schema_version")
        if event.get("session_id") != m["session_id"]:
            raise BatchError(f"{prefix}: session_id does not match metadata")
        if event.get("playtest_id") != m["playtest_id"]:
            raise BatchError(f"{prefix}: playtest_id does not match metadata")
        if event.get("build_version") != m["build_version"]:
            raise BatchError(f"{prefix}: build_version does not match metadata")
        if event.get("prototype_variant") != m["prototype_variant"]:
            raise BatchError(f"{prefix}: prototype_variant does not match metadata")
        timestamp = event.get("timestamp_utc_ms")
        if not isinstance(timestamp, int) or isinstance(timestamp, bool) or timestamp < 0:
            raise BatchError(f"{prefix}: timestamp_utc_ms must be a non-negative integer")
        if timestamp < last_timestamp:
            raise BatchError(f"{prefix}: events are not timestamp-ordered")
        last_timestamp = timestamp

        level_id = event.get("level_id")
        revision = event.get("level_revision")
        if level_id is None or revision is None:
            if not (level_id is None and revision is None):
                raise BatchError(f"{prefix}: level_id and level_revision must both be null or present")
        else:
            if level_id not in level_map:
                raise BatchError(f"{prefix}: unknown level_id {level_id!r}")
            if level_map[level_id] != revision:
                raise BatchError(
                    f"{prefix}: level revision mismatch for {level_id!r}: "
                    f"{revision!r} != {level_map[level_id]!r}"
                )

        event_name = event.get("event_name")
        if event_name == "prototype_start":
            prototype_start_count += 1
        if event_name in {"level_complete", "level_fail", "level_quit"}:
            attempt = event.get("attempt_index")
            if not isinstance(attempt, int) or isinstance(attempt, bool) or attempt < 1:
                raise BatchError(f"{prefix}: terminal event has invalid attempt_index")
            if level_id is None:
                raise BatchError(f"{prefix}: terminal event requires a level")
            key = (str(level_id), int(revision), attempt)
            if key in terminal_attempts:
                raise BatchError(f"{prefix}: duplicate terminal event for {key}")
            terminal_attempts.add(key)

    if prototype_start_count != 1:
        raise BatchError(
            f"{session.events_path}: expected exactly one prototype_start, found {prototype_start_count}"
        )


def _manifest_key(session: SessionData) -> tuple[tuple[str, int], ...]:
    return tuple(
        (str(item["id"]), int(item["revision"]))
        for item in session.metadata["levels"]
    )


def validate_batch_freeze(sessions: list[SessionData]) -> None:
    builds = {str(s.metadata["build_version"]) for s in sessions}
    commits = {str(s.metadata["commit_sha"]) for s in sessions}
    schemas = {int(s.metadata["event_schema_version"]) for s in sessions}
    if len(builds) != 1:
        raise BatchError("batch mixes build_version values: " + ", ".join(sorted(builds)))
    if len(commits) != 1:
        raise BatchError("batch mixes commit_sha values: " + ", ".join(sorted(commits)))
    if len(schemas) != 1:
        raise BatchError("batch mixes event schema versions")

    manifests: dict[str, tuple[tuple[str, int], ...]] = {}
    for session in sessions:
        current = _manifest_key(session)
        previous = manifests.setdefault(session.variant, current)
        if previous != current:
            raise BatchError(f"batch mixes level revisions inside variant {session.variant}")


def _rate(values: Iterable[bool]) -> dict[str, Any]:
    items = list(values)
    return {
        "numerator": sum(1 for value in items if value),
        "denominator": len(items),
        "rate": None if not items else sum(1 for value in items if value) / len(items),
    }


def _moderation_rate(
    sessions: list[SessionData],
    moderation: dict[str, ModerationRecord],
    value_name: str,
    exclude_name: str,
) -> dict[str, Any]:
    values: list[bool] = []
    missing = 0
    excluded = 0
    for session in sessions:
        record = moderation.get(session.session_id)
        if record is None or record.cohort != "fresh":
            continue
        value = getattr(record, value_name)
        excluded_flag = getattr(record, exclude_name)
        if excluded_flag:
            excluded += 1
            continue
        if value is None:
            missing += 1
            continue
        values.append(value)
    result = _rate(values)
    result["missing"] = missing
    result["excluded"] = excluded
    return result


def _event_count(session: SessionData, event_name: str) -> int:
    return sum(1 for event in session.events if event.get("event_name") == event_name)


def summarize_variant(
    sessions: list[SessionData],
    moderation: dict[str, ModerationRecord],
) -> dict[str, Any]:
    fresh_sessions = [
        session for session in sessions
        if moderation.get(session.session_id) is not None
        and moderation[session.session_id].cohort == "fresh"
    ]
    durations = [
        int(event["duration_ms"])
        for session in sessions
        for event in session.events
        if event.get("event_name") == "level_complete"
        and isinstance(event.get("duration_ms"), int)
        and not isinstance(event.get("duration_ms"), bool)
    ]
    offer_sessions = [
        session for session in sessions if _event_count(session, "next_puzzle_offered") > 0
    ]
    clicked_sessions = [
        session for session in offer_sessions
        if any(
            event.get("event_name") == "next_puzzle_clicked"
            and isinstance(event.get("ms_since_offer"), int)
            and event["ms_since_offer"] <= DEFAULT_CONTINUE_WINDOW_MS
            for event in session.events
        )
    ]
    help_values = [
        moderation[session.session_id].help_required
        for session in fresh_sessions
        if moderation[session.session_id].help_required is not None
    ]

    event_counts = {
        name: sum(_event_count(session, name) for session in sessions)
        for name in (
            "level_start", "level_complete", "level_fail",
            "level_retry", "level_quit", "invalid_interaction",
            "next_puzzle_offered", "next_puzzle_clicked",
        )
    }

    return {
        "sessions": len(sessions),
        "fresh_sessions_with_moderation": len(fresh_sessions),
        "tutorial_completion": _moderation_rate(
            sessions, moderation, "tutorial_completed", "exclude_tutorial"
        ),
        "unaided_comprehension": _moderation_rate(
            sessions, moderation, "unaided_comprehension", "exclude_unaided"
        ),
        "voluntary_continuation": _moderation_rate(
            sessions, moderation, "voluntary_continuation", "exclude_voluntary"
        ),
        "help_required": _rate(value for value in help_values if value is not None),
        "telemetry_continue_within_15s": {
            "numerator": len(clicked_sessions),
            "denominator": len(offer_sessions),
            "rate": None if not offer_sessions else len(clicked_sessions) / len(offer_sessions),
            "formal_voluntary_metric": False,
        },
        "median_completed_level_ms": None if not durations else int(statistics.median(durations)),
        "event_counts": event_counts,
    }


def summarize_batch(
    sessions: list[SessionData],
    moderation: dict[str, ModerationRecord],
) -> dict[str, Any]:
    session_ids = {session.session_id for session in sessions}
    unknown_moderation = sorted(set(moderation) - session_ids)
    if unknown_moderation:
        raise BatchError(
            "moderation CSV references unknown session_id values: "
            + ", ".join(unknown_moderation)
        )

    grouped = {
        variant: [session for session in sessions if session.variant == variant]
        for variant in VARIANTS
    }
    first = sessions[0]
    return {
        "event_schema_version": int(first.metadata["event_schema_version"]),
        "build_version": str(first.metadata["build_version"]),
        "commit_sha": str(first.metadata["commit_sha"]),
        "sessions_total": len(sessions),
        "variants": {
            variant: summarize_variant(grouped[variant], moderation)
            for variant in VARIANTS if grouped[variant]
        },
        "missing_moderation_session_ids": sorted(
            session.session_id for session in sessions if session.session_id not in moderation
        ),
    }


def _pct(metric: dict[str, Any]) -> str:
    if metric["rate"] is None:
        return "n/a"
    return f"{metric['rate'] * 100:.1f}% ({metric['numerator']}/{metric['denominator']})"


def _gate(metric: dict[str, Any], minimum: float) -> str:
    rate = metric["rate"]
    if rate is None:
        return "UNAVAILABLE"
    return "PASS" if rate >= minimum else "BELOW TARGET"


def render_report(summary: dict[str, Any]) -> str:
    lines = [
        "# Project 77 — P0 Batch Report",
        "",
        "Decision remains **UNDECIDED** until quantitative results are joined with moderator notes and qualitative evidence.",
        "",
        f"Build / commit: `{summary['build_version']}` / `{summary['commit_sha']}`",
        f"Event schema version: {summary['event_schema_version']}",
        f"Sessions loaded: {summary['sessions_total']}",
        "",
    ]

    for variant, data in summary["variants"].items():
        lines.extend([
            f"## {variant}",
            "",
            f"- Sessions: {data['sessions']}",
            f"- Fresh sessions with moderation: {data['fresh_sessions_with_moderation']}",
            f"- Tutorial completion: {_pct(data['tutorial_completion'])} — "
            f"{_gate(data['tutorial_completion'], 0.85)} vs P0 >=85% target",
            f"- Unaided comprehension: {_pct(data['unaided_comprehension'])} "
            "(reported; gate document has no separate numeric threshold)",
            f"- Voluntary continuation: {_pct(data['voluntary_continuation'])} — "
            f"{_gate(data['voluntary_continuation'], 0.70)} vs P0 >=70% target",
            f"- Telemetry next-click within 15 s: {_pct(data['telemetry_continue_within_15s'])} "
            "(diagnostic only; not automatically voluntary)",
            f"- Help required: {_pct(data['help_required'])}",
        ])
        median_ms = data["median_completed_level_ms"]
        if median_ms is None:
            lines.append("- Median completed level duration: n/a")
        else:
            seconds = median_ms / 1000
            duration_gate = "PASS" if 30 <= seconds <= 90 else "OUTSIDE 30–90 s TARGET"
            lines.append(f"- Median completed level duration: {seconds:.1f} s — {duration_gate}")
        counts = data["event_counts"]
        lines.append(
            "- Events: "
            f"start {counts['level_start']}, complete {counts['level_complete']}, "
            f"fail {counts['level_fail']}, retry {counts['level_retry']}, "
            f"quit {counts['level_quit']}, invalid {counts['invalid_interaction']}"
        )
        lines.append("")

    missing = summary["missing_moderation_session_ids"]
    if missing:
        lines.extend([
            "## Data limitations",
            "",
            f"- Missing moderation records for {len(missing)} session(s): "
            + ", ".join(f"`{session_id}`" for session_id in missing),
            "- Formal unaided/voluntary rates exclude those sessions until moderation data is joined.",
            "",
        ])

    lines.extend([
        "## Qualitative findings",
        "",
        "- Major confusion points:",
        "- Repeated spontaneous comments:",
        "- Observed frustration/hesitation patterns:",
        "",
        "## Gate decision",
        "",
        "Decision: **UNDECIDED**",
        "",
        "Reason against pre-registered gate:",
        "",
        "Next experiment:",
        "",
    ])
    return "\n".join(lines)


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Validate a frozen Project 77 P0 telemetry batch and generate a gate-ready summary."
    )
    parser.add_argument(
        "data_dir",
        type=Path,
        help="Directory containing *_metadata.json + *_events.jsonl pairs",
    )
    parser.add_argument(
        "--moderation",
        type=Path,
        help="CSV using Tools/P0_MODERATION_TEMPLATE.csv columns",
    )
    parser.add_argument("--output", type=Path, help="Markdown report output path")
    parser.add_argument(
        "--summary-json",
        type=Path,
        help="Optional machine-readable summary output",
    )
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    try:
        sessions = discover_sessions(args.data_dir)
        moderation = load_moderation(args.moderation)
        summary = summarize_batch(sessions, moderation)
        report = render_report(summary)
    except BatchError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1

    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(report + "\n", encoding="utf-8")
    else:
        print(report)

    if args.summary_json:
        args.summary_json.parent.mkdir(parents=True, exist_ok=True)
        args.summary_json.write_text(
            json.dumps(summary, indent=2, sort_keys=True) + "\n",
            encoding="utf-8",
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
