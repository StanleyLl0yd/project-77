import tempfile
import unittest
from pathlib import Path
import sys

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))

import p0_order_plan as plan


class P0OrderPlanTests(unittest.TestCase):
    def test_full_block_uses_every_permutation_once(self) -> None:
        assignments = plan.generate_assignments(6, "batch")
        self.assertEqual(6, len(set(assignment.order for assignment in assignments)))
        self.assertEqual({"A", "B", "C"}, set(assignments[0].order))
        self.assertEqual(
            {"A": [2, 2, 2], "B": [2, 2, 2], "C": [2, 2, 2]},
            plan.position_counts(assignments),
        )
        self.assertTrue(all(value == 2 for value in plan.transition_counts(assignments).values()))

    def test_partial_block_keeps_each_position_within_one(self) -> None:
        assignments = plan.generate_assignments(10, "p0")
        plan.validate_balance(assignments)
        positions = plan.position_counts(assignments)
        for position in range(3):
            values = [positions[code][position] for code in plan.VARIANTS]
            self.assertLessEqual(max(values) - min(values), 1)

    def test_ids_are_anonymous_unique_and_stable(self) -> None:
        assignments = plan.generate_assignments(3, "P0-001")
        self.assertEqual(
            ["P0-001-001", "P0-001-002", "P0-001-003"],
            [assignment.playtest_id for assignment in assignments],
        )

    def test_csv_contains_codes_and_canonical_variant_names(self) -> None:
        assignments = plan.generate_assignments(2, "test")
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "orders.csv"
            plan.write_csv(path, assignments)
            text = path.read_text(encoding="utf-8")
        self.assertIn("playtest_id", text)
        self.assertIn("energy_routing", text)
        self.assertIn("path_expedition_routing", text)
        self.assertIn("flow_network_restoration", text)

    def test_invalid_prefix_is_rejected(self) -> None:
        with self.assertRaises(ValueError):
            plan.generate_assignments(1, "bad,prefix")


if __name__ == "__main__":
    unittest.main()
