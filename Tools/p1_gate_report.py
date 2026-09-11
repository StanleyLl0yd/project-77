#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import json
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Iterable

import p0_event_schema
import p1_event_audit
import p1_freeze_manifest as freeze

BOOL_TRUE = {"1", "true", "yes", "y"}
BOOL_FALSE = {"0", "false", "no", "n"}
UNKNOWN = {"", "unknown", "na", "n/a", "null"}

MODERATION_COLUMNS = (
    "session_id",
    "cohort",
    "resource_comprehension",
    "puzzle_reward_comprehension",
    "repair_world_change_comprehension",
    "robot_77_remembered",
    "puzzle_island_connected",
    "island_increased_desire",
    "post_island_voluntary_continuation",
    "help_required",
    "exclude_resource",
    "exclude_repair",
    "exclude_voluntary",
    "exclusion_reason",
    "moderator_id",
)


class P1ReportError(ValueError):
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


@dataclass(frozen=True)
class ModerationRecord:
    session_id: str
    cohort: str
    resource_comprehension: bool | None
    puzzle_reward_comprehension: bool | None
    repair_world_change_comprehension: bool | None
    robot_77_remembered: bool | None
    puzzle_island_connected: bool | None
    island_increased_desire: bool | None
    post_island_voluntary_continuation: bool | None
    help_required: bool | None
    exclude_resource: bool
    exclude_repair: bool
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
    raise P1ReportError(f"{field} must be yes/no/unknown, got {raw!r}")


def _required_bool(raw: str, field: str) -> bool:
    value = _bool_or_none(raw, field)
    if value is None:
        raise P1ReportError(f"{field} must be yes/no")
    return value


def load_moderation(path: Path | None) -> dict[str, ModerationRecord]:
    if path is None:
        return {}
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        if reader.fieldnames is None:
            raise P1ReportError("moderation CSV has no header")
        missing = [column for column in MODERATION_COLUMNS if column not in reader.fieldnames]
        if missing:
            raise P1ReportError("moderation CSV missing columns: " + ", ".join(missing))

        result: dict[str, ModerationRecord] = {}
        for line_no, row in enumerate(reader, start=2):
            session_id = (row["session_id"] or "").strip()
            if not session_id:
                raise P1ReportError(f"moderation row {line_no}: session_id is required")
            if session_id in result:
                raise P1ReportError(f"moderation row {line_no}: duplicate session_id {session_id!r}")
            cohort = (row["cohort"] or "").strip().lower()
            if cohort not in {"fresh", "returning"}:
                raise P1ReportError(f"moderation row {line_no}: cohort must be fresh or returning")

            exclude_resource = _required_bool(row["exclude_resource"], "exclude_resource")
            exclude_repair = _required_bool(row["exclude_repair"], "exclude_repair")
            exclude_voluntary = _required_bool(row["exclude_voluntary"], "exclude_voluntary")
            exclusion_reason = (row["exclusion_reason"] or "").strip()
            if (exclude_resource or exclude_repair or exclude_voluntary) and not exclusion_reason:
                raise P1ReportError(
                    f"moderation row {line_no}: exclusion_reason is required when any exclusion flag is yes"
                )
            moderator_id = (row["moderator_id"] or "").strip()
            if not moderator_id:
                raise P1ReportError(f"moderation row {line_no}: moderator_id is required")

            result[session_id] = ModerationRecord(
                session_id=session_id,
                cohort=cohort,
                resource_comprehension=_bool_or_none(row["resource_comprehension"], "resource_comprehension"),
                puzzle_reward_comprehension=_bool_or_none(row["puzzle_reward_comprehension"], "puzzle_reward_comprehension"),
                repair_world_change_comprehension=_bool_or_none(
                    row["repair_world_change_comprehension"], "repair_world_change_comprehension"
                ),
                robot_77_remembered=_bool_or_none(row["robot_77_remembered"], "robot_77_remembered"),
                puzzle_island_connected=_bool_or_none(row["puzzle_island_connected"], "puzzle_island_connected"),
                island_increased_desire=_bool_or_none(row["island_increased_desire"], "island_increased_desire"),
                post_island_voluntary_continuation=_bool_or_none(
                    row["post_island_voluntary_continuation"], "post_island_voluntary_continuation"
                ),
                help_required=_bool_or_none(row["help_required"], "help_required"),
                exclude_resource=exclude_resource,
                exclude_repair=exclude_repair,
                exclude_voluntary=exclude_voluntary,
                exclusion_reason=exclusion_reason,
                moderator_id=moderator_id,
            )
        return result


