from __future__ import annotations

import json
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parents[1]
ENERGY_LEVEL_DIR = ROOT / "Assets/Project77/Content/Resources/Prototype/EnergyRouting"
PATH_LEVEL_DIR = ROOT / "Assets/Project77/Content/Resources/Prototype/PathExpedition"


def _fail(message: str) -> None:
    raise ValueError(message)


def _require_cell(value: object, width: int, height: int, context: str) -> tuple[int, int]:
    if not isinstance(value, dict) or set(value) != {"x", "y"}:
        _fail(f"{context}: expected {{x, y}} cell object")
    x = value.get("x")
    y = value.get("y")
    if not isinstance(x, int) or not isinstance(y, int):
        _fail(f"{context}: x and y must be integers")
    if not (0 <= x < width and 0 <= y < height):
        _fail(f"{context}: cell {(x, y)} is outside {width}x{height} board")
    return x, y


def _neighbors(cell: tuple[int, int], width: int, height: int) -> Iterable[tuple[int, int]]:
    x, y = cell
    for candidate in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
        cx, cy = candidate
        if 0 <= cx < width and 0 <= cy < height:
            yield candidate


def _read_level(path: Path) -> dict:
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        _fail(f"{path.relative_to(ROOT)}: invalid JSON: {exc}")
    if not isinstance(data, dict):
        _fail(f"{path.relative_to(ROOT)}: level root must be an object")
    return data


def _validate_common(data: dict, path: Path, expected_variant: str, active_ids: set[str]) -> tuple[str, dict, int, int]:
    if data.get("schemaVersion") != 1:
        _fail(f"{path.name}: schemaVersion must be 1")
    level_id = data.get("id")
    if not isinstance(level_id, str) or not level_id:
        _fail(f"{path.name}: non-empty id is required")
    if level_id in active_ids:
        _fail(f"duplicate active level id: {level_id}")
    active_ids.add(level_id)
    if not isinstance(data.get("revision"), int) or data["revision"] < 1:
        _fail(f"{level_id}: revision must be >= 1")
    if data.get("variant") != expected_variant:
        _fail(f"{level_id}: variant must be {expected_variant}")
    if not isinstance(data.get("difficultyTag"), str) or not data["difficultyTag"]:
        _fail(f"{level_id}: difficultyTag is required")
    payload = data.get("payload")
    if not isinstance(payload, dict):
        _fail(f"{level_id}: payload object is required")
    width = payload.get("width")
    height = payload.get("height")
    if not isinstance(width, int) or not isinstance(height, int) or not (2 <= width <= 12 and 2 <= height <= 12):
        _fail(f"{level_id}: board dimensions must be integers between 2 and 12")
    return level_id, payload, width, height


