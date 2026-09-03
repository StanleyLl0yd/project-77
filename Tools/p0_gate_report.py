#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

import p0_batch_report as batch
import p0_freeze_manifest as freeze

FRESH_EXPOSURE_TARGET = 10


class GateReportError(ValueError):
    pass


def _load_manifest(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise GateReportError(f"cannot parse freeze manifest {path}: {exc}") from exc
    if not isinstance(value, dict):
        raise GateReportError("freeze manifest must contain one JSON object")
    if value.get("freeze_manifest_schema_version") != freeze.MANIFEST_SCHEMA_VERSION:
        raise GateReportError("unsupported freeze manifest schema")
    return value


def validate_sessions_against_freeze(
    sessions: list[batch.SessionData],
    manifest: dict[str, Any],
) -> None:
    if not sessions:
        raise GateReportError("no sessions loaded")

    expected_build = manifest.get("build_version")
    expected_commit = manifest.get("commit_sha")
    expected_event_schema = manifest.get("event_schema_version")
    expected_unity = manifest.get("unity_version")
    frozen_levels = manifest.get("levels")
    if not isinstance(frozen_levels, dict):
        raise GateReportError("freeze manifest has no levels map")

    for session in sessions:
        metadata = session.metadata
        prefix = str(session.metadata_path)
        checks = (
            ("build_version", expected_build),
            ("commit_sha", expected_commit),
            ("event_schema_version", expected_event_schema),
            ("unity_version", expected_unity),
        )
        for field, expected in checks:
            if metadata.get(field) != expected:
                raise GateReportError(
                    f"{prefix}: {field} {metadata.get(field)!r} does not match freeze {expected!r}"
                )

        variant = metadata.get("prototype_variant")
        expected_records = frozen_levels.get(variant)
        if not isinstance(expected_records, list):
            raise GateReportError(f"{prefix}: variant {variant!r} is not present in freeze manifest")

        expected_identity = [
            {"id": record.get("id"), "revision": record.get("revision")}
            for record in expected_records
        ]
        if metadata.get("levels") != expected_identity:
            raise GateReportError(f"{prefix}: level IDs/revisions do not match frozen variant content")


def _event_count(session: batch.SessionData, event_name: str) -> int:
    return sum(1 for event in session.events if event.get("event_name") == event_name)


def _safe_rate(numerator: int, denominator: int) -> float | None:
    return None if denominator <= 0 else numerator / denominator


def build_operational_diagnostics(
    sessions: list[batch.SessionData],
    moderation: dict[str, batch.ModerationRecord],
) -> dict[str, Any]:
    diagnostics: dict[str, Any] = {}
    for variant in batch.VARIANTS:
        variant_sessions = [session for session in sessions if session.variant == variant]
        if not variant_sessions:
            continue

        fresh_sessions = [
            session
            for session in variant_sessions
            if moderation.get(session.session_id) is not None
            and moderation[session.session_id].cohort == "fresh"
        ]
        starts = sum(_event_count(session, "level_start") for session in variant_sessions)
        completes = sum(_event_count(session, "level_complete") for session in variant_sessions)
        fails = sum(_event_count(session, "level_fail") for session in variant_sessions)
        quits = sum(_event_count(session, "level_quit") for session in variant_sessions)
        retries = sum(_event_count(session, "level_retry") for session in variant_sessions)
        invalid = sum(_event_count(session, "invalid_interaction") for session in variant_sessions)
        terminals = completes + fails + quits
        open_attempts = max(0, starts - terminals)

        first_level_id = None
        levels = variant_sessions[0].metadata.get("levels")
        if isinstance(levels, list) and levels and isinstance(levels[0], dict):
            first_level_id = levels[0].get("id")
        first_level_completed_sessions = 0
        prototype_complete_sessions = 0
        offer_sessions = 0
        for session in variant_sessions:
            if first_level_id is not None and any(
                event.get("event_name") == "level_complete"
                and event.get("level_id") == first_level_id
                for event in session.events
            ):
                first_level_completed_sessions += 1
            if any(
                event.get("event_name") == "session_end"
                and event.get("end_reason") == "prototype_complete"
                for event in session.events
            ):
                prototype_complete_sessions += 1
            if _event_count(session, "next_puzzle_offered") > 0:
                offer_sessions += 1

        diagnostics[variant] = {
            "sessions": len(variant_sessions),
            "fresh_exposures_with_moderation": len(fresh_sessions),
            "fresh_exposure_target": FRESH_EXPOSURE_TARGET,
            "fresh_exposure_target_met": len(fresh_sessions) >= FRESH_EXPOSURE_TARGET,
            "attempts_started": starts,
            "attempts_terminal": terminals,
            "open_attempts": open_attempts,
            "completions": completes,
            "fails": fails,
            "quits": quits,
            "retries": retries,
            "invalid_interactions": invalid,
            "completion_per_attempt": _safe_rate(completes, starts),
            "fail_per_attempt": _safe_rate(fails, starts),
            "quit_per_attempt": _safe_rate(quits, starts),
            "invalid_interactions_per_attempt": None if starts == 0 else invalid / starts,
            "first_level_completed_sessions": first_level_completed_sessions,
            "first_level_completion_rate": _safe_rate(first_level_completed_sessions, len(variant_sessions)),
            "prototype_complete_sessions": prototype_complete_sessions,
            "prototype_complete_rate": _safe_rate(prototype_complete_sessions, len(variant_sessions)),
            "sessions_reaching_continuation_offer": offer_sessions,
            "continuation_offer_reach_rate": _safe_rate(offer_sessions, len(variant_sessions)),
        }
    return diagnostics


def _format_rate(value: float | None) -> str:
    return "n/a" if value is None else f"{value * 100:.1f}%"


def _render_operational_diagnostics(diagnostics: dict[str, Any]) -> str:
    lines = [
        "## P0 operational diagnostics",
        "",
        "These diagnostics describe telemetry coverage and data quality. They do not replace the formal comprehension/voluntary metrics or make the gate decision automatically.",
        "",
    ]
    for variant, data in diagnostics.items():
        target_state = "MET" if data["fresh_exposure_target_met"] else "NOT YET MET"
        lines.extend(
            [
                f"### {variant}",
                "",
                f"- Fresh exposures with moderation: {data['fresh_exposures_with_moderation']}/{data['fresh_exposure_target']} — {target_state}",
                f"- First-level completion by session: {data['first_level_completed_sessions']}/{data['sessions']} ({_format_rate(data['first_level_completion_rate'])})",
                f"- Full 10-level set completion by session: {data['prototype_complete_sessions']}/{data['sessions']} ({_format_rate(data['prototype_complete_rate'])})",
                f"- Reached a continuation offer: {data['sessions_reaching_continuation_offer']}/{data['sessions']} ({_format_rate(data['continuation_offer_reach_rate'])})",
                f"- Attempts: {data['attempts_started']} started, {data['attempts_terminal']} terminal, {data['open_attempts']} without a recorded terminal event",
                f"- Attempt outcomes: complete {_format_rate(data['completion_per_attempt'])}, fail {_format_rate(data['fail_per_attempt'])}, quit {_format_rate(data['quit_per_attempt'])}",
                f"- Retries: {data['retries']}; invalid interactions: {data['invalid_interactions']} ({data['invalid_interactions_per_attempt']:.2f}/attempt)" if data["invalid_interactions_per_attempt"] is not None else f"- Retries: {data['retries']}; invalid interactions: {data['invalid_interactions']} (n/a per attempt)",
                "",
            ]
        )
    return "\n".join(lines)


def render_gate_report(
    summary: dict[str, Any],
    manifest: dict[str, Any],
    diagnostics: dict[str, Any],
) -> str:
    report = batch.render_report(summary)
    header = [
        "# Frozen batch identity",
        "",
        f"- Batch ID: `{manifest.get('batch_id')}`",
        f"- Freeze fingerprint: `{manifest.get('freeze_fingerprint')}`",
        f"- Frozen commit: `{manifest.get('commit_sha')}`",
        "",
    ]
    marker = "# Project 77 — P0 Batch Report\n"
    if report.startswith(marker):
        report = marker + "\n" + "\n".join(header) + "\n" + report[len(marker):].lstrip("\n")
    else:
        report = "\n".join(header) + "\n" + report
    return report.rstrip() + "\n\n" + _render_operational_diagnostics(diagnostics).rstrip()


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate a Project 77 P0 gate report only from telemetry that matches a frozen batch manifest."
    )
    parser.add_argument("data_dir", type=Path)
    parser.add_argument("--freeze", type=Path, required=True, dest="freeze_path")
    parser.add_argument("--moderation", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--summary-json", type=Path)
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    try:
        manifest = _load_manifest(args.freeze_path)
        sessions = batch.discover_sessions(args.data_dir)
        validate_sessions_against_freeze(sessions, manifest)
        moderation = batch.load_moderation(args.moderation)
        summary = batch.summarize_batch(sessions, moderation)
        diagnostics = build_operational_diagnostics(sessions, moderation)
        summary["freeze"] = {
            "batch_id": manifest.get("batch_id"),
            "freeze_fingerprint": manifest.get("freeze_fingerprint"),
        }
        summary["operational_diagnostics"] = diagnostics
        report_text = render_gate_report(summary, manifest, diagnostics)
    except (GateReportError, batch.BatchError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1

    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(report_text + "\n", encoding="utf-8")
    else:
        print(report_text)

    if args.summary_json:
        args.summary_json.parent.mkdir(parents=True, exist_ok=True)
        args.summary_json.write_text(
            json.dumps(summary, indent=2, sort_keys=True) + "\n",
            encoding="utf-8",
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
