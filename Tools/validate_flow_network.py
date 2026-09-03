from __future__ import annotations

import itertools
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FLOW_LEVEL_DIR = ROOT / "Assets/Project77/Content/Resources/Prototype/FlowNetwork"

NORTH = 1
EAST = 2
SOUTH = 4
WEST = 8
DIRECTIONS = (
    (NORTH, (0, 1), SOUTH),
    (EAST, (1, 0), WEST),
    (SOUTH, (0, -1), NORTH),
    (WEST, (-1, 0), EAST),
)
ROLES = {"source", "target", "hazard", "relay"}


def _fail(message: str) -> None:
    raise ValueError(message)


def _rotate(mask: int, turns: int) -> int:
    for _ in range(turns % 4):
        rotated = 0
        if mask & NORTH:
            rotated |= EAST
        if mask & EAST:
            rotated |= SOUTH
        if mask & SOUTH:
            rotated |= WEST
        if mask & WEST:
            rotated |= NORTH
        mask = rotated
    return mask


def _is_solved(tiles: list[dict], rotations: dict[str, int]) -> bool:
    by_position = {(tile["x"], tile["y"]): tile for tile in tiles}
    masks = {
        tile["id"]: _rotate(tile["mask"], rotations.get(tile["id"], tile["initialRotation"]))
        for tile in tiles
    }
    powered = {tile["id"] for tile in tiles if tile["role"] == "source"}
    queue = list(powered)
    by_id = {tile["id"]: tile for tile in tiles}

    while queue:
        tile_id = queue.pop(0)
        tile = by_id[tile_id]
        mask = masks[tile_id]
        for direction, (dx, dy), opposite in DIRECTIONS:
            if not mask & direction:
                continue
            neighbor = by_position.get((tile["x"] + dx, tile["y"] + dy))
            if neighbor is None or not masks[neighbor["id"]] & opposite:
                continue
            if neighbor["id"] not in powered:
                powered.add(neighbor["id"])
                queue.append(neighbor["id"])

    targets = [tile["id"] for tile in tiles if tile["role"] == "target"]
    hazards = [tile["id"] for tile in tiles if tile["role"] == "hazard"]
    return all(tile_id in powered for tile_id in targets) and all(tile_id not in powered for tile_id in hazards)


def validate_flow_network_levels() -> int:
    files = sorted(FLOW_LEVEL_DIR.glob("C-*.json"))
    if len(files) < 10:
        _fail(f"Flow / Network Restoration requires at least 10 validated levels, found {len(files)}")

    active_ids: set[str] = set()
    for path in files:
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as exc:
            _fail(f"{path.relative_to(ROOT)}: invalid JSON: {exc}")

        if data.get("schemaVersion") != 1:
            _fail(f"{path.name}: schemaVersion must be 1")
        level_id = data.get("id")
        if not isinstance(level_id, str) or not level_id:
            _fail(f"{path.name}: non-empty id is required")
        if level_id in active_ids:
            _fail(f"duplicate Flow / Network level id: {level_id}")
        active_ids.add(level_id)
        if not isinstance(data.get("revision"), int) or data["revision"] < 1:
            _fail(f"{level_id}: revision must be >= 1")
        if data.get("variant") != "flow_network_restoration":
            _fail(f"{level_id}: variant must be flow_network_restoration")
        if not isinstance(data.get("difficultyTag"), str) or not data["difficultyTag"]:
            _fail(f"{level_id}: difficultyTag is required")

        payload = data.get("payload")
        if not isinstance(payload, dict):
            _fail(f"{level_id}: payload object is required")
        width = payload.get("width")
        height = payload.get("height")
        if not isinstance(width, int) or not isinstance(height, int) or not (2 <= width <= 12 and 2 <= height <= 12):
            _fail(f"{level_id}: board dimensions must be integers between 2 and 12")

        tiles = payload.get("tiles")
        if not isinstance(tiles, list) or not tiles:
            _fail(f"{level_id}: at least one tile is required")
        ids: set[str] = set()
        positions: set[tuple[int, int]] = set()
        source_count = 0
        target_count = 0
        rotatable_ids: list[str] = []
        normalized: list[dict] = []
        for index, raw_tile in enumerate(tiles):
            if not isinstance(raw_tile, dict):
                _fail(f"{level_id}: tile {index} must be an object")
            tile_id = raw_tile.get("id")
            if not isinstance(tile_id, str) or not tile_id or tile_id in ids:
                _fail(f"{level_id}: invalid/duplicate tile id at index {index}")
            ids.add(tile_id)
            x = raw_tile.get("x")
            y = raw_tile.get("y")
            if not isinstance(x, int) or not isinstance(y, int) or not (0 <= x < width and 0 <= y < height):
                _fail(f"{level_id}/{tile_id}: invalid tile position")
            position = (x, y)
            if position in positions:
                _fail(f"{level_id}: multiple tiles at {position}")
            positions.add(position)
            role = raw_tile.get("role")
            if role not in ROLES:
                _fail(f"{level_id}/{tile_id}: unknown role {role!r}")
            source_count += role == "source"
            target_count += role == "target"
            mask = raw_tile.get("mask")
            if not isinstance(mask, int) or not (1 <= mask <= 15):
                _fail(f"{level_id}/{tile_id}: mask must be integer 1..15")
            rotatable = raw_tile.get("rotatable")
            if not isinstance(rotatable, bool):
                _fail(f"{level_id}/{tile_id}: rotatable must be boolean")
            initial = raw_tile.get("initialRotation")
            if not isinstance(initial, int) or not (0 <= initial <= 3):
                _fail(f"{level_id}/{tile_id}: initialRotation must be 0..3")
            if rotatable:
                rotatable_ids.append(tile_id)
            normalized.append({
                "id": tile_id,
                "x": x,
                "y": y,
                "role": role,
                "mask": mask,
                "rotatable": rotatable,
                "initialRotation": initial,
            })

        if source_count == 0 or target_count == 0:
            _fail(f"{level_id}: source and target are both required")
        if not rotatable_ids:
            _fail(f"{level_id}: at least one rotatable tile is required")

        initial_rotations = {tile["id"]: tile["initialRotation"] for tile in normalized if tile["rotatable"]}
        if _is_solved(normalized, initial_rotations):
            _fail(f"{level_id}: initial state is already solved")

        solution_found = False
        for values in itertools.product(range(4), repeat=len(rotatable_ids)):
            rotations = dict(zip(rotatable_ids, values))
            if _is_solved(normalized, rotations):
                solution_found = True
                break
        if not solution_found:
            _fail(f"{level_id}: no valid rotation solution exists")

    return len(files)
