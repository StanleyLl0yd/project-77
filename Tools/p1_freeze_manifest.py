#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import verify_android_artifact as android_artifact

ROOT = Path(__file__).resolve().parents[1]
MANIFEST_SCHEMA_VERSION = 2
EVENT_SCHEMA_VERSION = 1
METADATA_SCHEMA_VERSION = 1
SELECTED_CORE = "energy_routing"
PROTOTYPE_VARIANT = "selected_meta"

GATE_PLAN = {
    "fresh_sessions_target": 10,
    "post_island_voluntary_continuation_min": 0.50,
    "voluntary_window_ms": 15_000,
}

CONTRACTS = {
    "playtest_protocol": "Docs/29_PROTOTYPE_PLAYTEST_PROTOCOL.md",
    "analytics_contract": "Docs/30_PROTOTYPE_ANALYTICS_CONTRACT.md",
    "product_gates": "Docs/18_PRODUCT_GATES.md",
    "prototype_spec": "Docs/28_PROTOTYPE_01_SPEC.md",
    "p0_decision": "Docs/33_P0_GATE_DECISION.md",
    "p1_external_playtest_plan": "Docs/34_P1_EXTERNAL_PLAYTEST_PLAN.md",
}

IMPLEMENTATION = {
    "meta_domain": "Assets/Project77/Meta/PrototypeMetaProgression.cs",
    "selected_core_controller": "Assets/Project77/Game/EnergyRoutingPrototypeController.cs",
    "playtest_runtime": "Assets/Project77/Game/PrototypePlaytestRuntime.cs",
}


class FreezeError(ValueError):
    pass


def build_test_plan(
    orientation: str,
    device_targets: list[str],
    help_threshold_seconds: int = 30,
) -> dict[str, Any]:
    normalized_orientation = (orientation or "").strip().lower()
    if normalized_orientation not in {"portrait", "landscape"}:
        raise FreezeError("orientation must be portrait or landscape")

    normalized_devices = [value.strip() for value in (device_targets or []) if value and value.strip()]
    if not normalized_devices:
        raise FreezeError("at least one device target must be preregistered")
    if len(set(normalized_devices)) != len(normalized_devices):
        raise FreezeError("device targets must be unique")
    if help_threshold_seconds < 1:
        raise FreezeError("help threshold must be at least 1 second")

    return {
        "orientation": normalized_orientation,
        "device_targets": normalized_devices,
        "help_threshold_seconds": int(help_threshold_seconds),
    }


def _validate_test_plan(value: Any) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise FreezeError("test_plan is required")
    orientation = value.get("orientation")
    devices = value.get("device_targets")
    threshold = value.get("help_threshold_seconds")
    if not isinstance(devices, list) or not isinstance(threshold, int) or isinstance(threshold, bool):
        raise FreezeError("test_plan is malformed")
    return build_test_plan(str(orientation or ""), [str(item) for item in devices], threshold)


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    try:
        with path.open("rb") as handle:
            for chunk in iter(lambda: handle.read(65536), b""):
                digest.update(chunk)
    except OSError as exc:
        raise FreezeError(f"cannot hash {path}: {exc}") from exc
    return digest.hexdigest()


def _canonical_hash(value: Any) -> str:
    payload = json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=True).encode("utf-8")
    return hashlib.sha256(payload).hexdigest()


