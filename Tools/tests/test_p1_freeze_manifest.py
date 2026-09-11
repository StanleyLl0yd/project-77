import copy
import struct
import tempfile
import unittest
import zipfile
from pathlib import Path

import p1_freeze_manifest as freeze


def elf64(load_alignment: int = 0x4000) -> bytes:
    data = bytearray(64 + 56)
    data[0:4] = b"\x7fELF"
    data[4] = 2
    data[5] = 1
    data[6] = 1
    struct.pack_into("<Q", data, 32, 64)
    struct.pack_into("<H", data, 52, 64)
    struct.pack_into("<H", data, 54, 56)
    struct.pack_into("<H", data, 56, 1)
    struct.pack_into("<I", data, 64, 1)
    struct.pack_into("<Q", data, 64 + 48, load_alignment)
    return bytes(data)


def write_marked_apk(path: Path, commit: str, build_version: str) -> None:
    with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        archive.writestr("lib/arm64-v8a/libunity.so", elf64())
        archive.writestr(
            "assets/bin/Data/build-info",
            f"commit_sha={commit}\nbuild_version={build_version}\n".encode("utf-8"),
        )
        archive.comment = freeze.android_artifact.APK_SIGNING_BLOCK_MAGIC


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
            test_plan=freeze.build_test_plan(
                "portrait",
                ["CI device | Android 10 | 1080x2340 | portrait"],
            ),
        )
        freeze.verify_manifest(manifest, commit)
        self.assertEqual(freeze.MANIFEST_SCHEMA_VERSION, manifest["freeze_manifest_schema_version"])
        self.assertIsNone(manifest["artifact"])

    def test_changed_fingerprint_is_rejected(self):
        commit = "b" * 40
        manifest = freeze.build_manifest(
            "P1-CI",
            commit,
            "0.0.1-prototype+" + commit[:12],
            "2026-09-08T00:00:00Z",
            test_plan=freeze.build_test_plan(
                "portrait",
                ["CI device | Android 10 | 1080x2340 | portrait"],
            ),
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
                test_plan=freeze.build_test_plan(
                    "portrait",
                    ["CI device | Android 10 | 1080x2340 | portrait"],
                ),
            )

    def test_test_plan_requires_device_target(self):
        with self.assertRaises(freeze.FreezeError):
            freeze.build_test_plan("portrait", [])

    def test_test_plan_is_part_of_batch_fingerprint(self):
        commit = "d" * 40
        manifest = freeze.build_manifest(
            "P1-CI",
            commit,
            "0.0.1-prototype+" + commit[:12],
            "2026-09-08T00:00:00Z",
            test_plan=freeze.build_test_plan(
                "portrait",
                ["Primary device"],
            ),
        )
        changed = copy.deepcopy(manifest)
        changed["test_plan"]["orientation"] = "landscape"
        with self.assertRaises(freeze.FreezeError):
            freeze.verify_manifest(changed, commit)

    def test_bound_manifest_verifies_exact_embedded_source_identity(self):
        commit = "e" * 40
        build_version = "0.0.1-prototype+" + commit[:12]
        with tempfile.TemporaryDirectory() as temp_dir:
            artifact = Path(temp_dir) / "project77.apk"
            write_marked_apk(artifact, commit, build_version)
            manifest = freeze.build_manifest(
                "P1-CI",
                commit,
                build_version,
                "2026-09-08T00:00:00Z",
                test_plan=freeze.build_test_plan("portrait", ["Primary device"]),
                artifact_path=artifact,
            )
            freeze.verify_manifest(manifest, commit, artifact_path=artifact)

        self.assertTrue(manifest["artifact"]["embedded_commit_sha_verified"])
        self.assertTrue(manifest["artifact"]["embedded_build_version_verified"])
        self.assertFalse(
            manifest["artifact"]["embedded_text_marker_verification_is_cryptographic"]
        )

    def test_stale_apk_commit_is_rejected(self):
        expected_commit = "f" * 40
        expected_build = "0.0.1-prototype+" + expected_commit[:12]
        stale_commit = "1" * 40
        stale_build = "0.0.1-prototype+" + stale_commit[:12]
        with tempfile.TemporaryDirectory() as temp_dir:
            artifact = Path(temp_dir) / "stale.apk"
            write_marked_apk(artifact, stale_commit, stale_build)
            with self.assertRaisesRegex(freeze.FreezeError, "embedded text marker"):
                freeze.build_manifest(
                    "P1-CI",
                    expected_commit,
                    expected_build,
                    "2026-09-08T00:00:00Z",
                    test_plan=freeze.build_test_plan("portrait", ["Primary device"]),
                    artifact_path=artifact,
                )

    def test_stale_apk_build_version_is_rejected(self):
        commit = "2" * 40
        expected_build = "0.0.1-prototype+" + commit[:12]
        with tempfile.TemporaryDirectory() as temp_dir:
            artifact = Path(temp_dir) / "stale-build.apk"
            write_marked_apk(artifact, commit, "0.0.1-prototype+stale-build")
            with self.assertRaisesRegex(freeze.FreezeError, "build_version"):
                freeze.build_manifest(
                    "P1-CI",
                    commit,
                    expected_build,
                    "2026-09-08T00:00:00Z",
                    test_plan=freeze.build_test_plan("portrait", ["Primary device"]),
                    artifact_path=artifact,
                )


if __name__ == "__main__":
    unittest.main()