def _is_energy_level_solvable(
    width: int,
    height: int,
    pairs: list[tuple[str, tuple[int, int], tuple[int, int]]],
    blocked: set[tuple[int, int]],
) -> bool:
    endpoints = {start for _, start, _ in pairs} | {end for _, _, end in pairs}

    def solve_pair(pair_index: int, occupied: set[tuple[int, int]]) -> bool:
        if pair_index == len(pairs):
            return True
        _, start, end = pairs[pair_index]
        visited = {start}
        path = [start]

        def walk(current: tuple[int, int]) -> bool:
            if current == end:
                return solve_pair(pair_index + 1, occupied | set(path))
            ordered = sorted(
                _neighbors(current, width, height),
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
        _fail(f"Energy Routing requires at least 10 validated levels, found {len(files)}")
    active_ids: set[str] = set()
    for path in files:
        data = _read_level(path)
        level_id, payload, width, height = _validate_common(data, path, "energy_routing", active_ids)
        raw_pairs = payload.get("pairs")
        if not isinstance(raw_pairs, list) or not raw_pairs:
            _fail(f"{level_id}: at least one pair is required")
        pair_ids: set[str] = set()
        endpoints: set[tuple[int, int]] = set()
        pairs: list[tuple[str, tuple[int, int], tuple[int, int]]] = []
        for index, pair in enumerate(raw_pairs):
            if not isinstance(pair, dict):
                _fail(f"{level_id}: pair {index} must be an object")
            pair_id = pair.get("id")
            if not isinstance(pair_id, str) or not pair_id:
                _fail(f"{level_id}: pair {index} needs an id")
            if pair_id in pair_ids:
                _fail(f"{level_id}: duplicate pair id {pair_id}")
            pair_ids.add(pair_id)
            start = _require_cell(pair.get("start"), width, height, f"{level_id}/{pair_id}/start")
            end = _require_cell(pair.get("end"), width, height, f"{level_id}/{pair_id}/end")
            if start == end:
                _fail(f"{level_id}/{pair_id}: endpoints must differ")
            if start in endpoints or end in endpoints:
                _fail(f"{level_id}/{pair_id}: endpoint cell reused")
            endpoints.update((start, end))
            pairs.append((pair_id, start, end))

        raw_blocked = payload.get("blocked", [])
        if not isinstance(raw_blocked, list):
            _fail(f"{level_id}: blocked must be an array")
        blocked: set[tuple[int, int]] = set()
        for index, raw_cell in enumerate(raw_blocked):
            cell = _require_cell(raw_cell, width, height, f"{level_id}/blocked/{index}")
            if cell in blocked:
                _fail(f"{level_id}: duplicate blocked cell {cell}")
            if cell in endpoints:
                _fail(f"{level_id}: blocked cell overlaps endpoint {cell}")
            blocked.add(cell)
        if not _is_energy_level_solvable(width, height, pairs, blocked):
            _fail(f"{level_id}: no non-crossing solution exists")
    return len(files)


def _validate_path_solution(
    level_id: str,
    width: int,
    height: int,
    agents: dict[str, tuple[tuple[int, int], tuple[int, int]]],
    blocked: set[tuple[int, int]],
    raw_solution: object,
) -> None:
    if not isinstance(raw_solution, dict) or set(raw_solution) != set(agents):
        _fail(f"{level_id}: validationSolution must contain exactly one route per agent")
    endpoints = {cell for endpoints_pair in agents.values() for cell in endpoints_pair}
    routes: dict[str, list[tuple[int, int]]] = {}
    for agent_id, (start, goal) in agents.items():
        raw_route = raw_solution[agent_id]
        if not isinstance(raw_route, list) or len(raw_route) < 2:
            _fail(f"{level_id}/{agent_id}: validation route must contain at least 2 cells")
        route = [_require_cell(value, width, height, f"{level_id}/{agent_id}/route") for value in raw_route]
        if route[0] != start or route[-1] != goal:
            _fail(f"{level_id}/{agent_id}: validation route has wrong start/goal")
        visited = {route[0]}
        for index, cell in enumerate(route):
            if cell in blocked:
                _fail(f"{level_id}/{agent_id}: validation route crosses blocked cell {cell}")
            if index == 0:
                continue
            previous = route[index - 1]
            if cell != previous and abs(cell[0] - previous[0]) + abs(cell[1] - previous[1]) != 1:
                _fail(f"{level_id}/{agent_id}: route has non-contiguous step {previous}->{cell}")
            if cell != previous and cell in visited:
                _fail(f"{level_id}/{agent_id}: route self-intersects at {cell}")
            if cell != previous:
                visited.add(cell)
            if cell in endpoints and cell not in (start, goal):
                _fail(f"{level_id}/{agent_id}: route crosses another agent endpoint {cell}")
        routes[agent_id] = route

    max_steps = max(len(route) for route in routes.values())
    agent_ids = sorted(routes)
    for step in range(max_steps):
        positions = {agent_id: routes[agent_id][min(step, len(routes[agent_id]) - 1)] for agent_id in agent_ids}
        if len(set(positions.values())) != len(positions):
            _fail(f"{level_id}: validationSolution has same-cell collision at step {step}")
        if step == 0:
            continue
        previous = {agent_id: routes[agent_id][min(step - 1, len(routes[agent_id]) - 1)] for agent_id in agent_ids}
        for first_index, first in enumerate(agent_ids):
            for second in agent_ids[first_index + 1 :]:
                if previous[first] == positions[second] and previous[second] == positions[first]:
                    _fail(f"{level_id}: validationSolution has swap collision at step {step}")


def validate_path_expedition_levels() -> int:
    files = sorted(PATH_LEVEL_DIR.glob("B-*.json"))
    if len(files) < 10:
        _fail(f"Path / Expedition Routing requires at least 10 validated levels, found {len(files)}")
    active_ids: set[str] = set()
    for path in files:
        data = _read_level(path)
        level_id, payload, width, height = _validate_common(data, path, "path_expedition_routing", active_ids)
        raw_agents = payload.get("agents")
        if not isinstance(raw_agents, list) or not raw_agents:
            _fail(f"{level_id}: at least one agent is required")
        agents: dict[str, tuple[tuple[int, int], tuple[int, int]]] = {}
        endpoints: set[tuple[int, int]] = set()
        for index, raw_agent in enumerate(raw_agents):
            if not isinstance(raw_agent, dict):
                _fail(f"{level_id}: agent {index} must be an object")
            agent_id = raw_agent.get("id")
            if not isinstance(agent_id, str) or not agent_id or agent_id in agents:
                _fail(f"{level_id}: invalid/duplicate agent id at index {index}")
            start = _require_cell(raw_agent.get("start"), width, height, f"{level_id}/{agent_id}/start")
            goal = _require_cell(raw_agent.get("goal"), width, height, f"{level_id}/{agent_id}/goal")
            if start == goal:
                _fail(f"{level_id}/{agent_id}: start and goal must differ")
            if start in endpoints or goal in endpoints:
                _fail(f"{level_id}/{agent_id}: start/goal cell reused")
            endpoints.update((start, goal))
            agents[agent_id] = (start, goal)

        raw_blocked = payload.get("blocked", [])
        if not isinstance(raw_blocked, list):
            _fail(f"{level_id}: blocked must be an array")
        blocked: set[tuple[int, int]] = set()
        for index, raw_cell in enumerate(raw_blocked):
            cell = _require_cell(raw_cell, width, height, f"{level_id}/blocked/{index}")
            if cell in blocked or cell in endpoints:
                _fail(f"{level_id}: invalid blocked cell {cell}")
            blocked.add(cell)
        _validate_path_solution(level_id, width, height, agents, blocked, data.get("validationSolution"))
    return len(files)


def validate_all() -> dict[str, int]:
    return {
        "energy_routing": validate_energy_routing_levels(),
        "path_expedition_routing": validate_path_expedition_levels(),
    }