def _read_json(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise FreezeError(f"cannot parse {path}: {exc}") from exc
    if not isinstance(value, dict):
        raise FreezeError(f"{path}: expected JSON object")
    return value


def _unity_version(root: Path) -> str:
    for line in (root / "ProjectSettings/ProjectVersion.txt").read_text(encoding="utf-8").splitlines():
        if line.startswith("m_EditorVersion:"):
            return line.split(":", 1)[1].strip()
    raise FreezeError("ProjectVersion.txt has no m_EditorVersion")


def _urp_version(root: Path) -> str:
    manifest = _read_json(root / "Packages/manifest.json")
    value = manifest.get("dependencies", {}).get("com.unity.render-pipelines.universal")
    if not isinstance(value, str) or not value:
        raise FreezeError("URP version is missing from Packages/manifest.json")
    return value


def _bundle_version(root: Path) -> str:
    text = (root / "Assets/Project77/Editor/Project77ProjectBootstrap.cs").read_text(encoding="utf-8")
    match = re.search(r'PlayerSettings\.bundleVersion\s*=\s*"([^"]+)"', text)
    if match is None:
        raise FreezeError("cannot resolve PlayerSettings.bundleVersion from bootstrap")
    return match.group(1)


def _selected_levels(root: Path) -> list[dict[str, Any]]:
    directory = root / "Assets/Project77/Content/Resources/Prototype/EnergyRouting"
    files = sorted(directory.glob("A-*.json"))
    if len(files) != 10:
        raise FreezeError(f"selected P1 core requires exactly 10 current levels, found {len(files)}")

    expected_ids = {f"A-{index:03d}" for index in range(1, 11)}
    actual_ids: set[str] = set()
    records: list[dict[str, Any]] = []
    for path in files:
        data = _read_json(path)
        level_id = data.get("id")
        revision = data.get("revision")
        difficulty = data.get("difficultyTag")
        if data.get("schemaVersion") != 1:
            raise FreezeError(f"{path.name}: schemaVersion must be 1")
        if data.get("variant") != SELECTED_CORE:
            raise FreezeError(f"{path.name}: expected variant {SELECTED_CORE}")
        if not isinstance(level_id, str) or not level_id:
            raise FreezeError(f"{path.name}: id is required")
        if not isinstance(revision, int) or isinstance(revision, bool) or revision < 1:
            raise FreezeError(f"{path.name}: revision must be >= 1")
        if not isinstance(difficulty, str) or not difficulty:
            raise FreezeError(f"{path.name}: difficultyTag is required")
        if level_id in actual_ids:
            raise FreezeError(f"duplicate selected-core level id {level_id}")
        actual_ids.add(level_id)
        records.append({
            "id": level_id,
            "revision": revision,
            "difficulty_tag": difficulty,
            "path": path.relative_to(root).as_posix(),
            "sha256": _sha256(path),
        })

    if actual_ids != expected_ids:
        raise FreezeError(
            f"unexpected selected-core IDs; missing={sorted(expected_ids - actual_ids)}, "
            f"extra={sorted(actual_ids - expected_ids)}"
        )
    records.sort(key=lambda item: item["id"])
    return records


def _file_records(root: Path, files: dict[str, str]) -> dict[str, dict[str, str]]:
    return {
        name: {"path": relative, "sha256": _sha256(root / relative)}
        for name, relative in files.items()
    }


def build_repository_snapshot(root: Path = ROOT) -> dict[str, Any]:
    levels = _selected_levels(root)
    contracts = _file_records(root, CONTRACTS)
    implementation = _file_records(root, IMPLEMENTATION)
    fingerprint_input = {
        "event_schema_version": EVENT_SCHEMA_VERSION,
        "metadata_schema_version": METADATA_SCHEMA_VERSION,
        "prototype_variant": PROTOTYPE_VARIANT,
        "selected_core": SELECTED_CORE,
        "levels": levels,
        "contracts": contracts,
        "implementation": implementation,
    }
    return {
        "event_schema_version": EVENT_SCHEMA_VERSION,
        "metadata_schema_version": METADATA_SCHEMA_VERSION,
        "unity_version": _unity_version(root),
        "urp_version": _urp_version(root),
        "prototype_variant": PROTOTYPE_VARIANT,
        "selected_core": SELECTED_CORE,
        "levels": levels,
        "contracts": contracts,
        "implementation": implementation,
        "freeze_fingerprint": _canonical_hash(fingerprint_input),
    }


def _artifact_record(path: Path | None) -> dict[str, Any] | None:
    if path is None:
        return None
    try:
        result = android_artifact.inspect_artifact(path)
    except android_artifact.ArtifactError as exc:
        raise FreezeError(f"playtest artifact failed Android acceptance checks: {exc}") from exc
    if result["kind"] != "apk":
        raise FreezeError("P1 external playtest freeze requires an installable APK")
    return {
        "filename": path.name,
        "sha256": result["sha256"],
        "size_bytes": result["size_bytes"],
        "abis": result["abis"],
        "native_library_count": result["native_library_count"],
        "arm64_elf_16kb_compatible": result["arm64_elf_16kb_compatible"],
        "apk_uncompressed_libs_16kb_zip_aligned": result["apk_uncompressed_libs_16kb_zip_aligned"],
        "signature_marker_present": result["signature_marker_present"],
        "signature_marker_is_cryptographic_verification": result[
            "signature_marker_is_cryptographic_verification"
        ],
    }


def _batch_identity(manifest: dict[str, Any]) -> dict[str, Any]:
    return {
        "batch_id": manifest.get("batch_id"),
        "build_version": manifest.get("build_version"),
        "commit_sha": manifest.get("commit_sha"),
        "freeze_fingerprint": manifest.get("freeze_fingerprint"),
        "gate_plan": manifest.get("gate_plan"),
        "test_plan": manifest.get("test_plan"),
        "artifact": manifest.get("artifact"),
    }


def build_manifest(
    batch_id: str,
    commit_sha: str,
    build_version: str,
    created_utc: str,
    *,
    test_plan: dict[str, Any],
    root: Path = ROOT,
    artifact_path: Path | None = None,
) -> dict[str, Any]:
    batch_id = batch_id.strip()
    commit_sha = commit_sha.strip().lower()
    build_version = build_version.strip()
    if not batch_id or len(batch_id) > 64 or "\n" in batch_id or "\r" in batch_id:
        raise FreezeError("batch_id must be 1-64 characters without line breaks")
    if re.fullmatch(r"[0-9a-f]{40}", commit_sha) is None:
        raise FreezeError("commit_sha must be a full 40-character Git SHA-1")
    if not build_version.endswith("+" + commit_sha[:12]):
        raise FreezeError("build_version must end with +<first 12 commit characters>")

    normalized_test_plan = _validate_test_plan(test_plan)

    manifest = {
        "freeze_manifest_schema_version": MANIFEST_SCHEMA_VERSION,
        "batch_id": batch_id,
        "created_utc": created_utc,
        "build_version": build_version,
        "commit_sha": commit_sha,
        "gate_plan": GATE_PLAN,
        "test_plan": normalized_test_plan,
        "artifact": _artifact_record(artifact_path),
        **build_repository_snapshot(root),
    }
    manifest["batch_fingerprint"] = _canonical_hash(_batch_identity(manifest))
    return manifest


def verify_artifact_binding(manifest: dict[str, Any], artifact_path: Path | None) -> None:
    expected = manifest.get("artifact")
    if expected is None:
        if artifact_path is not None:
            raise FreezeError("an artifact was supplied but the manifest is unbound")
        return
    if artifact_path is None:
        raise FreezeError("freeze is bound to an APK; --artifact is required")
    if _artifact_record(artifact_path) != expected:
        raise FreezeError("playtest APK does not match the frozen artifact record")


def verify_manifest(
    manifest: dict[str, Any],
    current_commit_sha: str,
    *,
    root: Path = ROOT,
    artifact_path: Path | None = None,
) -> None:
    if manifest.get("freeze_manifest_schema_version") != MANIFEST_SCHEMA_VERSION:
        raise FreezeError("unsupported P1 freeze manifest schema")
    if manifest.get("commit_sha") != current_commit_sha:
        raise FreezeError("freeze commit does not match checkout")
    expected_build = manifest.get("build_version")
    if not isinstance(expected_build, str) or not expected_build.endswith("+" + current_commit_sha[:12]):
        raise FreezeError("freeze build_version is not bound to checkout commit")
    if manifest.get("gate_plan") != GATE_PLAN:
        raise FreezeError("freeze gate plan changed")
    normalized_test_plan = _validate_test_plan(manifest.get("test_plan"))
    if manifest.get("test_plan") != normalized_test_plan:
        raise FreezeError("freeze test_plan is not normalized")

    snapshot = build_repository_snapshot(root)
    for key in (
        "event_schema_version",
        "metadata_schema_version",
        "unity_version",
        "urp_version",
        "prototype_variant",
        "selected_core",
        "levels",
        "contracts",
        "implementation",
        "freeze_fingerprint",
    ):
        if manifest.get(key) != snapshot[key]:
            raise FreezeError(f"freeze mismatch: {key} changed after batch freeze")

    verify_artifact_binding(manifest, artifact_path)
    if manifest.get("batch_fingerprint") != _canonical_hash(_batch_identity(manifest)):
        raise FreezeError("batch_fingerprint does not match frozen identity")


def _git(root: Path, *args: str) -> str:
    try:
        completed = subprocess.run(
            ["git", *args],
            cwd=root,
            check=True,
            capture_output=True,
            text=True,
        )
    except (OSError, subprocess.CalledProcessError) as exc:
        raise FreezeError(f"git {' '.join(args)} failed: {exc}") from exc
    return completed.stdout.strip()


def current_commit(root: Path = ROOT) -> str:
    value = _git(root, "rev-parse", "HEAD").lower()
    if re.fullmatch(r"[0-9a-f]{40}", value) is None:
        raise FreezeError("git rev-parse HEAD did not return a full SHA")
    return value


def require_clean_checkout(root: Path = ROOT) -> None:
    if _git(root, "status", "--porcelain", "--untracked-files=no"):
        raise FreezeError("tracked working tree changes are present; freeze from a clean checkout")


def _created_now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds").replace("+00:00", "Z")


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Freeze or verify a Project 77 P1 external-playtest batch.")
    sub = parser.add_subparsers(dest="command", required=True)

    generate = sub.add_parser("generate")
    generate.add_argument("--batch-id", required=True)
    generate.add_argument("--artifact", type=Path)
    generate.add_argument("--orientation", choices=("portrait", "landscape"), required=True)
    generate.add_argument(
        "--device-target",
        action="append",
        dest="device_targets",
        required=True,
        help="Repeat for every preregistered device/profile in this batch.",
    )
    generate.add_argument("--help-threshold-seconds", type=int, default=30)
    generate.add_argument("--allow-unbound-artifact", action="store_true")
    generate.add_argument("--output", type=Path, required=True)

    verify = sub.add_parser("verify")
    verify.add_argument("manifest", type=Path)
    verify.add_argument("--artifact", type=Path)

    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    try:
        require_clean_checkout()
        commit = current_commit()
        if args.command == "generate":
            if args.artifact is None and not args.allow_unbound_artifact:
                raise FreezeError(
                    "external P1 freeze requires --artifact with the exact APK; "
                    "use --allow-unbound-artifact only for tooling smoke"
                )
            build_version = _bundle_version(ROOT) + "+" + commit[:12]
            test_plan = build_test_plan(
                args.orientation,
                args.device_targets,
                args.help_threshold_seconds,
            )
            manifest = build_manifest(
                args.batch_id,
                commit,
                build_version,
                _created_now(),
                test_plan=test_plan,
                artifact_path=args.artifact,
            )
            args.output.parent.mkdir(parents=True, exist_ok=True)
            args.output.write_text(
                json.dumps(manifest, indent=2, sort_keys=True) + "\n",
                encoding="utf-8",
            )
            print(
                f"P1 freeze written: batch={manifest['batch_id']} "
                f"commit={commit} fingerprint={manifest['batch_fingerprint']}"
            )
            return 0

        manifest = _read_json(args.manifest)
        verify_manifest(manifest, commit, artifact_path=args.artifact)
        print(f"P1 freeze verified: {manifest.get('batch_id')}")
        return 0
    except FreezeError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
