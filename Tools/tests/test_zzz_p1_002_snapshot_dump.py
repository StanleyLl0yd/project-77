import json
import sys
import unittest
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))

import p1_freeze_manifest as freeze


class P1002SnapshotDumpTests(unittest.TestCase):
    def test_emit_snapshot_for_artifact_binding(self) -> None:
        commit = "96a0f0ae0ea3788338f6c676342e507e3bdcaf0f"
        manifest = freeze.build_manifest(
            "P1-002",
            commit,
            "0.0.1-prototype+96a0f0ae0ea3",
            "2026-09-11T15:48:00Z",
            test_plan=freeze.build_test_plan(
                "portrait",
                ["Joy 4 | Android 10 / VOS 3.0 | 1080x2340 | portrait"],
                30,
            ),
        )
        self.assertEqual(commit, manifest["commit_sha"])
        self.assertIsNone(manifest["artifact"])
        print("P1_002_SNAPSHOT=" + json.dumps(manifest, sort_keys=True, separators=(",", ":")))


if __name__ == "__main__":
    unittest.main()
