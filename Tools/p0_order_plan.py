#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import json
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

VARIANTS = {
    "A": "energy_routing",
    "B": "path_expedition_routing",
    "C": "flow_network_restoration",
}

# One complete block uses every permutation once. This balances every variant
# across positions and every directed carryover pair across the block.
ORDER_BLOCK = (
    ("A", "B", "C"),
    ("B", "C", "A"),
    ("C", "A", "B"),
    ("A", "C", "B"),
    ("C", "B", "A"),
    ("B", "A", "C"),
)


@dataclass(frozen=True)
class Assignment:
    playtest_id: str
    order: tuple[str, str, str]


def _normalize_prefix(value: str) -> str:
    prefix = (value or "").strip()
    if not prefix or len(prefix) > 48 or any(character in prefix for character in "\r\n,/"):
        raise ValueError("prefix must be 1-48 characters and contain no comma, slash or line break")
    return prefix


def generate_assignments(count: int, prefix: str = "p0") -> list[Assignment]:
    if count < 1:
        raise ValueError("count must be >= 1")
    prefix = _normalize_prefix(prefix)
    width = max(3, len(str(count)))
    return [
        Assignment(
            playtest_id=f"{prefix}-{index:0{width}d}",
            order=ORDER_BLOCK[(index - 1) % len(ORDER_BLOCK)],
        )
        for index in range(1, count + 1)
    ]


def position_counts(assignments: Iterable[Assignment]) -> dict[str, list[int]]:
    counts = {code: [0, 0, 0] for code in VARIANTS}
    for assignment in assignments:
        for position, code in enumerate(assignment.order):
            counts[code][position] += 1
    return counts


def transition_counts(assignments: Iterable[Assignment]) -> dict[str, int]:
    counts = {first + second: 0 for first in VARIANTS for second in VARIANTS if first != second}
    for assignment in assignments:
        for first, second in zip(assignment.order, assignment.order[1:]):
            counts[first + second] += 1
    return counts


def validate_balance(assignments: list[Assignment]) -> None:
    if not assignments:
        raise ValueError("assignment plan is empty")
    if len({assignment.playtest_id for assignment in assignments}) != len(assignments):
        raise ValueError("playtest IDs are not unique")
    for assignment in assignments:
        if set(assignment.order) != set(VARIANTS):
            raise ValueError(f"invalid variant order for {assignment.playtest_id}")

    positions = position_counts(assignments)
    for position in range(3):
        values = [positions[code][position] for code in VARIANTS]
        if max(values) - min(values) > 1:
            raise ValueError(f"variant position {position + 1} is not counterbalanced")


def write_csv(path: Path, assignments: list[Assignment]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(
            [
                "playtest_id",
                "order_1_code",
                "order_1_variant",
                "order_2_code",
                "order_2_variant",
                "order_3_code",
                "order_3_variant",
            ]
        )
        for assignment in assignments:
            row: list[str] = [assignment.playtest_id]
            for code in assignment.order:
                row.extend((code, VARIANTS[code]))
            writer.writerow(row)


def write_summary(path: Path, assignments: list[Assignment]) -> None:
    value = {
        "participants": len(assignments),
        "orders": [
            {"playtest_id": assignment.playtest_id, "order": list(assignment.order)}
            for assignment in assignments
        ],
        "position_counts": position_counts(assignments),
        "transition_counts": transition_counts(assignments),
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate a counterbalanced anonymous A/B/C order plan for Project 77 P0 playtests."
    )
    parser.add_argument("--participants", type=int, required=True)
    parser.add_argument("--prefix", default="p0")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--summary-json", type=Path)
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    try:
        assignments = generate_assignments(args.participants, args.prefix)
        validate_balance(assignments)
        write_csv(args.output, assignments)
        if args.summary_json:
            write_summary(args.summary_json, assignments)
    except ValueError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1

    positions = position_counts(assignments)
    print(f"P0 order plan written: {args.output} ({len(assignments)} anonymous participants)")
    print("position counts: " + ", ".join(f"{code}={positions[code]}" for code in VARIANTS))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