def _load_json(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise P1ReportError(f"cannot parse JSON {path}: {exc}") from exc
    if not isinstance(value, dict):
        raise P1ReportError(f"{path}: expected JSON object")
    return value


def _load_jsonl(path: Path) -> list[dict[str, Any]]:
    events: list[dict[str, Any]] = []
    try:
        lines = path.read_text(encoding="utf-8-sig").splitlines()
    except OSError as exc:
        raise P1ReportError(f"cannot read {path}: {exc}") from exc
    for line_no, line in enumerate(lines, start=1):
        if not line.strip():
            continue
        try:
            value = json.loads(line)
        except json.JSONDecodeError as exc:
            raise P1ReportError(f"{path}:{line_no}: invalid JSON: {exc}") from exc
        if not isinstance(value, dict):
            raise P1ReportError(f"{path}:{line_no}: event must be an object")
        events.append(value)
    if not events:
        raise P1ReportError(f"{path}: no events")
    return events


def discover_sessions(data_dir: Path) -> list[SessionData]:
    metadata_paths = sorted(data_dir.rglob("*_metadata.json"))
    if not metadata_paths:
        raise P1ReportError(f"no *_metadata.json files under {data_dir}")

    sessions: list[SessionData] = []
    seen: set[str] = set()
    for metadata_path in metadata_paths:
        stem = metadata_path.name[:-len("_metadata.json")]
        events_path = metadata_path.with_name(stem + "_events.jsonl")
        if not events_path.is_file():
            raise P1ReportError(f"missing matching events file for {metadata_path.name}")
        session = SessionData(
            metadata_path,
            events_path,
            _load_json(metadata_path),
            _load_jsonl(events_path),
        )
        if session.session_id in seen:
            raise P1ReportError(f"duplicate session_id {session.session_id!r}")
        seen.add(session.session_id)
        validate_session(session)
        sessions.append(session)
    return sessions


def validate_session(session: SessionData) -> None:
    metadata = session.metadata
    path = session.metadata_path
    required = (
        "metadata_schema_version",
        "event_schema_version",
        "session_id",
        "playtest_id",
        "build_version",
        "commit_sha",
        "app_version",
        "unity_version",
        "prototype_variant",
        "core_variant",
        "device_model",
        "operating_system",
        "android_api",
        "screen_orientation",
        "session_started_utc",
        "levels",
    )
    for name in required:
        if name not in metadata:
            raise P1ReportError(f"{path}: missing {name}")

    if metadata["metadata_schema_version"] != freeze.METADATA_SCHEMA_VERSION:
        raise P1ReportError(f"{path}: unsupported metadata schema")
    if metadata["event_schema_version"] != freeze.EVENT_SCHEMA_VERSION:
        raise P1ReportError(f"{path}: unsupported event schema")
    if metadata["prototype_variant"] != freeze.PROTOTYPE_VARIANT:
        raise P1ReportError(f"{path}: P1 requires prototype_variant={freeze.PROTOTYPE_VARIANT}")
    if metadata["core_variant"] != freeze.SELECTED_CORE:
        raise P1ReportError(f"{path}: P1 requires core_variant={freeze.SELECTED_CORE}")

    for name in ("device_model", "operating_system"):
        value = metadata[name]
        if not isinstance(value, str) or not value.strip():
            raise P1ReportError(f"{path}: {name} must be a non-empty string")

    android_api = metadata["android_api"]
    if not isinstance(android_api, int) or isinstance(android_api, bool) or android_api < 1:
        raise P1ReportError(f"{path}: android_api must be a positive integer")

    orientation = metadata["screen_orientation"]
    if orientation not in {"portrait", "landscape"}:
        raise P1ReportError(f"{path}: screen_orientation must be portrait or landscape")

    levels = metadata["levels"]
    if not isinstance(levels, list) or len(levels) != 10:
        raise P1ReportError(f"{path}: P1 metadata must contain exactly 10 selected-core levels")
    level_map: dict[str, int] = {}
    for record in levels:
        if not isinstance(record, dict):
            raise P1ReportError(f"{path}: level manifest entries must be objects")
        level_id = record.get("id")
        revision = record.get("revision")
        if not isinstance(level_id, str) or not level_id.startswith("A-"):
            raise P1ReportError(f"{path}: invalid Energy Routing level id {level_id!r}")
        if not isinstance(revision, int) or isinstance(revision, bool) or revision < 1:
            raise P1ReportError(f"{path}: invalid level revision for {level_id!r}")
        if level_id in level_map:
            raise P1ReportError(f"{path}: duplicate level id {level_id!r}")
        level_map[level_id] = revision

    last_timestamp = -1
    for index, event in enumerate(session.events, start=1):
        prefix = f"{session.events_path}:{index}"
        for key in (
            "session_id",
            "playtest_id",
            "build_version",
            "prototype_variant",
            "screen_orientation",
        ):
            if event.get(key) != metadata[key]:
                raise P1ReportError(f"{prefix}: {key} does not match metadata")

        errors = p0_event_schema.validate_event(event)
        if errors:
            raise P1ReportError(f"{prefix}: " + "; ".join(errors))

        timestamp = event.get("timestamp_utc_ms")
        if not isinstance(timestamp, int) or isinstance(timestamp, bool):
            raise P1ReportError(f"{prefix}: invalid timestamp")
        if timestamp < last_timestamp:
            raise P1ReportError(f"{prefix}: events are not timestamp-ordered")
        last_timestamp = timestamp

        level_id = event.get("level_id")
        revision = event.get("level_revision")
        if level_id is not None and level_map.get(level_id) != revision:
            raise P1ReportError(f"{prefix}: level identity is not in frozen metadata")

    audit = p1_event_audit.audit_p1_event_sequence(session.events)
    if audit.errors:
        raise P1ReportError(f"{session.events_path}: sequence audit failed: " + "; ".join(audit.errors))


def _level_key(levels: list[dict[str, Any]]) -> tuple[tuple[str, int], ...]:
    return tuple((str(item["id"]), int(item["revision"])) for item in levels)


def validate_sessions_against_freeze(
    sessions: list[SessionData],
    manifest: dict[str, Any],
) -> None:
    expected_levels = tuple(
        (str(item["id"]), int(item["revision"]))
        for item in manifest["levels"]
    )
    for session in sessions:
        metadata = session.metadata
        if metadata["build_version"] != manifest["build_version"]:
            raise P1ReportError(f"{session.metadata_path}: build_version does not match freeze")
        if metadata["commit_sha"] != manifest["commit_sha"]:
            raise P1ReportError(f"{session.metadata_path}: commit_sha does not match freeze")
        if metadata["unity_version"] != manifest["unity_version"]:
            raise P1ReportError(f"{session.metadata_path}: unity_version does not match freeze")
        if metadata["event_schema_version"] != manifest["event_schema_version"]:
            raise P1ReportError(f"{session.metadata_path}: event schema does not match freeze")
        if metadata["prototype_variant"] != manifest["prototype_variant"]:
            raise P1ReportError(f"{session.metadata_path}: prototype variant does not match freeze")
        if metadata["core_variant"] != manifest["selected_core"]:
            raise P1ReportError(f"{session.metadata_path}: selected core does not match freeze")
        expected_orientation = manifest["test_plan"]["orientation"]
        if metadata["screen_orientation"] != expected_orientation:
            raise P1ReportError(
                f"{session.metadata_path}: screen_orientation {metadata['screen_orientation']!r} "
                f"does not match frozen orientation {expected_orientation!r}"
            )
        if _level_key(metadata["levels"]) != expected_levels:
            raise P1ReportError(f"{session.metadata_path}: level revisions do not match freeze")


def _rate(values: Iterable[bool]) -> dict[str, Any]:
    items = list(values)
    yes = sum(1 for value in items if value)
    total = len(items)
    return {"yes": yes, "total": total, "rate": None if total == 0 else yes / total}


def _moderation_rate(
    sessions: list[SessionData],
    moderation: dict[str, ModerationRecord],
    field: str,
    exclude_field: str | None = None,
) -> dict[str, Any]:
    values: list[bool] = []
    for session in sessions:
        record = moderation.get(session.session_id)
        if record is None or record.cohort != "fresh":
            continue
        if exclude_field is not None and getattr(record, exclude_field):
            continue
        value = getattr(record, field)
        if value is not None:
            values.append(value)
    return _rate(values)


def _has(session: SessionData, event_name: str, **properties: Any) -> bool:
    for event in session.events:
        if event.get("event_name") != event_name:
            continue
        if all(event.get(name) == value for name, value in properties.items()):
            return True
    return False


def _post_77_click_within_window(session: SessionData, window_ms: int) -> bool:
    for event in session.events:
        if event.get("event_name") != "next_puzzle_clicked":
            continue
        if event.get("offer_context") != "post_77_discovery":
            continue
        delay = event.get("ms_since_offer")
        return isinstance(delay, int) and not isinstance(delay, bool) and delay <= window_ms
    return False


def validate_moderation_against_sessions(
    sessions: list[SessionData],
    moderation: dict[str, ModerationRecord],
    manifest: dict[str, Any],
) -> None:
    sessions_by_id = {session.session_id: session for session in sessions}
    window = int(manifest["gate_plan"]["voluntary_window_ms"])
    for session_id, record in moderation.items():
        session = sessions_by_id.get(session_id)
        if session is None:
            continue
        if (
            record.cohort == "fresh"
            and not record.exclude_voluntary
            and record.post_island_voluntary_continuation is None
        ):
            raise P1ReportError(
                f"moderation session {session_id!r}: fresh non-excluded "
                "post_island_voluntary_continuation must be yes/no"
            )
        if (
            not record.exclude_voluntary
            and record.post_island_voluntary_continuation is True
            and not _post_77_click_within_window(session, window)
        ):
            raise P1ReportError(
                f"moderation session {session_id!r}: formal voluntary continuation requires "
                "post_77_discovery click telemetry inside the frozen window"
            )


def _environment_profiles(sessions: list[SessionData]) -> list[dict[str, Any]]:
    counts: dict[tuple[str, str, str, str], int] = {}
    for session in sessions:
        metadata = session.metadata
        device = str(metadata.get("device_model") or "unknown")
        operating_system = str(metadata.get("operating_system") or "unknown")
        api = metadata.get("android_api")
        api_text = str(api) if isinstance(api, int) and not isinstance(api, bool) else "unknown"
        orientation = str(metadata.get("screen_orientation") or "unknown")
        key = (device, operating_system, api_text, orientation)
        counts[key] = counts.get(key, 0) + 1

    return [
        {
            "device_model": key[0],
            "operating_system": key[1],
            "android_api": key[2],
            "screen_orientation": key[3],
            "sessions": count,
        }
        for key, count in sorted(counts.items())
    ]


def _session_time_range(sessions: list[SessionData]) -> dict[str, str | None]:
    values = sorted(
        str(session.metadata.get("session_started_utc"))
        for session in sessions
        if session.metadata.get("session_started_utc")
    )
    return {
        "first": values[0] if values else None,
        "last": values[-1] if values else None,
    }


def _exclusion_records(moderation: dict[str, ModerationRecord]) -> list[dict[str, Any]]:
    records: list[dict[str, Any]] = []
    for session_id in sorted(moderation):
        record = moderation[session_id]
        metrics: list[str] = []
        if record.exclude_resource:
            metrics.extend(("resource_comprehension", "puzzle_reward_comprehension"))
        if record.exclude_repair:
            metrics.append("repair_world_change_comprehension")
        if record.exclude_voluntary:
            metrics.append("post_island_voluntary_continuation")
        if not metrics:
            continue
        records.append({
            "session_id": session_id,
            "cohort": record.cohort,
            "metrics": metrics,
            "reason": record.exclusion_reason,
            "moderator_id": record.moderator_id,
        })
    return records


def summarize(
    sessions: list[SessionData],
    moderation: dict[str, ModerationRecord],
    manifest: dict[str, Any],
) -> dict[str, Any]:
    session_ids = {session.session_id for session in sessions}
    unknown = sorted(set(moderation) - session_ids)
    if unknown:
        raise P1ReportError("moderation references unknown sessions: " + ", ".join(unknown))

    fresh_count = sum(
        1 for session in sessions
        if moderation.get(session.session_id) is not None
        and moderation[session.session_id].cohort == "fresh"
    )
    returning_count = sum(
        1 for session in sessions
        if moderation.get(session.session_id) is not None
        and moderation[session.session_id].cohort == "returning"
    )
    missing_moderation = sorted(
        session.session_id for session in sessions if session.session_id not in moderation
    )
    fresh_target = int(manifest["gate_plan"]["fresh_sessions_target"])
    window = int(manifest["gate_plan"]["voluntary_window_ms"])

    telemetry = {
        "reward_claim_reach": _rate(_has(s, "reward_claimed") for s in sessions),
        "generator_repair_reach": _rate(_has(s, "generator_repair") for s in sessions),
        "visible_generator_change_reach": _rate(
            _has(s, "island_change", change_type="power_on", caused_by="generator_repair")
            for s in sessions
        ),
        "area_unlock_reach": _rate(_has(s, "area_unlock") for s in sessions),
        "robot_77_reach": _rate(_has(s, "robot_77_discovered") for s in sessions),
        "post_77_offer_reach": _rate(
            _has(s, "next_puzzle_offered", offer_context="post_77_discovery")
            for s in sessions
        ),
        "post_77_click_within_window": _rate(
            _post_77_click_within_window(s, window) for s in sessions
        ),
        "prototype_complete": _rate(
            _has(s, "session_end", end_reason="prototype_complete") for s in sessions
        ),
    }

    formal = {
        "resource_comprehension": _moderation_rate(
            sessions, moderation, "resource_comprehension", "exclude_resource"
        ),
        "puzzle_reward_comprehension": _moderation_rate(
            sessions, moderation, "puzzle_reward_comprehension", "exclude_resource"
        ),
        "repair_world_change_comprehension": _moderation_rate(
            sessions, moderation, "repair_world_change_comprehension", "exclude_repair"
        ),
        "robot_77_remembered": _moderation_rate(
            sessions, moderation, "robot_77_remembered"
        ),
        "puzzle_island_connected": _moderation_rate(
            sessions, moderation, "puzzle_island_connected"
        ),
        "island_increased_desire": _moderation_rate(
            sessions, moderation, "island_increased_desire"
        ),
        "post_island_voluntary_continuation": _moderation_rate(
            sessions, moderation, "post_island_voluntary_continuation", "exclude_voluntary"
        ),
        "help_required": _moderation_rate(sessions, moderation, "help_required"),
    }

    voluntary = formal["post_island_voluntary_continuation"]
    threshold = float(manifest["gate_plan"]["post_island_voluntary_continuation_min"])
    fresh_sample_complete = fresh_count >= fresh_target
    gate_state = "INSUFFICIENT DATA"
    if fresh_sample_complete and voluntary["rate"] is not None:
        gate_state = "MEETS INITIAL TARGET" if voluntary["rate"] >= threshold else "BELOW INITIAL TARGET"

    return {
        "batch_id": manifest["batch_id"],
        "build_version": manifest["build_version"],
        "commit_sha": manifest["commit_sha"],
        "sessions_total": len(sessions),
        "fresh_sessions_with_moderation": fresh_count,
        "returning_sessions_with_moderation": returning_count,
        "sessions_without_moderation": len(missing_moderation),
        "fresh_sessions_target": fresh_target,
        "fresh_sample_complete": fresh_sample_complete,
        "environment_profiles": _environment_profiles(sessions),
        "session_time_range": _session_time_range(sessions),
        "moderator_ids": sorted({record.moderator_id for record in moderation.values()}),
        "exclusions": _exclusion_records(moderation),
        "telemetry": telemetry,
        "formal": formal,
        "post_island_voluntary_gate_state": gate_state,
        "missing_moderation_session_ids": missing_moderation,
    }


def _pct(metric: dict[str, Any]) -> str:
    value = metric["rate"]
    return "n/a" if value is None else f"{value * 100:.1f}% ({metric['yes']}/{metric['total']})"


def _frozen_levels(manifest: dict[str, Any]) -> str:
    levels = manifest.get("levels") or []
    if not levels:
        return "unknown"
    return ", ".join(
        f"`{item.get('id', 'unknown')}` r{item.get('revision', 'unknown')}"
        for item in levels
    )


def render_report(summary: dict[str, Any], manifest: dict[str, Any]) -> str:
    artifact = manifest.get("artifact")
    artifact_lines = ["- APK: UNBOUND — tooling smoke only"]
    if isinstance(artifact, dict):
        artifact_lines = [
            f"- APK: `{artifact['filename']}`",
            f"- APK SHA-256: `{artifact['sha256']}`",
            f"- ABI: {', '.join(artifact['abis'])}",
            f"- 16 KB ELF: {'PASS' if artifact['arm64_elf_16kb_compatible'] else 'FAIL'}",
            f"- 16 KB APK ZIP alignment: {'PASS' if artifact['apk_uncompressed_libs_16kb_zip_aligned'] else 'FAIL'}",
        ]

    test_plan = manifest.get("test_plan") or {}
    device_targets = test_plan.get("device_targets") or []
    time_range = summary["session_time_range"]
    time_text = "none"
    if time_range["first"] is not None:
        time_text = time_range["first"]
        if time_range["last"] != time_range["first"]:
            time_text += " -> " + str(time_range["last"])

    lines = [
        "# Project 77 — P1 Batch Report",
        "",
        f"- Batch: `{summary['batch_id']}`",
        f"- Build: `{summary['build_version']}`",
        f"- Commit: `{summary['commit_sha']}`",
        f"- Event schema version: {manifest.get('event_schema_version', 'unknown')}",
        f"- Metadata schema version: {manifest.get('metadata_schema_version', 'unknown')}",
        f"- Prototype variant: `{manifest.get('prototype_variant', 'unknown')}`",
        f"- Selected core: `{manifest.get('selected_core', 'unknown')}`",
        "- Hypothesis: the integrated island meta loop increases desire to continue after Energy Routing.",
        *artifact_lines,
        f"- Frozen orientation: {test_plan.get('orientation', 'unknown')}",
        f"- Frozen help threshold: {test_plan.get('help_threshold_seconds', 'unknown')} s",
        f"- Frozen device targets: {'; '.join(str(value) for value in device_targets) if device_targets else 'none'}",
        f"- Frozen level set + revisions: {_frozen_levels(manifest)}",
        f"- Session time range: {time_text}",
        f"- Sessions: {summary['sessions_total']}",
        f"- Fresh / returning split: {summary['fresh_sessions_with_moderation']} / {summary['returning_sessions_with_moderation']}",
        f"- Sessions without moderation: {summary['sessions_without_moderation']}",
        f"- Fresh sessions with moderation: {summary['fresh_sessions_with_moderation']}/{summary['fresh_sessions_target']}",
        f"- Fresh sample: {'COMPLETE' if summary['fresh_sample_complete'] else 'INCOMPLETE'}",
        "",
        "## Actual session environment coverage",
        "",
    ]

    profiles = summary["environment_profiles"]
    if profiles:
        for profile in profiles:
            lines.append(
                "- "
                f"{profile['device_model']} | {profile['operating_system']} | "
                f"API {profile['android_api']} | {profile['screen_orientation']}: "
                f"{profile['sessions']} session(s)"
            )
    else:
        lines.append("- No session environments loaded.")

    lines.extend(["", "## Exclusions", ""])
    exclusions = summary["exclusions"]
    if exclusions:
        for record in exclusions:
            lines.append(
                f"- `{record['session_id']}` ({record['cohort']}): "
                f"{', '.join(record['metrics'])}; reason: {record['reason']}; "
                f"moderator: `{record['moderator_id']}`"
            )
    else:
        lines.append("- No formal metric exclusions recorded.")

    lines.extend(["", "## Telemetry reach (all loaded sessions)", ""])
    for name, metric in summary["telemetry"].items():
        lines.append(f"- {name}: {_pct(metric)}")

    lines.extend(["", "## Formal P1 observations (fresh moderated sessions)", ""])
    for name, metric in summary["formal"].items():
        lines.append(f"- {name}: {_pct(metric)}")

    lines.extend([
        "",
        "## Gate signal",
        "",
        f"- Post-island voluntary continuation: **{summary['post_island_voluntary_gate_state']}**",
        f"- Initial threshold: >= {manifest['gate_plan']['post_island_voluntary_continuation_min'] * 100:.0f}%",
        "- Final CONTINUE / ITERATE / PIVOT / STOP remains an owner/product decision after qualitative review.",
        "",
        "## Data quality",
        "",
    ])
    missing = summary["missing_moderation_session_ids"]
    if missing:
        lines.append(
            f"- Missing moderation for {len(missing)} session(s): " + ", ".join(f"`{value}`" for value in missing)
        )
        lines.append("- Formal comprehension/voluntary rates exclude sessions without moderation records.")
    else:
        lines.append("- Every loaded session has a moderation record.")

    moderators = summary["moderator_ids"]
    lines.append(
        "- Moderators represented: " +
        (", ".join(f"`{value}`" for value in moderators) if moderators else "none")
    )
    lines.append("- Returning sessions are reported separately and are not included in fresh formal rates.")
    lines.append("- Telemetry reach above is explicitly scoped to all loaded sessions and does not replace formal moderation.")

    return "\n".join(lines)


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate a Gate P1 report from selected-meta telemetry bound to a P1 freeze."
    )
    parser.add_argument("data_dir", type=Path)
    parser.add_argument("--freeze", type=Path, required=True, dest="freeze_path")
    parser.add_argument("--moderation", type=Path)
    parser.add_argument("--artifact", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--summary-json", type=Path)
    parser.add_argument("--allow-unbound-artifact", action="store_true")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    try:
        manifest = _load_json(args.freeze_path)
        if manifest.get("artifact") is None and not args.allow_unbound_artifact:
            raise P1ReportError(
                "real P1 gate report requires an artifact-bound freeze; "
                "use --allow-unbound-artifact only for tooling smoke"
            )
        freeze.verify_manifest(
            manifest,
            freeze.current_commit(),
            artifact_path=args.artifact,
        )
        sessions = discover_sessions(args.data_dir)
        validate_sessions_against_freeze(sessions, manifest)
        moderation = load_moderation(args.moderation)
        validate_moderation_against_sessions(sessions, moderation, manifest)
        summary = summarize(sessions, moderation, manifest)
        report = render_report(summary, manifest)
    except (P1ReportError, freeze.FreezeError) as exc:
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
