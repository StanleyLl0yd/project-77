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
from validate_flow_network import validate_flow_network_levels
from validate_prototype_content import validate_all

ROOT = Path(__file__).resolve().parents[1]
MANIFEST_SCHEMA_VERSION = 3
EVENT_SCHEMA_VERSION = 1
METADATA_SCHEMA_VERSION = 1

GATE_PLAN = {
    "fresh_exposures_per_variant": 10,
    "tutorial_completion_min": 0.85,
    "voluntary_continuation_min": 0.70,
    "voluntary_window_ms": 15_000,
    "level_duration_target_ms": {
        "min": 30_000,
        "max": 90_000,
    },
}

VARIANTS = {
    "energy_routing": ("Assets/Project77/Content/Resources/Prototype/EnergyRouting", "A"),
    "path_expedition_routing": ("Assets/Project77/Content/Resources/Prototype/PathExpedition", "B"),
    "flow_network_restoration": ("Assets/Project77/Content/Resources/Prototype/FlowNetwork", "C"),
}

CONTRACTS = {
    "playtest_protocol": "Docs/29_PROTOTYPE_PLAYTEST_PROTOCOL.md",
    "analytics_contract": "Docs/30_PROTOTYPE_ANALYTICS_CONTRACT.md",
    "product_gates": "Docs/18_PRODUCT_GATES.md",
    "prototype_spec": "Docs/28_PROTOTYPE_01_SPEC.md",
}


class FreezeError(ValueError):
    pass


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
    path = root / "ProjectSettings/ProjectVersion.txt"
    for line in path.read_text(encoding="utf-8").splitlines():
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


def _variant_records(root: Path) -> dict[str, list[dict[str, Any]]]:
    result: dict[str, list[dict[str, Any]]] = {}
    for variant, (relative_dir, prefix) in VARIANTS.items():
        directory = root / relative_dir
        files = sorted(directory.glob(f"{prefix}-*.json"))
        if len(files) != 10:
            raise FreezeError(f"{variant}: expected exactly 10 P0 levels, found {len(files)}")

        records: list[dict[str, Any]] = []
        expected_ids = {f"{prefix}-{index:03d}" for index in range(1, 11)}
        actual_ids: set[str] = set()
        for path in files:
            data = _read_json(path)
            level_id = data.get("id")
            revision = data.get("revision")
            difficulty = data.get("difficultyTag")
            if data.get("schemaVersion") != 1:
                raise FreezeError(f"{path.name}: schemaVersion must be 1")
            if data.get("variant") != variant:
                raise FreezeError(f"{path.name}: variant mismatch")
            if not isinstance(level_id, str) or not level_id:
                raise FreezeError(f"{path.name}: id is required")
            if not isinstance(revision, int) or isinstance(revision, bool) or revision < 1:
                raise FreezeError(f"{path.name}: revision must be >= 1")
            if not isinstance(difficulty, str) or not difficulty:
                raise FreezeError(f"{path.name}: difficultyTag is required")
            if level_id in actual_ids:
                raise FreezeError(f"{variant}: duplicate level id {level_id}")
            actual_ids.add(level_id)
            records.append(
                {
                    "id": level_id,
                    "revision": revision,
                    "difficulty_tag": difficulty,
                    "path": path.relative_to(root).as_posix(),
                    "sha256": _sha256(path),
                }
            )

        if actual_ids != expected_ids:
            missing = sorted(expected_ids - actual_ids)
            extra = sorted(actual_ids - expected_ids)
            raise FreezeError(f"{variant}: unexpected level ID set; missing={missing}, extra={extra}")
        records.sort(key=lambda item: item["id"])
        result[variant] = records
    return result


def build_repository_snapshot(root: Path = ROOT) -> dict[str, Any]:
    levels = _variant_records(root)
    contracts = {
        name: {
            "path": relative,
            "sha256": _sha256(root / relative),
        }
        for name, relative in CONTRACTS.items()
    }
    fingerprint_input = {
        "event_schema_version": EVENT_SCHEMA_VERSION,
        "metadata_schema_version": METADATA_SCHEMA_VERSION,
        "levels": levels,
        "contracts": contracts,
    }
    return {
        "event_schema_version": EVENT_SCHEMA_VERSION,
        "metadata_schema_version": METADATA_SCHEMA_VERSION,
        "unity_version": _unity_version(root),
        "urp_version": _urp_version(root),
        "levels": levels,
        "contracts": contracts,
        "freeze_fingerprint": _canonical_hash(fingerprint_input),
    }


