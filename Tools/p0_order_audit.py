#!/usr/bin/env python3
from __future__ import annotations

import csv
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Mapping

from p0_order_plan import VARIANTS

REQUIRED_COLUMNS = (
    "playtest_id",
    "order_1_code",
    "order_1_variant",
    "order_2_code",
    "order_2_variant",
    "order_3_code",
    "order_3_variant",
)


class OrderAuditError(ValueError):
    pass


@dataclass(frozen=True)
class OrderAuditResult:
    errors: tuple[str, ...]
    warnings: tuple[str, ...]
    verified_sessions: int
    planned_participants: int
    observed_participants: int

    @property
    def ok(self) -> bool:
        return not self.errors


def load_order_plan(path: Path) -> dict[str, tuple[str, str, str]]:
    try:
        handle = path.open("r", encoding="utf-8-sig", newline="")
    except OSError as exc:
        raise OrderAuditError(f"cannot read order plan {path}: {exc}") from exc

    with handle:
        reader = csv.DictReader(handle)
        if reader.fieldnames is None:
            raise OrderAuditError("order plan CSV has no header")
        missing = [name for name in REQUIRED_COLUMNS if name not in reader.fieldnames]
        if missing:
            raise OrderAuditError("order plan CSV missing columns: " + ", ".join(missing))

        plan: dict[str, tuple[str, str, str]] = {}
        for line_no, row in enumerate(reader, start=2):
            playtest_id = (row["playtest_id"] or "").strip()
            if not playtest_id:
                raise OrderAuditError(f"order plan row {line_no}: playtest_id is required")
            if playtest_id in plan:
                raise OrderAuditError(f"order plan row {line_no}: duplicate playtest_id {playtest_id!r}")

            codes = tuple((row[f"order_{index}_code"] or "").strip() for index in range(1, 4))
            variants = tuple((row[f"order_{index}_variant"] or "").strip() for index in range(1, 4))
            if set(codes) != set(VARIANTS):
                raise OrderAuditError(
                    f"order plan row {line_no}: codes must contain A, B and C exactly once"
                )
            expected_variants = tuple(VARIANTS[code] for code in codes)
            if variants != expected_variants:
                raise OrderAuditError(
                    f"order plan row {line_no}: variant names do not match codes {codes}"
                )
            plan[playtest_id] = expected_variants

    if not plan:
        raise OrderAuditError("order plan is empty")
    return plan


def audit_assignments(
    sessions: list[Any],
    moderation: Mapping[str, Any],
    plan: Mapping[str, tuple[str, str, str]],
) -> OrderAuditResult:
    errors: list[str] = []
    warnings: list[str] = []
    verified = 0
    observed_participants: set[str] = set()
    seen_slots: set[tuple[str, int]] = set()
    seen_variants: set[tuple[str, str]] = set()

    for session in sessions:
        metadata = getattr(session, "metadata", {})
        session_id = str(getattr(session, "session_id", metadata.get("session_id", "")))
        playtest_id = metadata.get("playtest_id")
        variant = str(getattr(session, "variant", metadata.get("prototype_variant", "")))
        if not isinstance(playtest_id, str) or not playtest_id.strip():
            errors.append(f"session {session_id!r}: playtest_id is missing")
            continue
        playtest_id = playtest_id.strip()
        observed_participants.add(playtest_id)

        assigned = plan.get(playtest_id)
        if assigned is None:
            errors.append(f"session {session_id!r}: playtest_id {playtest_id!r} is not in the order plan")
            continue

        duplicate_variant = (playtest_id, variant)
        if duplicate_variant in seen_variants:
            errors.append(
                f"playtest_id {playtest_id!r}: variant {variant!r} appears in more than one session"
            )
        seen_variants.add(duplicate_variant)

        record = moderation.get(session_id)
        if record is None:
            warnings.append(
                f"session {session_id!r}: no moderation row, so assigned order position cannot be verified"
            )
            continue

        position = getattr(record, "variant_order_index", None)
        if not isinstance(position, int) or isinstance(position, bool) or not 1 <= position <= 3:
            warnings.append(
                f"session {session_id!r}: variant_order_index is missing/invalid; assigned order position cannot be verified"
            )
            continue

        slot = (playtest_id, position)
        if slot in seen_slots:
            errors.append(
                f"playtest_id {playtest_id!r}: order position {position} is used by more than one session"
            )
            continue
        seen_slots.add(slot)

        expected_variant = assigned[position - 1]
        if variant != expected_variant:
            errors.append(
                f"session {session_id!r}: order position {position} expects {expected_variant!r}, got {variant!r}"
            )
            continue
        verified += 1

    for playtest_id in sorted(observed_participants):
        assigned = plan.get(playtest_id)
        if assigned is None:
            continue
        observed_slots = sorted(position for pid, position in seen_slots if pid == playtest_id)
        if len(observed_slots) < 3:
            warnings.append(
                f"playtest_id {playtest_id!r}: only order positions {observed_slots or 'none'} are verified; full A/B/C crossover is incomplete"
            )

    return OrderAuditResult(
        tuple(errors),
        tuple(warnings),
        verified_sessions=verified,
        planned_participants=len(plan),
        observed_participants=len(observed_participants),
    )
