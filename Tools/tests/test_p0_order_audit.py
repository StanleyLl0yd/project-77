import csv
import tempfile
import unittest
from pathlib import Path
from types import SimpleNamespace
import sys

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))

import p0_order_audit as audit
import p0_order_plan as plan_tool


class P0OrderAuditTests(unittest.TestCase):
    def _write_plan(self, root: Path):
        assignments = plan_tool.generate_assignments(2, "P0-TEST")
        path = root / "orders.csv"
        plan_tool.write_csv(path, assignments)
        return path, assignments

    def _session(self, session_id: str, playtest_id: str, variant: str):
        return SimpleNamespace(
            session_id=session_id,
            variant=variant,
            metadata={
                "session_id": session_id,
                "playtest_id": playtest_id,
                "prototype_variant": variant,
            },
        )

    def _moderation(self, position: int):
        return SimpleNamespace(variant_order_index=position)

    def test_generated_order_plan_loads_with_exact_variant_mapping(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            path, assignments = self._write_plan(Path(temp))
            loaded = audit.load_order_plan(path)
        first = assignments[0]
        self.assertEqual(
            tuple(plan_tool.VARIANTS[code] for code in first.order),
            loaded[first.playtest_id],
        )

    def test_three_assigned_sessions_are_verified(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            path, assignments = self._write_plan(Path(temp))
            loaded = audit.load_order_plan(path)
        assignment = assignments[0]
        variants = [plan_tool.VARIANTS[code] for code in assignment.order]
        sessions = [
            self._session(f"s{index}", assignment.playtest_id, variant)
            for index, variant in enumerate(variants, start=1)
        ]
        moderation = {
            f"s{index}": self._moderation(index)
            for index in range(1, 4)
        }
        result = audit.audit_assignments(sessions, moderation, loaded)
        self.assertTrue(result.ok)
        self.assertEqual(3, result.verified_sessions)
        self.assertEqual((), result.warnings)

    def test_wrong_variant_for_order_position_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            path, assignments = self._write_plan(Path(temp))
            loaded = audit.load_order_plan(path)
        assignment = assignments[0]
        expected = [plan_tool.VARIANTS[code] for code in assignment.order]
        wrong = expected[1]
        session = self._session("s1", assignment.playtest_id, wrong)
        result = audit.audit_assignments([session], {"s1": self._moderation(1)}, loaded)
        self.assertFalse(result.ok)
        self.assertTrue(any("expects" in error for error in result.errors))

    def test_missing_moderation_is_warning_not_fabricated_order(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            path, assignments = self._write_plan(Path(temp))
            loaded = audit.load_order_plan(path)
        assignment = assignments[0]
        variant = plan_tool.VARIANTS[assignment.order[0]]
        session = self._session("s1", assignment.playtest_id, variant)
        result = audit.audit_assignments([session], {}, loaded)
        self.assertTrue(result.ok)
        self.assertEqual(0, result.verified_sessions)
        self.assertTrue(any("no moderation row" in warning for warning in result.warnings))

    def test_duplicate_order_slot_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            path, assignments = self._write_plan(Path(temp))
            loaded = audit.load_order_plan(path)
        assignment = assignments[0]
        first_variant = plan_tool.VARIANTS[assignment.order[0]]
        second_variant = plan_tool.VARIANTS[assignment.order[1]]
        sessions = [
            self._session("s1", assignment.playtest_id, first_variant),
            self._session("s2", assignment.playtest_id, second_variant),
        ]
        moderation = {"s1": self._moderation(1), "s2": self._moderation(1)}
        result = audit.audit_assignments(sessions, moderation, loaded)
        self.assertFalse(result.ok)
        self.assertTrue(any("order position 1" in error for error in result.errors))

    def test_malformed_order_csv_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "bad.csv"
            with path.open("w", encoding="utf-8", newline="") as handle:
                writer = csv.writer(handle)
                writer.writerow(audit.REQUIRED_COLUMNS)
                writer.writerow(["P0-X", "A", "wrong", "B", "path_expedition_routing", "C", "flow_network_restoration"])
            with self.assertRaises(audit.OrderAuditError):
                audit.load_order_plan(path)


if __name__ == "__main__":
    unittest.main()
