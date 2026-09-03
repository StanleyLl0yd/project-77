import unittest
from pathlib import Path
import sys

TOOLS = Path(__file__).resolve().parents[1]
ROOT = TOOLS.parent
sys.path.insert(0, str(TOOLS))

import p0_batch_report as batch
import p0_freeze_manifest as freeze
import p0_gate_report as gate


class P0GateReportTests(unittest.TestCase):
    def _manifest(self) -> dict:
        commit = "e" * 40
        return freeze.build_manifest(
            "P0-TEST",
            commit,
            "0.0.1-prototype+" + commit[:12],
            "2026-09-03T00:00:00Z",
            ROOT,
        )

    def _session(self, manifest: dict, variant: str, events=None) -> batch.SessionData:
        levels = [
            {"id": record["id"], "revision": record["revision"]}
            for record in manifest["levels"][variant]
        ]
        metadata = {
            "session_id": "session-1",
            "build_version": manifest["build_version"],
            "commit_sha": manifest["commit_sha"],
            "event_schema_version": manifest["event_schema_version"],
            "unity_version": manifest["unity_version"],
            "prototype_variant": variant,
            "levels": levels,
        }
        return batch.SessionData(
            Path("session_metadata.json"),
            Path("session_events.jsonl"),
            metadata,
            [] if events is None else events,
        )

    def test_matching_session_metadata_is_accepted(self) -> None:
        manifest = self._manifest()
        session = self._session(manifest, "energy_routing")
        gate.validate_sessions_against_freeze([session], manifest)

    def test_commit_mismatch_is_rejected(self) -> None:
        manifest = self._manifest()
        session = self._session(manifest, "energy_routing")
        session.metadata["commit_sha"] = "f" * 40
        with self.assertRaises(gate.GateReportError):
            gate.validate_sessions_against_freeze([session], manifest)

    def test_level_revision_mismatch_is_rejected(self) -> None:
        manifest = self._manifest()
        session = self._session(manifest, "flow_network_restoration")
        session.metadata["levels"][0]["revision"] += 1
        with self.assertRaises(gate.GateReportError):
            gate.validate_sessions_against_freeze([session], manifest)

    def test_operational_diagnostics_count_attempt_quality(self) -> None:
        manifest = self._manifest()
        first_level = manifest["levels"]["energy_routing"][0]["id"]
        events = [
            {"event_name": "level_start", "level_id": first_level},
            {"event_name": "invalid_interaction", "level_id": first_level},
            {"event_name": "level_complete", "level_id": first_level},
            {"event_name": "next_puzzle_offered", "level_id": first_level},
            {"event_name": "level_retry", "level_id": first_level},
            {"event_name": "level_start", "level_id": first_level},
            {"event_name": "level_quit", "level_id": first_level},
        ]
        session = self._session(manifest, "energy_routing", events)
        moderation = {
            "session-1": batch.ModerationRecord(
                session_id="session-1",
                cohort="fresh",
                variant_order_index=1,
                tutorial_completed=True,
                unaided_comprehension=True,
                help_required=False,
                voluntary_continuation=True,
                exclude_tutorial=False,
                exclude_unaided=False,
                exclude_voluntary=False,
                exclusion_reason="",
                moderator_id="M",
            )
        }
        diagnostics = gate.build_operational_diagnostics([session], moderation)["energy_routing"]
        self.assertEqual(2, diagnostics["attempts_started"])
        self.assertEqual(2, diagnostics["attempts_terminal"])
        self.assertEqual(0, diagnostics["open_attempts"])
        self.assertEqual(1, diagnostics["first_level_completed_sessions"])
        self.assertEqual(1, diagnostics["invalid_interactions"])
        self.assertEqual(0.5, diagnostics["invalid_interactions_per_attempt"])
        self.assertFalse(diagnostics["fresh_exposure_target_met"])

    def test_open_attempt_is_visible_as_data_quality_gap(self) -> None:
        manifest = self._manifest()
        first_level = manifest["levels"]["path_expedition_routing"][0]["id"]
        session = self._session(
            manifest,
            "path_expedition_routing",
            [{"event_name": "level_start", "level_id": first_level}],
        )
        diagnostics = gate.build_operational_diagnostics([session], {})["path_expedition_routing"]
        self.assertEqual(1, diagnostics["open_attempts"])


if __name__ == "__main__":
    unittest.main()
