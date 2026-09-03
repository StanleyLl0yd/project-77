import unittest
from pathlib import Path
import sys

TOOLS = Path(__file__).resolve().parents[1]
ROOT = TOOLS.parent
sys.path.insert(0, str(TOOLS))

import p0_freeze_manifest as freeze


class P0FreezeManifestTests(unittest.TestCase):
    def test_repository_snapshot_covers_exact_p0_content(self) -> None:
        snapshot = freeze.build_repository_snapshot(ROOT)
        self.assertEqual("6000.3.22f1", snapshot["unity_version"])
        self.assertEqual("17.3.0", snapshot["urp_version"])
        self.assertEqual(1, snapshot["event_schema_version"])
        self.assertEqual(1, snapshot["metadata_schema_version"])
        self.assertEqual(64, len(snapshot["freeze_fingerprint"]))
        self.assertEqual(set(freeze.VARIANTS), set(snapshot["levels"]))
        for records in snapshot["levels"].values():
            self.assertEqual(10, len(records))
            self.assertTrue(all(len(record["sha256"]) == 64 for record in records))

    def test_manifest_binds_build_to_full_commit_and_verifies(self) -> None:
        commit = "a" * 40
        manifest = freeze.build_manifest(
            "P0-TEST",
            commit,
            "0.0.1-prototype+" + commit[:12],
            "2026-09-03T00:00:00Z",
            ROOT,
        )
        freeze.verify_manifest(manifest, commit, ROOT)
        self.assertEqual("P0-TEST", manifest["batch_id"])
        self.assertEqual(commit, manifest["commit_sha"])

    def test_manifest_rejects_build_not_bound_to_commit(self) -> None:
        with self.assertRaises(freeze.FreezeError):
            freeze.build_manifest(
                "P0-TEST",
                "b" * 40,
                "0.0.1-prototype+wrong",
                "2026-09-03T00:00:00Z",
                ROOT,
            )

    def test_verify_rejects_different_checkout_commit(self) -> None:
        commit = "c" * 40
        manifest = freeze.build_manifest(
            "P0-TEST",
            commit,
            "0.0.1-prototype+" + commit[:12],
            "2026-09-03T00:00:00Z",
            ROOT,
        )
        with self.assertRaises(freeze.FreezeError):
            freeze.verify_manifest(manifest, "d" * 40, ROOT)


if __name__ == "__main__":
    unittest.main()
