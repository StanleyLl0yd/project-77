import csv
import tempfile
import unittest
from pathlib import Path

import p1_gate_report as report


def moderation_record(session_id: str, voluntary: bool, exclude_voluntary: bool = False):
    return report.ModerationRecord(
        session_id=session_id,
        cohort="fresh",
        resource_comprehension=True,
        puzzle_reward_comprehension=True,
        repair_world_change_comprehension=True,
        robot_77_remembered=True,
        puzzle_island_connected=True,
        island_increased_desire=True,
        post_island_voluntary_continuation=voluntary,
        help_required=False,
        exclude_resource=False,
        exclude_repair=False,
        exclude_voluntary=exclude_voluntary,
        exclusion_reason="" if not exclude_voluntary else "excluded",
        moderator_id="M",
    )


def gate_manifest(target: int = 10):
    return {
        "batch_id": "P1-002",
        "build_version": "0.0.1-prototype+abc",
        "commit_sha": "abc",
        "gate_plan": {
            "fresh_sessions_target": target,
            "post_island_voluntary_continuation_min": 0.50,
            "voluntary_window_ms": 15000,
        },
    }


def write_moderation_csv(path: Path, *, exclusion_reason: str):
    row = {column: "" for column in report.MODERATION_COLUMNS}
    row.update(
        {
            "session_id": "s1",
            "cohort": "fresh",
            "post_island_voluntary_continuation": "yes",
            "help_required": "yes",
            "exclude_resource": "no",
            "exclude_repair": "no",
            "exclude_voluntary": "yes",
            "exclusion_reason": exclusion_reason,
            "moderator_id": "M",
        }
    )
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=report.MODERATION_COLUMNS)
        writer.writeheader()
        writer.writerow(row)


def valid_session(session_id: str = "s1") -> report.SessionData:
    build_version = "0.0.1-prototype+abcdef123456"
    playtest_id = "p1-test"
    orientation = "portrait"
    levels = [{"id": f"A-{index:03d}", "revision": 1} for index in range(1, 11)]
    metadata = {
        "metadata_schema_version": 1,
        "event_schema_version": 1,
        "session_id": session_id,
        "playtest_id": playtest_id,
        "build_version": build_version,
        "commit_sha": "a" * 40,
        "app_version": "0.0.1-prototype",
        "unity_version": "6000.3.22f1",
        "prototype_variant": "selected_meta",
        "core_variant": "energy_routing",
        "device_model": "Joy 4",
        "operating_system": "Android OS 10 / API-29",
        "android_api": 29,
        "screen_orientation": orientation,
        "session_started_utc": "2026-09-11T06:00:00Z",
        "levels": levels,
    }
    events = [
        {
            "event_schema_version": 1,
            "event_name": "prototype_start",
            "timestamp_utc_ms": 1_000,
            "session_id": session_id,
            "playtest_id": playtest_id,
            "build_version": build_version,
            "prototype_variant": "selected_meta",
            "level_id": None,
            "level_revision": None,
            "device_tier": "unknown",
            "screen_orientation": orientation,
            "entry_point": "variant_select",
            "core_variant": "energy_routing",
        }
    ]
    return report.SessionData(
        Path(f"{session_id}_metadata.json"),
        Path(f"{session_id}_events.jsonl"),
        metadata,
        events,
    )


