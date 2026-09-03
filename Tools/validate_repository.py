#!/usr/bin/env python3
from __future__ import annotations

import json
import sys
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parents[1]

REQUIRED_FILES = [
    "AGENTS.md",
    "README.md",
    "Docs/13_DECISION_LOG.md",
    "Docs/28_PROTOTYPE_01_SPEC.md",
    "Docs/29_PROTOTYPE_PLAYTEST_PROTOCOL.md",
    "Docs/30_PROTOTYPE_ANALYTICS_CONTRACT.md",
    "Docs/31_ENGINEERING_CONVENTIONS.md",
    "Docs/32_PROTOTYPE_IMPLEMENTATION_BACKLOG.md",
    "ProjectSettings/ProjectVersion.txt",
    "Packages/manifest.json",
    "Assets/Project77/Core/PrototypeEntryPoint.cs",
    "Assets/Project77/Editor/Project77ProjectBootstrap.cs",
    "Assets/Project77/Puzzle/Project77.Puzzle.asmdef",
    "Assets/Project77/Puzzle/PrototypeLevelDefinition.cs",
    "Assets/Project77/Puzzle/PrototypeLevelValidator.cs",
    "Assets/Project77/Puzzle/IPuzzleRunner.cs",
    "Assets/Project77/Puzzle/EnergyRouting/EnergyRoutingLevel.cs",
    "Assets/Project77/Puzzle/EnergyRouting/EnergyRoutingRunner.cs",
    "Assets/Project77/Analytics/PrototypeAnalytics.cs",
    "Assets/Project77/Tests/EditMode/Project77.Tests.EditMode.asmdef",
]

EXPECTED_EDITOR = "6000.3.22f1"
EXPECTED_URP_MAJOR_MINOR = "17.3."
ENERGY_LEVEL_DIR = ROOT / "Assets/Project77/Content/Prototype/EnergyRouting"


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def require_cell(value: object, width: int, height: int, context: str) -> tuple[int, int]:
    if not isinstance(value, list) or len(value) != 2 or not all(isinstance(v, int) for v in value):
        fail(f"{context}: expected [x, y] integer cell")
    x, y = value
    if not (0 <= x < width and 0 <= y < height):
        fail(f"{context}: cell {(x, y)} is outside {width}x{height} board")
    return x, y


def neighbors(cell: tuple[int, int], width: int, height: int) -> Iterable[tuple[int, int]]:
    x, y = cell
    for candidate in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
        cx, cy = candidate
        if 0 <= cx < width and 0 <= cy < height:
            yield candidate


def is_energy_level_solvable(
    width: int,
    height: int,
    pairs: list[tuple[str, tuple[int, int], tuple[int, int]]],
    blocked: set[tuple[int, int]],
) -> bool:
    endpoints = {start for _, start, _ in pairs} | {end for _, _, end in pairs}

    def solve_pair(pair_index: int, occupied: set[tuple[int, int]]) -> bool:
        if pair_index == len(pairs):
            return True

        pair_id, start, end = pairs[pair_index]
        visited = {start}
        path = [start]

        def walk(current: tuple[int, int]) -> bool:
            if current == end:
                return solve_pair(pair_index + 1, occupied | set(path))

            ordered = sorted(
                neighbors(current, width, height),
                key=lambda cell: abs(cell[0] - end[0]) + abs(cell[1] - end[1]),
            )
            for candidate in ordered:
                if candidate in visited or candidate in blocked or candidate in occupied:
                    continue
                if candidate in endpoints and candidate != end:
                    continue
                visited.add(candidate)
                path.append(candidate)
                if walk(candidate):
                    return True
                path.pop()
                visited.remove(candidate)
            return False

        return walk(start)

    return solve_pair(0, set())


