import struct
import tempfile
import unittest
import zipfile
from pathlib import Path
import sys

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))

import verify_android_artifact as verify


def elf64(load_alignment: int) -> bytes:
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


def write_aab(
    path: Path,
    alignment: int = 0x4000,
    extra_abi: bool = False,
    include_il2cpp: bool = True,
) -> None:
    with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        archive.writestr("base/lib/arm64-v8a/libunity.so", elf64(alignment))
        if include_il2cpp:
            archive.writestr("base/lib/arm64-v8a/libil2cpp.so", elf64(alignment))
        if extra_abi:
            archive.writestr("base/lib/x86_64/libunity.so", elf64(alignment))
        archive.writestr("META-INF/KEY0.SF", b"signature file")
        archive.writestr("META-INF/KEY0.RSA", b"signature block")


def write_apk(path: Path, payload: bytes = b"", include_il2cpp: bool = True) -> None:
    with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        archive.writestr("lib/arm64-v8a/libunity.so", elf64(0x4000))
        if include_il2cpp:
            archive.writestr("lib/arm64-v8a/libil2cpp.so", elf64(0x4000))
        archive.writestr("assets/bin/Data/build-info", payload)


class VerifyAndroidArtifactTests(unittest.TestCase):
    def test_signed_aab_with_arm64_il2cpp_and_16kb_elf_passes(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "project77.aab"
            write_aab(path)
            result = verify.inspect_artifact(path)
        self.assertEqual(["arm64-v8a"], result["abis"])
        self.assertTrue(result["unity_player_library_present"])
        self.assertTrue(result["il2cpp_library_present"])
        self.assertTrue(result["arm64_elf_16kb_compatible"])
        self.assertTrue(result["signature_marker_present"])
        self.assertFalse(result["signature_marker_is_cryptographic_verification"])
        self.assertEqual({}, result["embedded_text_markers"])

    def test_artifact_without_il2cpp_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "mono-like.apk"
            write_apk(path, include_il2cpp=False)
            with self.assertRaisesRegex(verify.ArtifactError, "libil2cpp.so"):
                verify.inspect_artifact(path, require_signature_marker=False)

    def test_aab_with_4kb_pt_load_alignment_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "bad.aab"
            write_aab(path, alignment=0x1000)
            with self.assertRaises(verify.ArtifactError):
                verify.inspect_artifact(path)

    def test_extra_abi_is_rejected_by_project_baseline(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "extra.aab"
            write_aab(path, extra_abi=True)
            with self.assertRaises(verify.ArtifactError):
                verify.inspect_artifact(path)

    def test_aab_without_signature_markers_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "unsigned.aab"
            with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
                archive.writestr("base/lib/arm64-v8a/libunity.so", elf64(0x4000))
                archive.writestr("base/lib/arm64-v8a/libil2cpp.so", elf64(0x4000))
            with self.assertRaises(verify.ArtifactError):
                verify.inspect_artifact(path)

    def test_uncompressed_apk_library_must_be_16kb_zip_aligned(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "unaligned.apk"
            with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_STORED) as archive:
                archive.writestr("lib/arm64-v8a/libunity.so", elf64(0x4000))
                archive.writestr("lib/arm64-v8a/libil2cpp.so", elf64(0x4000))
            with self.assertRaises(verify.ArtifactError):
                verify.inspect_artifact(path, require_signature_marker=False)

    def test_compressed_apk_skips_uncompressed_zip_alignment_rule(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "compressed.apk"
            write_apk(path)
            result = verify.inspect_artifact(path, require_signature_marker=False)
        self.assertTrue(result["arm64_elf_16kb_compatible"])
        self.assertTrue(result["apk_uncompressed_libs_16kb_zip_aligned"])
        self.assertTrue(result["il2cpp_library_present"])

    def test_exact_embedded_text_markers_pass(self) -> None:
        commit = "a" * 40
        build_version = "0.0.1-prototype+" + commit[:12]
        payload = f"commit={commit}\nbuild={build_version}\n".encode("utf-8")
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "marked.apk"
            write_apk(path, payload)
            result = verify.inspect_artifact(
                path,
                require_signature_marker=False,
                expected_text_markers={
                    "commit_sha": commit,
                    "build_version": build_version,
                },
            )

        self.assertEqual(
            {"commit_sha": True, "build_version": True},
            result["embedded_text_markers"],
        )
        self.assertFalse(result["embedded_text_marker_verification_is_cryptographic"])

    def test_missing_embedded_text_marker_is_rejected(self) -> None:
        commit = "b" * 40
        build_version = "0.0.1-prototype+" + commit[:12]
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "stale.apk"
            write_apk(path, f"commit={commit}\n".encode("utf-8"))
            with self.assertRaisesRegex(verify.ArtifactError, "build_version"):
                verify.inspect_artifact(
                    path,
                    require_signature_marker=False,
                    expected_text_markers={
                        "commit_sha": commit,
                        "build_version": build_version,
                    },
                )

    def test_marker_scan_handles_chunk_boundary(self) -> None:
        marker = "boundary-marker"
        prefix = b"x" * (verify.MARKER_SCAN_CHUNK_SIZE - 3)
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "boundary.apk"
            write_apk(path, prefix + marker.encode("utf-8") + b"tail")
            result = verify.inspect_artifact(
                path,
                require_signature_marker=False,
                expected_text_markers={"source": marker},
            )

        self.assertTrue(result["embedded_text_markers"]["source"])


if __name__ == "__main__":
    unittest.main()
