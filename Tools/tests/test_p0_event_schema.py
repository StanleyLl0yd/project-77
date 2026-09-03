import unittest
from pathlib import Path
import sys

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))

import p0_event_schema as schema


def envelope(name: str, **properties):
    event = {
        "event_schema_version": 1,
        "event_name": name,
        "timestamp_utc_ms": 1_000,
        "session_id": "session-001",
        "playtest_id": "P0-001-001",
        "build_version": "0.0.1-prototype+abcdef123456",
        "prototype_variant": "energy_routing",
        "level_id": None,
        "level_revision": None,
        "device_tier": "unknown",
        "screen_orientation": "portrait",
    }
    event.update(properties)
    return event


def level_event(name: str, **properties):
    return envelope(name, level_id="A-001", level_revision=1, **properties)


class P0EventSchemaTests(unittest.TestCase):
    def test_every_canonical_event_has_a_schema_valid_fixture(self) -> None:
        events = [
            envelope("prototype_start", entry_point="variant_select", core_variant="energy_routing"),
            envelope(
                "tutorial_exposed",
                tutorial_step_id="energy_routing_core_instruction",
                exposure_index=1,
                presentation_type="text",
                auto_advance_ms=None,
            ),
            level_event(
                "level_start",
                attempt_index=1,
                core_variant="energy_routing",
                level_sequence_index=1,
            ),
            level_event(
                "level_complete",
                attempt_index=1,
                duration_ms=45_000,
                valid_interaction_count=4,
                invalid_interaction_count=1,
                core_variant="energy_routing",
                move_count=None,
                path_action_count=5,
            ),
            level_event(
                "level_fail",
                attempt_index=1,
                duration_ms=20_000,
                fail_reason="same_cell_collision",
                core_variant="energy_routing",
            ),
            level_event(
                "level_retry",
                previous_attempt_index=1,
                new_attempt_index=2,
                retry_source="manual_restart",
            ),
            level_event(
                "level_quit",
                attempt_index=2,
                duration_ms=2_000,
                quit_destination="app_exit",
            ),
            level_event(
                "invalid_interaction",
                interaction_type="path_end",
                invalid_reason="wrong_target",
                attempt_index=1,
                board_element_id=None,
            ),
            envelope(
                "reward_shown",
                reward_id="reward-001",
                reward_source="level_complete",
                scrap_amount=10,
                energy_amount=0,
            ),
            envelope(
                "reward_claimed",
                reward_id="reward-001",
                claim_mode="explicit",
                scrap_amount=10,
                energy_amount=0,
            ),
            envelope(
                "resource_spend",
                resource_type="scrap",
                amount=10,
                sink_id="generator-stage-1",
                balance_after=0,
            ),
            envelope(
                "generator_repair",
                repair_stage=1,
                scrap_spent=10,
                energy_spent=0,
                time_since_session_start_ms=60_000,
            ),
            envelope(
                "island_change",
                change_id="generator-powered",
                change_type="power_on",
                caused_by="generator_repair",
            ),
            envelope("area_unlock", area_id="generator-yard", unlock_source="generator_repair"),
            envelope(
                "robot_77_discovered",
                discovery_id="first-77-reveal",
                time_since_session_start_ms=90_000,
                levels_completed_before_discovery=3,
            ),
            level_event(
                "next_puzzle_offered",
                offer_context="post_level",
                next_level_id="A-002",
                offer_sequence_index=1,
            ),
            level_event(
                "next_puzzle_clicked",
                offer_context="post_level",
                next_level_id="A-002",
                ms_since_offer=2_000,
                offer_sequence_index=1,
            ),
            envelope(
                "session_end",
                duration_ms=120_000,
                levels_started=3,
                levels_completed=3,
                end_reason="user_exit",
                last_visible_step="next_puzzle_offered",
            ),
        ]

        self.assertEqual(set(schema.KNOWN_EVENTS), {event["event_name"] for event in events})
        for event in events:
            with self.subTest(event=event["event_name"]):
                self.assertEqual([], schema.validate_event(event))

    def test_missing_required_event_property_is_rejected(self) -> None:
        event = level_event(
            "level_complete",
            attempt_index=1,
            duration_ms=45_000,
            valid_interaction_count=4,
            core_variant="energy_routing",
        )
        errors = schema.validate_event(event)
        self.assertTrue(any("invalid_interaction_count" in error for error in errors))

    def test_boolean_is_not_accepted_as_integer(self) -> None:
        event = level_event(
            "level_start",
            attempt_index=True,
            core_variant="energy_routing",
            level_sequence_index=1,
        )
        errors = schema.validate_event(event)
        self.assertTrue(any("attempt_index" in error for error in errors))

    def test_unknown_enum_is_rejected(self) -> None:
        event = envelope("prototype_start", entry_point="mystery", core_variant="energy_routing")
        errors = schema.validate_event(event)
        self.assertTrue(any("entry_point" in error for error in errors))

    def test_level_identity_must_be_present_as_a_pair(self) -> None:
        event = envelope(
            "level_start",
            level_id="A-001",
            level_revision=None,
            attempt_index=1,
            core_variant="energy_routing",
            level_sequence_index=1,
        )
        errors = schema.validate_event(event)
        self.assertTrue(any("both be null or both be present" in error for error in errors))

    def test_unknown_event_name_is_rejected(self) -> None:
        errors = schema.validate_event(envelope("made_up_event"))
        self.assertTrue(any("unknown event_name" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
