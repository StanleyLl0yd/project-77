#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import struct
import sys
import zipfile
from pathlib import Path
from typing import Any

PAGE_SIZE = 16 * 1024
APK_SIGNING_BLOCK_MAGIC = b"APK Sig Block 42"


class ArtifactError(ValueError):
    pass


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _load_alignments(data: bytes, member: str) -> list[int]:
    if len(data) < 64 or data[:4] != b"\x7fELF":
        raise ArtifactError(f"{member}: not an ELF file")
    if data[4] != 2:
        raise ArtifactError(f"{member}: expected ELF64 for 64-bit Android artifact")
    if data[5] == 1:
        endian = "<"
    elif data[5] == 2:
        endian = ">"
    else:
        raise ArtifactError(f"{member}: unsupported ELF byte order")

    phoff = struct.unpack_from(endian + "Q", data, 32)[0]
    phentsize = struct.unpack_from(endian + "H", data, 54)[0]
    phnum = struct.unpack_from(endian + "H", data, 56)[0]
    if phnum < 1 or phentsize < 56:
        raise ArtifactError(f"{member}: invalid ELF program header table")
    if phoff + phentsize * phnum > len(data):
        raise ArtifactError(f"{member}: truncated ELF program header table")

    alignments: list[int] = []
    for index in range(phnum):
        offset = phoff + index * phentsize
        p_type = struct.unpack_from(endian + "I", data, offset)[0]
        if p_type != 1:
            continue
        p_align = struct.unpack_from(endian + "Q", data, offset + 48)[0]
        alignments.append(p_align)
    if not alignments:
        raise ArtifactError(f"{member}: ELF has no PT_LOAD segments")
    return alignments


def _zip_data_offset(handle, info: zipfile.ZipInfo) -> int:
    handle.seek(info.header_offset)
    header = handle.read(30)
    if len(header) != 30:
        raise ArtifactError(f"{info.filename}: truncated ZIP local header")
    signature, *_, name_length, extra_length = struct.unpack("<IHHHHHIIIHH", header)
    if signature != 0x04034B50:
        raise ArtifactError(f"{info.filename}: invalid ZIP local header")
    return info.header_offset + 30 + name_length + extra_length


def _native_members(kind: str, infos: list[zipfile.ZipInfo]) -> list[tuple[str, str, zipfile.ZipInfo]]:
    result: list[tuple[str, str, zipfile.ZipInfo]] = []
    for info in infos:
        parts = info.filename.split("/")
        if kind == "apk":
            if len(parts) >= 3 and parts[0] == "lib" and info.filename.endswith(".so"):
                result.append((parts[1], info.filename, info))
        else:
            if len(parts) >= 4 and parts[0] == "base" and parts[1] == "lib" and info.filename.endswith(".so"):
                result.append((parts[2], info.filename, info))
    return result


def _aab_signature_marker(infos: list[zipfile.ZipInfo]) -> bool:
    names = {info.filename.upper() for info in infos}
    has_sf = any(name.startswith("META-INF/") and name.endswith(".SF") for name in names)
    has_block = any(
        name.startswith("META-INF/") and name.endswith((".RSA", ".DSA", ".EC"))
        for name in names
    )
    return has_sf and has_block


def _apk_signature_marker(path: Path) -> bool:
    with path.open("rb") as handle:
        data = handle.read()
    return APK_SIGNING_BLOCK_MAGIC in data


