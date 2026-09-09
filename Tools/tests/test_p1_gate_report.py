import unittest
from pathlib import Path

import p1_gate_report as report


class P1GateReportTests(unittest.TestCase):
    def test_summary_uses_moderated_post_island_continuation(self):
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
        moderation = {
            "s1": report.ModerationRecord(
                session_id="s1",
                cohort="fresh",
                resource_comprehension=True,
                puzzle_reward_comprehension=True,
                repair_world_change_comprehension=True,
                robot_77_remembered=True,
                puzzle_island_connected=True,
                island_increased_desire=True,
                post_island_voluntary_continuation=True,
                help_required=False,
                exclude_resource=False,
                exclude_repair=False,
                exclude_voluntary=False,
                exclusion_reason="",
                moderator_id="M",
            )
        }
        manifest = {
            "batch_id": "P1-001",
            "build_version": "0.0.1-prototype+abc",
            "commit_sha": "abc",
            "gate_plan": {
                "fresh_sessions_target": 10,
                "post_island_voluntary_continuation_min": 0.50,
                "voluntary_window_ms": 15000,
            },
        }

        summary = report.summarize([session], moderation, manifest)

        self.assertEqual(summary["post_island_voluntary_gate_state"], "MEETS INITIAL TARGET")
        self.assertEqual(summary["telemetry"]["robot_77_reach"]["rate"], 1.0)
        self.assertEqual(summary["telemetry"]["post_77_click_within_window"]["rate"], 1.0)
        self.assertEqual(summary["formal"]["repair_world_change_comprehension"]["rate"], 1.0)

    def test_excluded_voluntary_session_is_not_counted(self):
        session = report.SessionData(
            Path("s1_metadata.json"),
            Path("s1_events.jsonl"),
            {"session_id": "s1"},
            [],
        )
        moderation = {
            "s1": report.ModerationRecord(
                "s1", "fresh", True, True, True, True, True, True, True, False,
                False, False, True, "moderator prompted", "M"
            )
        }
        manifest = {
            "batch_id": "P1-001",
            "build_version": "x",
            "commit_sha": "y",
            "gate_plan": {
                "fresh_sessions_target": 10,
                "post_island_voluntary_continuation_min": 0.50,
                "voluntary_window_ms": 15000,
            },
        }

        summary = report.summarize([session], moderation, manifest)
        metric = summary["formal"]["post_island_voluntary_continuation"]
        self.assertEqual(metric["total"], 0)
        self.assertIsNone(metric["rate"])

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
