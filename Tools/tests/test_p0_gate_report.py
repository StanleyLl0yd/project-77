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

    def _session(self, manifest: dict, variant: str) -> batch.SessionData:
        levels = [
            {"id": record["id"], "revision": record["revision"]}
            for record in manifest["levels"][variant]
        ]
        metadata = {
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
            [],
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


if __name__ == "__main__":
    unittest.main()