def inspect_artifact(
    path: Path,
    *,
    require_arm64_only: bool = True,
    require_signature_marker: bool = True,
) -> dict[str, Any]:
    suffix = path.suffix.lower()
    if suffix not in {".apk", ".aab"}:
        raise ArtifactError("artifact must be an .apk or .aab file")
    if not path.is_file():
        raise ArtifactError(f"artifact does not exist: {path}")
    kind = suffix[1:]

    try:
        with zipfile.ZipFile(path, "r") as archive:
            bad = archive.testzip()
            if bad is not None:
                raise ArtifactError(f"ZIP integrity check failed at {bad}")
            infos = archive.infolist()
            native = _native_members(kind, infos)
            if not native:
                raise ArtifactError("artifact contains no native shared libraries")
            abis = sorted({abi for abi, _, _ in native})
            if "arm64-v8a" not in abis:
                raise ArtifactError(f"arm64-v8a is missing; found ABIs: {abis}")
            if require_arm64_only and abis != ["arm64-v8a"]:
                raise ArtifactError(f"unexpected ABI set {abis}; P0/release baseline is arm64-v8a only")

            libraries: list[dict[str, Any]] = []
            with path.open("rb") as raw_handle:
                for abi, member, info in native:
                    data = archive.read(info)
                    alignments = _load_alignments(data, member) if abi == "arm64-v8a" else []
                    page_compatible = all(
                        alignment >= PAGE_SIZE and alignment % PAGE_SIZE == 0
                        for alignment in alignments
                    ) if alignments else None
                    if abi == "arm64-v8a" and not page_compatible:
                        values = ", ".join(f"0x{value:x}" for value in alignments)
                        raise ArtifactError(
                            f"{member}: PT_LOAD alignment is not 16 KB compatible: {values}"
                        )

                    zip_alignment = None
                    if kind == "apk" and info.compress_type == zipfile.ZIP_STORED:
                        data_offset = _zip_data_offset(raw_handle, info)
                        zip_alignment = data_offset % PAGE_SIZE == 0
                        if not zip_alignment:
                            raise ArtifactError(
                                f"{member}: uncompressed native library data offset {data_offset} is not 16 KB ZIP-aligned"
                            )

                    libraries.append(
                        {
                            "abi": abi,
                            "member": member,
                            "size": info.file_size,
                            "compression": "stored" if info.compress_type == zipfile.ZIP_STORED else "compressed",
                            "pt_load_alignments": alignments,
                            "elf_16kb_compatible": page_compatible,
                            "zip_16kb_aligned": zip_alignment,
                        }
                    )

            signature_marker = _apk_signature_marker(path) if kind == "apk" else _aab_signature_marker(infos)
            if require_signature_marker and not signature_marker:
                label = "APK Signing Block" if kind == "apk" else "JAR signature entries"
                raise ArtifactError(f"{label} not found")
    except zipfile.BadZipFile as exc:
        raise ArtifactError(f"invalid ZIP-based Android artifact: {exc}") from exc

    return {
        "artifact": path.name,
        "kind": kind,
        "sha256": _sha256(path),
        "size_bytes": path.stat().st_size,
        "abis": abis,
        "native_library_count": len(libraries),
        "arm64_elf_16kb_compatible": all(
            library["elf_16kb_compatible"] is True
            for library in libraries
            if library["abi"] == "arm64-v8a"
        ),
        "apk_uncompressed_libs_16kb_zip_aligned": (
            all(
                library["zip_16kb_aligned"] is not False
                for library in libraries
                if library["abi"] == "arm64-v8a"
            )
            if kind == "apk"
            else None
        ),
        "signature_marker_present": signature_marker,
        "signature_marker_is_cryptographic_verification": False,
        "libraries": libraries,
    }


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Verify Project 77 APK/AAB ABI and native 16 KB page-size readiness."
    )
    parser.add_argument("artifact", type=Path)
    parser.add_argument("--allow-extra-abis", action="store_true")
    parser.add_argument(
        "--no-signature-marker-check",
        action="store_true",
        help="Skip structural signature-marker requirement; does not affect ELF/ZIP checks.",
    )
    parser.add_argument("--json", type=Path, dest="json_output")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    try:
        result = inspect_artifact(
            args.artifact,
            require_arm64_only=not args.allow_extra_abis,
            require_signature_marker=not args.no_signature_marker_check,
        )
    except ArtifactError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1

    print(
        f"Android artifact verified: {result['artifact']} · {result['kind'].upper()} · "
        f"ABIs={','.join(result['abis'])} · native={result['native_library_count']} · "
        f"ELF16K={'yes' if result['arm64_elf_16kb_compatible'] else 'no'}"
    )
    if result["kind"] == "apk":
        print(
            "APK uncompressed native ZIP alignment: "
            + ("16 KB compatible" if result["apk_uncompressed_libs_16kb_zip_aligned"] else "not compatible")
        )
    print(
        "Signature structure marker: present. "
        "Certificate identity/cryptographic validity must still be verified with apksigner/jarsigner."
    )
    print(f"SHA-256: {result['sha256']}")

    if args.json_output:
        args.json_output.parent.mkdir(parents=True, exist_ok=True)
        args.json_output.write_text(
            json.dumps(result, indent=2, sort_keys=True) + "\n",
            encoding="utf-8",
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
