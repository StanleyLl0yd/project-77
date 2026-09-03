import struct
import tempfile
import unittest
import zipfile
from pathlib import Path
import sys

TOOLS = Path(__file__).resolve().parents[1]
ROOT = TOOLS.parent
sys.path.insert(0, str(TOOLS))

import p0_freeze_manifest as freeze


def _elf64(load_alignment: int = 0x4000) -> bytes:
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


def _write_test_apk(path: Path, marker: bytes = b"APK Sig Block 42") -> None:
    with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        archive.writestr("lib/arm64-v8a/libunity.so", _elf64())
        info = zipfile.ZipInfo("META-INF/project77-signature-marker.bin")
        info.compress_type = zipfile.ZIP_STORED
        archive.writestr(info, marker)


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

    def test_manifest_binds_build_and_preregistered_gate_to_commit(self) -> None:
        commit = "a" * 40
        manifest = freeze.build_manifest(
            "P0-TEST",
            commit,
            "0.0.1-prototype+" + commit[:12],
            "2026-09-03T00:00:00Z",
            ROOT,
        )
        freeze.verify_manifest(manifest, commit, ROOT)
        self.assertEqual(freeze.MANIFEST_SCHEMA_VERSION, manifest["freeze_manifest_schema_version"])
        self.assertEqual("P0-TEST", manifest["batch_id"])
        self.assertEqual(commit, manifest["commit_sha"])
        self.assertEqual(freeze.GATE_PLAN, manifest["gate_plan"])
        self.assertIsNone(manifest["order_plan"])
        self.assertIsNone(manifest["artifact"])
        self.assertEqual(64, len(manifest["batch_fingerprint"]))

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

    def test_order_plan_hash_is_bound_and_required_when_frozen(self) -> None:
        commit = "e" * 40
        with tempfile.TemporaryDirectory() as temp:
            order_path = Path(temp) / "orders.csv"
            order_path.write_text("playtest_id,order\nP0-001,ABC\n", encoding="utf-8")
            manifest = freeze.build_manifest(
                "P0-TEST",
                commit,
                "0.0.1-prototype+" + commit[:12],
                "2026-09-03T00:00:00Z",
                ROOT,
                order_path,
            )
            self.assertEqual(order_path.name, manifest["order_plan"]["filename"])
            self.assertEqual(64, len(manifest["order_plan"]["sha256"]))
            freeze.verify_manifest(manifest, commit, ROOT, order_path)
            with self.assertRaises(freeze.FreezeError):
                freeze.verify_manifest(manifest, commit, ROOT)

    def test_tampered_order_plan_is_rejected(self) -> None:
        commit = "f" * 40
        with tempfile.TemporaryDirectory() as temp:
            order_path = Path(temp) / "orders.csv"
            order_path.write_text("original\n", encoding="utf-8")
            manifest = freeze.build_manifest(
                "P0-TEST",
                commit,
                "0.0.1-prototype+" + commit[:12],
                "2026-09-03T00:00:00Z",
                ROOT,
                order_path,
            )
            order_path.write_text("changed\n", encoding="utf-8")
            with self.assertRaises(freeze.FreezeError):
                freeze.verify_order_plan_binding(manifest, order_path)

    def test_verified_apk_is_bound_and_required_when_frozen(self) -> None:
        commit = "1" * 40
        with tempfile.TemporaryDirectory() as temp:
            artifact_path = Path(temp) / "Project77.apk"
            _write_test_apk(artifact_path)
            manifest = freeze.build_manifest(
                "P0-TEST",
                commit,
                "0.0.1-prototype+" + commit[:12],
                "2026-09-03T00:00:00Z",
                ROOT,
                artifact_path=artifact_path,
            )
            record = manifest["artifact"]
            self.assertEqual("Project77.apk", record["filename"])
            self.assertEqual(["arm64-v8a"], record["abis"])
            self.assertTrue(record["arm64_elf_16kb_compatible"])
            self.assertTrue(record["signature_marker_present"])
            self.assertEqual(64, len(record["sha256"]))
            freeze.verify_manifest(manifest, commit, ROOT, artifact_path=artifact_path)
            with self.assertRaises(freeze.FreezeError):
                freeze.verify_manifest(manifest, commit, ROOT)

    def test_different_apk_is_rejected_against_frozen_artifact(self) -> None:
        commit = "2" * 40
        with tempfile.TemporaryDirectory() as temp:
            artifact_path = Path(temp) / "Project77.apk"
            other_path = Path(temp) / "Other.apk"
            _write_test_apk(artifact_path)
            _write_test_apk(other_path)
            with zipfile.ZipFile(other_path, "a", compression=zipfile.ZIP_DEFLATED) as archive:
                archive.writestr("extra.txt", "different exact artifact")
            manifest = freeze.build_manifest(
                "P0-TEST",
                commit,
                "0.0.1-prototype+" + commit[:12],
                "2026-09-03T00:00:00Z",
                ROOT,
                artifact_path=artifact_path,
            )
            with self.assertRaises(freeze.FreezeError):
                freeze.verify_artifact_binding(manifest, other_path)

    def test_aab_cannot_be_bound_as_external_p0_playtest_artifact(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            aab_path = Path(temp) / "Project77.aab"
            with zipfile.ZipFile(aab_path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
                archive.writestr("base/lib/arm64-v8a/libunity.so", _elf64())
                archive.writestr("META-INF/KEY0.SF", b"signature")
                archive.writestr("META-INF/KEY0.RSA", b"signature")
            with self.assertRaises(freeze.FreezeError):
                freeze._artifact_record(aab_path)


if __name__ == "__main__":
    unittest.main()