def validate_energy_routing_levels() -> int:
    files = sorted(ENERGY_LEVEL_DIR.glob("A-*.json"))
    if len(files) < 10:
        fail(f"Energy Routing requires at least 10 validated levels, found {len(files)}")

    active_ids: set[str] = set()
    for path in files:
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as exc:
            fail(f"{path.relative_to(ROOT)}: invalid JSON: {exc}")

        if data.get("schemaVersion") != 1:
            fail(f"{path.name}: schemaVersion must be 1")
        level_id = data.get("id")
        if not isinstance(level_id, str) or not level_id:
            fail(f"{path.name}: non-empty id is required")
        if level_id in active_ids:
            fail(f"duplicate Energy Routing level id: {level_id}")
        active_ids.add(level_id)
        if data.get("revision", 0) < 1:
            fail(f"{level_id}: revision must be >= 1")
        if data.get("variant") != "energy_routing":
            fail(f"{level_id}: variant must be energy_routing")
        if not isinstance(data.get("difficultyTag"), str) or not data["difficultyTag"]:
            fail(f"{level_id}: difficultyTag is required")

        payload = data.get("payload")
        if not isinstance(payload, dict):
            fail(f"{level_id}: payload object is required")
        width = payload.get("width")
        height = payload.get("height")
        if not isinstance(width, int) or not isinstance(height, int) or not (2 <= width <= 12 and 2 <= height <= 12):
            fail(f"{level_id}: board dimensions must be integers between 2 and 12")

        raw_pairs = payload.get("pairs")
        if not isinstance(raw_pairs, list) or not raw_pairs:
            fail(f"{level_id}: at least one pair is required")
        pair_ids: set[str] = set()
        endpoints: set[tuple[int, int]] = set()
        pairs: list[tuple[str, tuple[int, int], tuple[int, int]]] = []
        for index, pair in enumerate(raw_pairs):
            if not isinstance(pair, dict):
                fail(f"{level_id}: pair {index} must be an object")
            pair_id = pair.get("id")
            if not isinstance(pair_id, str) or not pair_id:
                fail(f"{level_id}: pair {index} needs an id")
            if pair_id in pair_ids:
                fail(f"{level_id}: duplicate pair id {pair_id}")
            pair_ids.add(pair_id)
            start = require_cell(pair.get("start"), width, height, f"{level_id}/{pair_id}/start")
            end = require_cell(pair.get("end"), width, height, f"{level_id}/{pair_id}/end")
            if start == end:
                fail(f"{level_id}/{pair_id}: endpoints must differ")
            if start in endpoints or end in endpoints:
                fail(f"{level_id}/{pair_id}: endpoint cell reused")
            endpoints.update((start, end))
            pairs.append((pair_id, start, end))

        raw_blocked = payload.get("blocked", [])
        if not isinstance(raw_blocked, list):
            fail(f"{level_id}: blocked must be an array")
        blocked: set[tuple[int, int]] = set()
        for index, raw_cell in enumerate(raw_blocked):
            cell = require_cell(raw_cell, width, height, f"{level_id}/blocked/{index}")
            if cell in blocked:
                fail(f"{level_id}: duplicate blocked cell {cell}")
            if cell in endpoints:
                fail(f"{level_id}: blocked cell overlaps endpoint {cell}")
            blocked.add(cell)

        if not is_energy_level_solvable(width, height, pairs, blocked):
            fail(f"{level_id}: no non-crossing solution exists")

    return len(files)


def main() -> None:
    missing = [path for path in REQUIRED_FILES if not (ROOT / path).is_file()]
    if missing:
        fail("missing required files: " + ", ".join(missing))

    version_lines = (ROOT / "ProjectSettings/ProjectVersion.txt").read_text(encoding="utf-8").splitlines()
    editor_line = next((line for line in version_lines if line.startswith("m_EditorVersion:")), None)
    if editor_line is None:
        fail("ProjectVersion.txt has no m_EditorVersion")
    editor_version = editor_line.split(":", 1)[1].strip()
    if editor_version != EXPECTED_EDITOR:
        fail(f"expected Unity {EXPECTED_EDITOR}, found {editor_version}")

    manifest = json.loads((ROOT / "Packages/manifest.json").read_text(encoding="utf-8"))
    dependencies = manifest.get("dependencies", {})
    urp = dependencies.get("com.unity.render-pipelines.universal")
    if not isinstance(urp, str) or not urp.startswith(EXPECTED_URP_MAJOR_MINOR):
        fail(f"expected URP {EXPECTED_URP_MAJOR_MINOR}x, found {urp!r}")

    project_manifest = json.loads((ROOT / "Docs/project77_manifest.json").read_text(encoding="utf-8"))
    if project_manifest.get("status") != "prototype_phase":
        fail("project77_manifest.json must remain in prototype_phase")
    if project_manifest.get("active_milestone") != "Prototype 0.1":
        fail("active milestone must remain Prototype 0.1")

    gitignore = (ROOT / ".gitignore").read_text(encoding="utf-8")
    required_ignore_fragments = ["/[Ll]ibrary/", "/[Tt]emp/", "/[Oo]bj/", "/[Ll]ogs/", "/[Uu]ser[Ss]ettings/", "*.keystore", "*.jks"]
    absent = [fragment for fragment in required_ignore_fragments if fragment not in gitignore]
    if absent:
        fail(".gitignore is missing required Unity/security entries: " + ", ".join(absent))

    energy_level_count = validate_energy_routing_levels()
    print(
        f"Repository validation passed: Unity {editor_version}, URP {urp}, "
        f"Prototype 0.1 governance present, Energy Routing levels validated/solvable: {energy_level_count}."
    )


if __name__ == "__main__":
    main()
