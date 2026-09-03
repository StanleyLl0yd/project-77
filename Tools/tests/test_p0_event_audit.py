import unittest
from pathlib import Path
import sys

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))

import p0_event_audit as audit


def event(name: str, **properties):
    value = {"event_name": name}
    value.update(properties)
    return value


class P0EventAuditTests(unittest.TestCase):
    def test_valid_complete_retry_and_exit_sequence_passes(self) -> None:
        events = [
            event("prototype_start"),
            event("tutorial_exposed"),
            event("level_start", level_id="A-001", level_revision=1, attempt_index=1),
            event("invalid_interaction", level_id="A-001", level_revision=1, attempt_index=1),
            event("level_retry", level_id="A-001", level_revision=1, previous_attempt_index=1, new_attempt_index=2),
            event("level_start", level_id="A-001", level_revision=1, attempt_index=2),
            event("level_complete", level_id="A-001", level_revision=1, attempt_index=2),
            event(
                "next_puzzle_offered",
                level_id="A-001",
                level_revision=1,
                offer_context="post_level",
                next_level_id="A-002",
                offer_sequence_index=1,
            ),
            event(
                "next_puzzle_clicked",
                level_id="A-001",
                level_revision=1,
                offer_context="post_level",
                next_level_id="A-002",
                offer_sequence_index=1,
            ),
            event("level_start", level_id="A-002", level_revision=1, attempt_index=1),
            event("level_quit", level_id="A-002", level_revision=1, attempt_index=1),
            event("session_end"),
        ]
        result = audit.audit_event_sequence(events)
        self.assertTrue(result.ok)
        self.assertEqual((), result.warnings)

    def test_click_without_offer_is_rejected(self) -> None:
        events = [
            event("prototype_start"),
            event("level_start", level_id="A-001", level_revision=1, attempt_index=1),
            event("level_complete", level_id="A-001", level_revision=1, attempt_index=1),
            event(
                "next_puzzle_clicked",
                level_id="A-001",
                level_revision=1,
                offer_context="post_level",
                next_level_id="A-002",
                offer_sequence_index=1,
            ),
            event("session_end"),
        ]
        result = audit.audit_event_sequence(events)
        self.assertFalse(result.ok)
        self.assertTrue(any("without an outstanding offer" in error for error in result.errors))

    def test_retry_index_must_increment_by_one(self) -> None:
        events = [
            event("prototype_start"),
            event("level_start", level_id="B-001", level_revision=1, attempt_index=1),
            event("level_retry", level_id="B-001", level_revision=1, previous_attempt_index=1, new_attempt_index=3),
            event("level_start", level_id="B-001", level_revision=1, attempt_index=3),
            event("level_quit", level_id="B-001", level_revision=1, attempt_index=3),
            event("session_end"),
        ]
        result = audit.audit_event_sequence(events)
        self.assertFalse(result.ok)
        self.assertTrue(any("previous_attempt_index + 1" in error for error in result.errors))

    def test_retry_after_completed_attempt_is_rejected(self) -> None:
        events = [
            event("prototype_start"),
            event("level_start", level_id="A-001", level_revision=1, attempt_index=1),
            event("level_complete", level_id="A-001", level_revision=1, attempt_index=1),
            event(
                "next_puzzle_offered",
                level_id="A-001",
                level_revision=1,
                offer_context="post_level",
                next_level_id="A-002",
                offer_sequence_index=1,
            ),
            event("level_retry", level_id="A-001", level_revision=1, previous_attempt_index=1, new_attempt_index=2),
            event("level_start", level_id="A-001", level_revision=1, attempt_index=2),
            event("level_quit", level_id="A-001", level_revision=1, attempt_index=2),
            event("session_end"),
        ]
        result = audit.audit_event_sequence(events)
        self.assertFalse(result.ok)
        self.assertTrue(any("no active/retryable previous attempt" in error for error in result.errors))

    def test_missing_session_end_is_warning_not_structural_failure(self) -> None:
        events = [
            event("prototype_start"),
            event("level_start", level_id="C-001", level_revision=1, attempt_index=1),
            event("level_complete", level_id="C-001", level_revision=1, attempt_index=1),
            event(
                "next_puzzle_offered",
                level_id="C-001",
                level_revision=1,
                offer_context="post_level",
                next_level_id="C-002",
                offer_sequence_index=1,
            ),
        ]
        result = audit.audit_event_sequence(events)
        self.assertTrue(result.ok)
        self.assertTrue(any("session_end is missing" in warning for warning in result.warnings))

    def test_second_level_must_match_clicked_next_level(self) -> None:
        events = [
            event("prototype_start"),
            event("level_start", level_id="C-001", level_revision=1, attempt_index=1),
            event("level_complete", level_id="C-001", level_revision=1, attempt_index=1),
            event(
                "next_puzzle_offered",
                level_id="C-001",
                level_revision=1,
                offer_context="post_level",
                next_level_id="C-002",
                offer_sequence_index=1,
            ),
            event(
                "next_puzzle_clicked",
                level_id="C-001",
                level_revision=1,
                offer_context="post_level",
                next_level_id="C-002",
                offer_sequence_index=1,
            ),
            event("level_start", level_id="C-003", level_revision=1, attempt_index=1),
            event("level_quit", level_id="C-003", level_revision=1, attempt_index=1),
            event("session_end"),
        ]
        result = audit.audit_event_sequence(events)
        self.assertFalse(result.ok)
        self.assertTrue(any("expected next level" in error for error in result.errors))


if __name__ == "__main__":
    unittest.main()
