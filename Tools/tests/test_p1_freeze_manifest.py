import copy
import unittest

import p1_freeze_manifest as freeze


class P1FreezeManifestTests(unittest.TestCase):
    def test_repository_snapshot_freezes_selected_core_and_meta_implementation(self):
        snapshot = freeze.build_repository_snapshot()
        self.assertEqual(snapshot["prototype_variant"], "selected_meta")
        self.assertEqual(snapshot["selected_core"], "energy_routing")
        self.assertEqual(len(snapshot["levels"]), 10)
        self.assertIn("meta_domain", snapshot["implementation"])

    def test_unbound_manifest_round_trip_verifies_for_tooling_smoke(self):
        commit = "a" * 40
        manifest = freeze.build_manifest(
            "P1-CI",
            commit,
            "0.0.1-prototype+" + commit[:12],
            "2026-09-08T00:00:00Z",
        )
        freeze.verify_manifest(manifest, commit)

    def test_changed_fingerprint_is_rejected(self):
        commit = "b" * 40
        manifest = freeze.build_manifest(
            "P1-CI",
            commit,
            "0.0.1-prototype+" + commit[:12],
            "2026-09-08T00:00:00Z",
        )
        changed = copy.deepcopy(manifest)
        changed["freeze_fingerprint"] = "0" * 64
        with self.assertRaises(freeze.FreezeError):
            freeze.verify_manifest(changed, commit)

    def test_build_version_must_bind_commit(self):
        with self.assertRaises(freeze.FreezeError):
            freeze.build_manifest(
                "P1-CI",
                "c" * 40,
                "0.0.1-prototype+wrong",
                "2026-09-08T00:00:00Z",
            )


if __name__ == "__main__":
    unittest.main()
