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

from validate_flow_network import validate_flow_network_levels
from validate_prototype_content import validate_all

ROOT = Path(__file__).resolve().parents[1]
MANIFEST_SCHEMA_VERSION = 1
EVENT_SCHEMA_VERSION = 1
METADATA_SCHEMA_VERSION = 1

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
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(65536), b""):
            digest.update(chunk)
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


def build_manifest(
    batch_id: str,
    commit_sha: str,
    build_version: str,
    created_utc: str,
    root: Path = ROOT,
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
    return {
        "freeze_manifest_schema_version": MANIFEST_SCHEMA_VERSION,
        "batch_id": batch_id,
        "created_utc": created_utc,
        "build_version": build_version,
        "commit_sha": commit_sha,
        **snapshot,
    }


def verify_manifest(manifest: dict[str, Any], current_commit: str, root: Path = ROOT) -> None:
    if manifest.get("freeze_manifest_schema_version") != MANIFEST_SCHEMA_VERSION:
        raise FreezeError("unsupported freeze manifest schema")
    expected_commit = manifest.get("commit_sha")
    if expected_commit != current_commit:
        raise FreezeError(f"freeze commit {expected_commit!r} does not match checkout {current_commit!r}")
    expected_build = manifest.get("build_version")
    if not isinstance(expected_build, str) or not expected_build.endswith("+" + current_commit[:12]):
        raise FreezeError("freeze build_version is not bound to the checkout commit")

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

    verify = sub.add_parser("verify", help="Verify a freeze manifest against the current checkout")
    verify.add_argument("manifest", type=Path)
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    try:
        require_clean_checkout(ROOT)
        validate_current_content()
        commit = current_commit(ROOT)
        if args.command == "generate":
            build_version = args.build_version or (_bundle_version(ROOT) + "+" + commit[:12])
            manifest = build_manifest(args.batch_id, commit, build_version, _created_now(), ROOT)
            args.output.parent.mkdir(parents=True, exist_ok=True)
            args.output.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
            print(f"P0 freeze manifest written: {args.output}")
            print(f"commit={commit} build={build_version} fingerprint={manifest['freeze_fingerprint']}")
            return 0

        manifest = _read_json(args.manifest)
        verify_manifest(manifest, commit, ROOT)
        print(f"P0 freeze verified: {args.manifest}")
        return 0
    except FreezeError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
