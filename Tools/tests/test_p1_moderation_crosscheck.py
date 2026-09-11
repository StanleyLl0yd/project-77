import sys
import unittest
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))

import p1_gate_report as report


def session(session_id: str, delay_ms: int | None = None) -> report.SessionData:
    events = []
    if delay_ms is not None:
        events.append(
            {
                "event_name": "next_puzzle_clicked",
                "offer_context": "post_77_discovery",
                "ms_since_offer": delay_ms,
            }
        )
    return report.SessionData(
        Path(f"{session_id}_metadata.json"),
        Path(f"{session_id}_events.jsonl"),
        {"session_id": session_id},
        events,
    )


def moderation(
    session_id: str,
    outcome: bool | None,
    *,
    cohort: str = "fresh",
    excluded: bool = False,
) -> report.ModerationRecord:
    return report.ModerationRecord(
        session_id=session_id,
        cohort=cohort,
        resource_comprehension=None,
        puzzle_reward_comprehension=None,
        repair_world_change_comprehension=None,
        robot_77_remembered=None,
        puzzle_island_connected=None,
        island_increased_desire=None,
        post_island_voluntary_continuation=outcome,
        help_required=None,
        exclude_resource=False,
        exclude_repair=False,
        exclude_voluntary=excluded,
        exclusion_reason="prompted" if excluded else "",
        moderator_id="M1",
    )


class P1ModerationCrosscheckTests(unittest.TestCase):
    manifest = {"gate_plan": {"voluntary_window_ms": 15_000}}

    def validate(
        self,
        session_data: report.SessionData,
        record: report.ModerationRecord,
    ) -> None:
        report.validate_moderation_against_sessions(
            [session_data],
            {record.session_id: record},
            self.manifest,
        )

    def test_formal_yes_requires_click_inside_frozen_window(self) -> None:
        with self.assertRaisesRegex(report.P1ReportError, "click telemetry"):
            self.validate(session("s1"), moderation("s1", True))
        with self.assertRaisesRegex(report.P1ReportError, "click telemetry"):
            self.validate(session("s1", 15_001), moderation("s1", True))
        self.validate(session("s1", 15_000), moderation("s1", True))

    def test_fresh_non_excluded_primary_outcome_cannot_be_unknown(self) -> None:
        with self.assertRaisesRegex(report.P1ReportError, "must be yes/no"):
            self.validate(session("s1"), moderation("s1", None))

    def test_formal_no_remains_valid_even_when_click_exists(self) -> None:
        self.validate(session("s1", 2_000), moderation("s1", False))

    def test_excluded_primary_outcome_does_not_require_click_or_classification(self) -> None:
        self.validate(session("s1"), moderation("s1", True, excluded=True))
        self.validate(session("s1"), moderation("s1", None, excluded=True))

    def test_returning_unknown_primary_outcome_remains_allowed(self) -> None:
        self.validate(session("s1"), moderation("s1", None, cohort="returning"))


if __name__ == "__main__":
    unittest.main()
