import unittest

from p1_event_audit import audit_p1_event_sequence


def event(name, **properties):
    value = {
        "event_name": name,
        "level_id": None,
        "level_revision": None,
    }
    value.update(properties)
    return value


class P1EventAuditTests(unittest.TestCase):
    def test_meta_happy_path(self):
        events = [
            event("prototype_start"),
            event("level_start", level_id="A-001", level_revision=1, attempt_index=1),
            event("level_complete", level_id="A-001", level_revision=1, attempt_index=1),
            event("reward_shown", reward_id="p1-level-001", scrap_amount=2, energy_amount=1),
            event("reward_claimed", reward_id="p1-level-001", scrap_amount=2, energy_amount=1),
            event("next_puzzle_offered", level_id="A-001", level_revision=1, offer_context="post_reward", next_level_id="A-002", offer_sequence_index=1),
            event("next_puzzle_clicked", level_id="A-001", level_revision=1, offer_context="post_reward", next_level_id="A-002", offer_sequence_index=1),
            event("level_start", level_id="A-002", level_revision=1, attempt_index=1),
            event("level_complete", level_id="A-002", level_revision=1, attempt_index=1),
            event("reward_shown", reward_id="p1-level-002", scrap_amount=2, energy_amount=1),
            event("reward_claimed", reward_id="p1-level-002", scrap_amount=2, energy_amount=1),
            event("resource_spend", resource_type="scrap", amount=4, sink_id="island_generator", balance_after=0),
            event("resource_spend", resource_type="energy", amount=2, sink_id="island_generator", balance_after=0),
            event("generator_repair", scrap_spent=4, energy_spent=2),
            event("island_change", change_type="power_on", caused_by="generator_repair"),
            event("area_unlock"),
            event("island_change", change_type="unlock_visual", caused_by="area_unlock"),
            event("robot_77_discovered"),
            event("next_puzzle_offered", level_id="A-002", level_revision=1, offer_context="post_77_discovery", next_level_id="A-003", offer_sequence_index=2),
            event("next_puzzle_clicked", level_id="A-002", level_revision=1, offer_context="post_77_discovery", next_level_id="A-003", offer_sequence_index=2),
        ]

        result = audit_p1_event_sequence(events)
        self.assertEqual(result.errors, ())

    def test_repair_before_spend_is_rejected(self):
        events = [
            event("prototype_start"),
            event("generator_repair", scrap_spent=4, energy_spent=2),
        ]
        result = audit_p1_event_sequence(events)
        self.assertTrue(any("scrap_spent" in error for error in result.errors))
        self.assertTrue(any("energy_spent" in error for error in result.errors))

    def test_robot_before_area_is_rejected(self):
        result = audit_p1_event_sequence([
            event("prototype_start"),
            event("robot_77_discovered"),
        ])
        self.assertTrue(any("precedes area_unlock" in error for error in result.errors))


if __name__ == "__main__":
    unittest.main()