class P1GateReportTests(unittest.TestCase):
    def test_incomplete_sample_keeps_observed_rate_but_does_not_claim_target(self):
        metadata = {
            "session_id": "s1",
        }
        events = [
            {"event_name": "reward_claimed"},
            {"event_name": "generator_repair"},
            {"event_name": "island_change", "change_type": "power_on", "caused_by": "generator_repair"},
            {"event_name": "area_unlock"},
            {"event_name": "robot_77_discovered"},
            {"event_name": "next_puzzle_offered", "offer_context": "post_77_discovery"},
            {"event_name": "next_puzzle_clicked", "offer_context": "post_77_discovery", "ms_since_offer": 3000},
            {"event_name": "session_end", "end_reason": "prototype_complete"},
        ]
        session = report.SessionData(Path("s1_metadata.json"), Path("s1_events.jsonl"), metadata, events)
        moderation = {"s1": moderation_record("s1", True)}

        summary = report.summarize([session], moderation, gate_manifest())

        self.assertEqual(summary["post_island_voluntary_gate_state"], "INSUFFICIENT DATA")
        self.assertFalse(summary["fresh_sample_complete"])
        self.assertEqual(summary["formal"]["post_island_voluntary_continuation"]["rate"], 1.0)
        self.assertEqual(summary["telemetry"]["robot_77_reach"]["rate"], 1.0)
        self.assertEqual(summary["telemetry"]["post_77_click_within_window"]["rate"], 1.0)
        self.assertEqual(summary["formal"]["repair_world_change_comprehension"]["rate"], 1.0)

    def test_complete_fresh_sample_can_meet_target(self):
        sessions = []
        moderation = {}
        for index in range(10):
            session_id = f"s{index}"
            sessions.append(
                report.SessionData(
                    Path(f"{session_id}_metadata.json"),
                    Path(f"{session_id}_events.jsonl"),
                    {"session_id": session_id},
                    [],
                )
            )
            moderation[session_id] = moderation_record(session_id, index < 5)

        summary = report.summarize(sessions, moderation, gate_manifest())

        self.assertTrue(summary["fresh_sample_complete"])
        self.assertEqual(summary["formal"]["post_island_voluntary_continuation"]["rate"], 0.5)
        self.assertEqual(summary["post_island_voluntary_gate_state"], "MEETS INITIAL TARGET")

    def test_complete_fresh_sample_can_be_below_target(self):
        sessions = []
        moderation = {}
        for index in range(10):
            session_id = f"s{index}"
            sessions.append(
                report.SessionData(
                    Path(f"{session_id}_metadata.json"),
                    Path(f"{session_id}_events.jsonl"),
                    {"session_id": session_id},
                    [],
                )
            )
            moderation[session_id] = moderation_record(session_id, index < 4)

        summary = report.summarize(sessions, moderation, gate_manifest())

        self.assertTrue(summary["fresh_sample_complete"])
        self.assertEqual(summary["formal"]["post_island_voluntary_continuation"]["rate"], 0.4)
        self.assertEqual(summary["post_island_voluntary_gate_state"], "BELOW INITIAL TARGET")

    def test_excluded_voluntary_session_is_not_counted(self):
        session = report.SessionData(
            Path("s1_metadata.json"),
            Path("s1_events.jsonl"),
            {"session_id": "s1"},
            [],
        )
        moderation = {"s1": moderation_record("s1", True, exclude_voluntary=True)}

        summary = report.summarize([session], moderation, gate_manifest())
        metric = summary["formal"]["post_island_voluntary_continuation"]
        self.assertEqual(metric["total"], 0)
        self.assertIsNone(metric["rate"])
        self.assertEqual(summary["post_island_voluntary_gate_state"], "INSUFFICIENT DATA")

    def test_exclusion_requires_explicit_reason(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "moderation.csv"
            write_moderation_csv(path, exclusion_reason="")

            with self.assertRaisesRegex(report.P1ReportError, "exclusion_reason is required"):
                report.load_moderation(path)

            write_moderation_csv(path, exclusion_reason="moderator prompted continuation")
            records = report.load_moderation(path)

        self.assertTrue(records["s1"].exclude_voluntary)
        self.assertEqual(records["s1"].exclusion_reason, "moderator prompted continuation")

    def test_render_report_marks_incomplete_sample(self):
        session = report.SessionData(
            Path("s1_metadata.json"),
            Path("s1_events.jsonl"),
            {"session_id": "s1"},
            [],
        )
        manifest = gate_manifest()
        manifest["test_plan"] = {
            "orientation": "portrait",
            "help_threshold_seconds": 30,
            "device_targets": ["Joy 4"],
        }
        summary = report.summarize([session], {"s1": moderation_record("s1", True)}, manifest)

        rendered = report.render_report(summary, manifest)

        self.assertIn("Fresh sample: INCOMPLETE", rendered)
        self.assertIn("Post-island voluntary continuation: **INSUFFICIENT DATA**", rendered)
        self.assertIn("100.0% (1/1)", rendered)

    def test_valid_android_environment_and_orientation_are_accepted(self):
        report.validate_session(valid_session())

    def test_android_api_is_required_and_must_be_positive_integer(self):
        missing = valid_session("missing-api")
        missing.metadata.pop("android_api")
        with self.assertRaisesRegex(report.P1ReportError, "missing android_api"):
            report.validate_session(missing)

        for value in (0, -1, True):
            with self.subTest(android_api=value):
                invalid = valid_session(f"invalid-api-{value}")
                invalid.metadata["android_api"] = value
                with self.assertRaisesRegex(report.P1ReportError, "android_api must be a positive integer"):
                    report.validate_session(invalid)

    def test_device_and_operating_system_identity_must_be_non_empty(self):
        for field in ("device_model", "operating_system"):
            with self.subTest(field=field):
                session = valid_session(f"blank-{field}")
                session.metadata[field] = "   "
                with self.assertRaisesRegex(report.P1ReportError, field):
                    report.validate_session(session)

    def test_mid_session_orientation_drift_is_rejected(self):
        session = valid_session("rotated")
        session.events[0]["screen_orientation"] = "landscape"

        with self.assertRaisesRegex(report.P1ReportError, "screen_orientation does not match metadata"):
            report.validate_session(session)

    def test_freeze_rejects_mismatched_session_orientation(self):
        levels = [{"id": f"A-{index:03d}", "revision": 1} for index in range(1, 11)]
        session = report.SessionData(
            Path("s1_metadata.json"),
            Path("s1_events.jsonl"),
            {
                "session_id": "s1",
                "build_version": "0.0.1-prototype+abc",
                "commit_sha": "abc",
                "unity_version": "6000.3.22f1",
                "event_schema_version": 1,
                "prototype_variant": "selected_meta",
                "core_variant": "energy_routing",
                "screen_orientation": "landscape",
                "levels": levels,
            },
            [],
        )
        manifest = {
            "build_version": "0.0.1-prototype+abc",
            "commit_sha": "abc",
            "unity_version": "6000.3.22f1",
            "event_schema_version": 1,
            "prototype_variant": "selected_meta",
            "selected_core": "energy_routing",
            "test_plan": {"orientation": "portrait"},
            "levels": levels,
        }

        with self.assertRaisesRegex(report.P1ReportError, "frozen orientation"):
            report.validate_sessions_against_freeze([session], manifest)


if __name__ == "__main__":
    unittest.main()