def _order_plan_record(path: Path | None) -> dict[str, str] | None:
    if path is None:
        return None
    return {
        "filename": path.name,
        "sha256": _sha256(path),
    }


def _artifact_record(path: Path | None) -> dict[str, Any] | None:
    if path is None:
        return None
    try:
        result = android_artifact.inspect_artifact(path)
    except android_artifact.ArtifactError as exc:
        raise FreezeError(f"playtest artifact failed Android acceptance checks: {exc}") from exc
    if result["kind"] != "apk":
        raise FreezeError("P0 external playtest freeze requires an installable APK, not an AAB")
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
        "order_plan": manifest.get("order_plan"),
        "artifact": manifest.get("artifact"),
    }


def build_manifest(
    batch_id: str,
    commit_sha: str,
    build_version: str,
    created_utc: str,
    root: Path = ROOT,
    order_plan_path: Path | None = None,
    artifact_path: Path | None = None,
) -> dict[str, Any]:
    batch_id = batch_id.strip()
    commit_sha = commit_sha.strip().lower()
    build_version = build_version.strip()
    if not batch_id or len(batch_id) > 64 or "\n" in batch_id or "\r" in batch_id:
        raise FreezeError("batch_id must be 1-64 characters without line breaks")
    if re.fullmatch(r"[0-9a-f]{40}", commit_sha) is None:
        raise FreezeError("commit_sha must be a full 40-character Git SHA-1")
    if not build_version:
        raise FreezeError("build_version is required")
    if not build_version.endswith("+" + commit_sha[:12]):
        raise FreezeError("build_version must end with +<first 12 commit characters>")

    snapshot = build_repository_snapshot(root)
    manifest = {
        "freeze_manifest_schema_version": MANIFEST_SCHEMA_VERSION,
        "batch_id": batch_id,
        "created_utc": created_utc,
        "build_version": build_version,
        "commit_sha": commit_sha,
        "gate_plan": GATE_PLAN,
        "order_plan": _order_plan_record(order_plan_path),
        "artifact": _artifact_record(artifact_path),
        **snapshot,
    }
    manifest["batch_fingerprint"] = _canonical_hash(_batch_identity(manifest))
    return manifest


def verify_order_plan_binding(manifest: dict[str, Any], order_plan_path: Path | None) -> None:
    expected = manifest.get("order_plan")
    if expected is None:
        if order_plan_path is not None:
            raise FreezeError("an order plan was supplied but the freeze manifest did not bind one")
        return
    if not isinstance(expected, dict) or not isinstance(expected.get("sha256"), str):
        raise FreezeError("freeze order_plan record is malformed")
    if order_plan_path is None:
        raise FreezeError("freeze manifest is bound to an order plan; --order-plan is required")
    actual_hash = _sha256(order_plan_path)
    if actual_hash != expected["sha256"]:
        raise FreezeError(
            f"order plan hash mismatch: {actual_hash} != frozen {expected['sha256']}"
        )


def verify_artifact_binding(manifest: dict[str, Any], artifact_path: Path | None) -> None:
    expected = manifest.get("artifact")
    if expected is None:
        if artifact_path is not None:
            raise FreezeError("an artifact was supplied but the freeze manifest did not bind one")
        return
    if not isinstance(expected, dict) or not isinstance(expected.get("sha256"), str):
        raise FreezeError("freeze artifact record is malformed")
    if artifact_path is None:
        raise FreezeError("freeze manifest is bound to a playtest APK; --artifact is required")
    actual = _artifact_record(artifact_path)
    if actual != expected:
        raise FreezeError("playtest APK does not match the frozen artifact acceptance record")


