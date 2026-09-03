#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

import p0_batch_report as batch
import p0_freeze_manifest as freeze


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


def render_gate_report(summary: dict[str, Any], manifest: dict[str, Any]) -> str:
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
        return marker + "\n" + "\n".join(header) + "\n" + report[len(marker):].lstrip("\n")
    return "\n".join(header) + "\n" + report


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
        summary["freeze"] = {
            "batch_id": manifest.get("batch_id"),
            "freeze_fingerprint": manifest.get("freeze_fingerprint"),
        }
        report_text = render_gate_report(summary, manifest)
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
