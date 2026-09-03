import csv
import json
import tempfile
import unittest
from pathlib import Path
import sys

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))

import p0_batch_report as report


def write_session(root: Path, session_id: str, variant: str, build: str = "build-1") -> None:
    code = {
        "energy_routing": "A",
        "path_expedition_routing": "B",
        "flow_network_restoration": "C",
    }[variant]
    metadata = {
        "metadata_schema_version": 1,
        "event_schema_version": 1,
        "session_id": session_id,
        "playtest_id": "batch-test",
        "build_version": build,
        "commit_sha": "commit-1",
        "app_version": "0.0.1",
        "unity_version": "6000.3.22f1",
        "prototype_variant": variant,
        "device_model": "test-device",
        "operating_system": "Android test",
        "android_api": 36,
        "screen_orientation": "portrait",
        "session_started_utc": "2026-09-03T00:00:00Z",
        "levels": [{"id": f"{code}-{index:03d}", "revision": 1} for index in range(1, 11)],
    }
    events = [
        {
            "event_schema_version": 1,
            "event_name": "prototype_start",
            "timestamp_utc_ms": 1,
            "session_id": session_id,
            "playtest_id": "batch-test",
            "build_version": build,
            "prototype_variant": variant,
            "level_id": None,
            "level_revision": None,
            "device_tier": "unknown",
            "screen_orientation": "portrait",
            "entry_point": "variant_select",
            "core_variant": variant,
        },
        {
            "event_schema_version": 1,
            "event_name": "level_start",
            "timestamp_utc_ms": 2,
            "session_id": session_id,
            "playtest_id": "batch-test",
            "build_version": build,
            "prototype_variant": variant,
            "level_id": f"{code}-001",
            "level_revision": 1,
            "device_tier": "unknown",
            "screen_orientation": "portrait",
            "attempt_index": 1,
            "core_variant": variant,
            "level_sequence_index": 1,
        },
        {
            "event_schema_version": 1,
            "event_name": "level_complete",
            "timestamp_utc_ms": 40_002,
            "session_id": session_id,
            "playtest_id": "batch-test",
            "build_version": build,
            "prototype_variant": variant,
            "level_id": f"{code}-001",
            "level_revision": 1,
            "device_tier": "unknown",
            "screen_orientation": "portrait",
            "attempt_index": 1,
            "duration_ms": 40_000,
            "valid_interaction_count": 1,
            "invalid_interaction_count": 0,
            "core_variant": variant,
        },
        {
            "event_schema_version": 1,
            "event_name": "next_puzzle_offered",
            "timestamp_utc_ms": 40_003,
            "session_id": session_id,
            "playtest_id": "batch-test",
            "build_version": build,
            "prototype_variant": variant,
            "level_id": f"{code}-001",
            "level_revision": 1,
            "device_tier": "unknown",
            "screen_orientation": "portrait",
            "offer_context": "post_level",
            "next_level_id": f"{code}-002",
            "offer_sequence_index": 1,
        },
        {
            "event_schema_version": 1,
            "event_name": "next_puzzle_clicked",
            "timestamp_utc_ms": 45_003,
            "session_id": session_id,
            "playtest_id": "batch-test",
            "build_version": build,
            "prototype_variant": variant,
            "level_id": f"{code}-001",
            "level_revision": 1,
            "device_tier": "unknown",
            "screen_orientation": "portrait",
            "offer_context": "post_level",
            "next_level_id": f"{code}-002",
            "ms_since_offer": 5_000,
            "offer_sequence_index": 1,
        },
    ]
    (root / f"{session_id}_metadata.json").write_text(json.dumps(metadata), encoding="utf-8")
    (root / f"{session_id}_events.jsonl").write_text(
        "\n".join(json.dumps(event) for event in events), encoding="utf-8"
    )


def write_moderation(root: Path, rows: list[dict[str, str]]) -> Path:
    path = root / "moderation.csv"
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=report.MODERATION_COLUMNS)
        writer.writeheader()
        writer.writerows(rows)
    return path


def moderation_row(session_id: str, *, exclude_voluntary: str = "no") -> dict[str, str]:
    return {
        "session_id": session_id,
        "cohort": "fresh",
        "variant_order_index": "1",
        "tutorial_completed": "yes",
        "unaided_comprehension": "yes",
        "help_required": "no",
        "voluntary_continuation": "yes",
        "exclude_tutorial": "no",
        "exclude_unaided": "no",
        "exclude_voluntary": exclude_voluntary,
        "exclusion_reason": "prompted" if exclude_voluntary == "yes" else "",
        "moderator_id": "M1",
    }


class P0BatchReportTests(unittest.TestCase):
    def test_valid_frozen_batch_summarizes_formal_and_telemetry_metrics(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_session(root, "s1", "energy_routing")
            moderation = report.load_moderation(
                write_moderation(root, [moderation_row("s1")])
            )
            sessions = report.discover_sessions(root)
            summary = report.summarize_batch(sessions, moderation)

            variant = summary["variants"]["energy_routing"]
            self.assertEqual(variant["voluntary_continuation"]["rate"], 1.0)
            self.assertEqual(variant["telemetry_continue_within_15s"]["rate"], 1.0)
            self.assertEqual(variant["median_completed_level_ms"], 40_000)

    def test_mixed_builds_fail_freeze_validation(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_session(root, "s1", "energy_routing", build="build-1")
            write_session(root, "s2", "path_expedition_routing", build="build-2")
            with self.assertRaises(report.BatchError):
                report.discover_sessions(root)

    def test_excluded_voluntary_session_is_not_in_formal_denominator(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_session(root, "s1", "energy_routing")
            moderation = report.load_moderation(
                write_moderation(root, [moderation_row("s1", exclude_voluntary="yes")])
            )
            sessions = report.discover_sessions(root)
            summary = report.summarize_batch(sessions, moderation)

            metric = summary["variants"]["energy_routing"]["voluntary_continuation"]
            self.assertIsNone(metric["rate"])
            self.assertEqual(metric["denominator"], 0)
            self.assertEqual(metric["excluded"], 1)

    def test_duplicate_terminal_attempt_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_session(root, "s1", "energy_routing")
            events_path = root / "s1_events.jsonl"
            events = events_path.read_text(encoding="utf-8").splitlines()
            complete = json.loads(events[2])
            complete["timestamp_utc_ms"] = 40_003
            events.insert(3, json.dumps(complete))
            events_path.write_text("\n".join(events), encoding="utf-8")
            with self.assertRaises(report.BatchError):
                report.discover_sessions(root)


if __name__ == "__main__":
    unittest.main()