def verify_manifest(
    manifest: dict[str, Any],
    current_commit: str,
    root: Path = ROOT,
    order_plan_path: Path | None = None,
    artifact_path: Path | None = None,
) -> None:
    if manifest.get("freeze_manifest_schema_version") != MANIFEST_SCHEMA_VERSION:
        raise FreezeError("unsupported freeze manifest schema")
    expected_commit = manifest.get("commit_sha")
    if expected_commit != current_commit:
        raise FreezeError(f"freeze commit {expected_commit!r} does not match checkout {current_commit!r}")
    expected_build = manifest.get("build_version")
    if not isinstance(expected_build, str) or not expected_build.endswith("+" + current_commit[:12]):
        raise FreezeError("freeze build_version is not bound to the checkout commit")
    if manifest.get("gate_plan") != GATE_PLAN:
        raise FreezeError("freeze gate_plan does not match the preregistered P0 gate plan")

    snapshot = build_repository_snapshot(root)
    for key in (
        "event_schema_version",
        "metadata_schema_version",
        "unity_version",
        "urp_version",
        "levels",
        "contracts",
        "freeze_fingerprint",
    ):
        if manifest.get(key) != snapshot[key]:
            raise FreezeError(f"freeze mismatch: {key} changed after the batch was frozen")

    verify_order_plan_binding(manifest, order_plan_path)
    verify_artifact_binding(manifest, artifact_path)
    expected_batch_fingerprint = _canonical_hash(_batch_identity(manifest))
    if manifest.get("batch_fingerprint") != expected_batch_fingerprint:
        raise FreezeError("batch_fingerprint does not match the frozen batch identity")


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
    dirty = _git(root, "status", "--porcelain", "--untracked-files=no")
    if dirty:
        raise FreezeError("tracked working tree changes are present; freeze from a clean checkout")


def validate_current_content() -> None:
    try:
        counts = validate_all()
        counts["flow_network_restoration"] = validate_flow_network_levels()
    except ValueError as exc:
        raise FreezeError(str(exc)) from exc
    bad = {name: count for name, count in counts.items() if count != 10}
    if bad:
        raise FreezeError(f"P0 freeze requires exactly 10 levels per variant, got {bad}")


def _created_now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds").replace("+00:00", "Z")


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Create or verify a Project 77 P0 batch freeze manifest.")
    sub = parser.add_subparsers(dest="command", required=True)

    generate = sub.add_parser("generate", help="Create a freeze manifest from the current clean checkout")
    generate.add_argument("--batch-id", required=True)
    generate.add_argument("--output", type=Path, required=True)
    generate.add_argument("--build-version")
    generate.add_argument(
        "--order-plan",
        type=Path,
        help="Counterbalanced CSV to bind into this experiment freeze",
    )
    generate.add_argument(
        "--artifact",
        type=Path,
        help="Exact signed APK distributed to P0 testers; also runs ABI/16 KB/signature-structure checks",
    )
    generate.add_argument(
        "--allow-unbound-artifact",
        action="store_true",
        help="CI/tooling smoke only. External P0 batches must bind the exact APK.",
    )

    verify = sub.add_parser("verify", help="Verify a freeze manifest against the current checkout")
    verify.add_argument("manifest", type=Path)
    verify.add_argument(
        "--order-plan",
        type=Path,
        help="Required when the freeze manifest is bound to a counterbalanced order plan",
    )
    verify.add_argument(
        "--artifact",
        type=Path,
        help="Required when the freeze manifest is bound to a playtest APK",
    )
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    try:
        require_clean_checkout(ROOT)
        validate_current_content()
        commit = current_commit(ROOT)
        if args.command == "generate":
            if args.artifact is None and not args.allow_unbound_artifact:
                raise FreezeError(
                    "external P0 freeze requires --artifact with the exact signed APK; "
                    "use --allow-unbound-artifact only for CI/tooling smoke"
                )
            build_version = args.build_version or (_bundle_version(ROOT) + "+" + commit[:12])
            manifest = build_manifest(
                args.batch_id,
                commit,
                build_version,
                _created_now(),
                ROOT,
                args.order_plan,
                args.artifact,
            )
            args.output.parent.mkdir(parents=True, exist_ok=True)
            args.output.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
            print(f"P0 freeze manifest written: {args.output}")
            print(
                f"commit={commit} build={build_version} "
                f"fingerprint={manifest['batch_fingerprint']}"
            )
            return 0

        manifest = _read_json(args.manifest)
        verify_manifest(manifest, commit, ROOT, args.order_plan, args.artifact)
        print(f"P0 freeze verified: {args.manifest}")
        return 0
    except FreezeError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
