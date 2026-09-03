#!/usr/bin/env python3
from __future__ import annotations

from dataclasses import dataclass
from typing import Any


@dataclass(frozen=True)
class AuditResult:
    errors: tuple[str, ...]
    warnings: tuple[str, ...]

    @property
    def ok(self) -> bool:
        return not self.errors


def _integer(event: dict[str, Any], name: str) -> int | None:
    value = event.get(name)
    if isinstance(value, int) and not isinstance(value, bool):
        return value
    return None


def audit_event_sequence(events: list[dict[str, Any]]) -> AuditResult:
    errors: list[str] = []
    warnings: list[str] = []
    active: tuple[str, int, int] | None = None
    last_attempt: tuple[str, int, int, str] | None = None
    pending_retry: tuple[str, int, int] | None = None
    last_completed: tuple[str, int] | None = None
    offer: tuple[str, int, str, str, int] | None = None
    expected_next_level: str | None = None
    session_ended = False
    prototype_start_seen = False

    for index, event in enumerate(events, start=1):
        name = event.get("event_name")
        prefix = f"event {index} ({name!r})"
        if session_ended:
            errors.append(f"{prefix}: event appears after session_end")
            continue

        if name == "prototype_start":
            if prototype_start_seen:
                errors.append(f"{prefix}: duplicate prototype_start")
            if index != 1:
                errors.append(f"{prefix}: prototype_start must be the first recorded event")
            prototype_start_seen = True
            continue

        if name == "level_start":
            level_id = event.get("level_id")
            revision = event.get("level_revision")
            attempt = _integer(event, "attempt_index")
            if not isinstance(level_id, str) or not isinstance(revision, int) or attempt is None:
                errors.append(f"{prefix}: malformed level_start identity")
                continue
            if active is not None:
                errors.append(f"{prefix}: another attempt is already active: {active}")
            if pending_retry is not None:
                if (level_id, revision, attempt) != pending_retry:
                    errors.append(
                        f"{prefix}: expected retry start {pending_retry}, got {(level_id, revision, attempt)}"
                    )
                pending_retry = None
            else:
                if attempt != 1:
                    errors.append(f"{prefix}: a non-retry level start must use attempt_index=1")
                if expected_next_level is not None:
                    if level_id != expected_next_level:
                        errors.append(
                            f"{prefix}: expected next level {expected_next_level!r}, got {level_id!r}"
                        )
                    expected_next_level = None
            active = (level_id, revision, attempt)
            last_attempt = None
            last_completed = None
            continue

        if name == "invalid_interaction":
            attempt = _integer(event, "attempt_index")
            identity = (event.get("level_id"), event.get("level_revision"), attempt)
            if active is None:
                errors.append(f"{prefix}: invalid interaction recorded without an active attempt")
            elif identity != active:
                errors.append(f"{prefix}: invalid interaction identity {identity} does not match active {active}")
            continue

        if name in {"level_complete", "level_fail", "level_quit"}:
            attempt = _integer(event, "attempt_index")
            identity = (event.get("level_id"), event.get("level_revision"), attempt)
            if active is None:
                errors.append(f"{prefix}: terminal event recorded without an active attempt")
            elif identity != active:
                errors.append(f"{prefix}: terminal identity {identity} does not match active {active}")
            else:
                level_id, revision, attempt_index = active
                last_attempt = (level_id, revision, attempt_index, str(name))
                active = None
                if name == "level_complete":
                    last_completed = (level_id, revision)
                else:
                    last_completed = None
                    offer = None
            continue

        if name == "level_retry":
            level_id = event.get("level_id")
            revision = event.get("level_revision")
            previous = _integer(event, "previous_attempt_index")
            new = _integer(event, "new_attempt_index")
            if not isinstance(level_id, str) or not isinstance(revision, int) or previous is None or new is None:
                errors.append(f"{prefix}: malformed retry identity")
                continue
            if new != previous + 1:
                errors.append(f"{prefix}: new_attempt_index must equal previous_attempt_index + 1")

            source: tuple[str, int, int] | None = None
            if active is not None:
                source = active
                active = None
            elif last_attempt is not None and last_attempt[3] != "level_quit":
                source = last_attempt[:3]
            if source is None:
                errors.append(f"{prefix}: retry has no active/retryable previous attempt")
            elif source != (level_id, revision, previous):
                errors.append(
                    f"{prefix}: retry previous identity {(level_id, revision, previous)} does not match {source}"
                )

            pending_retry = (level_id, revision, new)
            last_attempt = None
            last_completed = None
            offer = None
            expected_next_level = None
            continue

        if name == "next_puzzle_offered":
            level_id = event.get("level_id")
            revision = event.get("level_revision")
            next_level = event.get("next_level_id")
            context = event.get("offer_context")
            sequence = _integer(event, "offer_sequence_index")
            if active is not None:
                errors.append(f"{prefix}: continuation offered while an attempt is active")
            if last_completed != (level_id, revision):
                errors.append(
                    f"{prefix}: continuation offer does not follow completion of {(level_id, revision)}"
                )
            if offer is not None:
                errors.append(f"{prefix}: another continuation offer is already outstanding")
            if isinstance(level_id, str) and isinstance(revision, int) and isinstance(next_level, str) and isinstance(context, str) and sequence is not None:
                offer = (level_id, revision, context, next_level, sequence)
            continue

        if name == "next_puzzle_clicked":
            level_id = event.get("level_id")
            revision = event.get("level_revision")
            next_level = event.get("next_level_id")
            context = event.get("offer_context")
            sequence = _integer(event, "offer_sequence_index")
            clicked = (level_id, revision, context, next_level, sequence)
            if offer is None:
                errors.append(f"{prefix}: continuation clicked without an outstanding offer")
            elif clicked != offer:
                errors.append(f"{prefix}: continuation click {clicked} does not match offer {offer}")
            offer = None
            last_completed = None
            if isinstance(next_level, str) and not next_level.endswith("-END"):
                expected_next_level = next_level
            else:
                expected_next_level = None
            continue

        if name == "session_end":
            if active is not None:
                errors.append(f"{prefix}: session ended while attempt {active} is still active")
            if pending_retry is not None:
                errors.append(f"{prefix}: session ended while retry start {pending_retry} is pending")
            session_ended = True
            continue

    if not prototype_start_seen:
        errors.append("prototype_start is missing")
    if active is not None:
        warnings.append(f"session ended in data with active attempt {active} and no recorded terminal/session_end")
    if pending_retry is not None:
        warnings.append(f"session data ends before retry attempt {pending_retry} starts")
    if expected_next_level is not None:
        warnings.append(f"session data ends after next click but before level {expected_next_level!r} starts")
    if not session_ended:
        warnings.append("session_end is missing; this may be a crash/force-kill or incomplete export")

    return AuditResult(tuple(errors), tuple(warnings))
