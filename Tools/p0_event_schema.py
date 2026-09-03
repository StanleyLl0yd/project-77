#!/usr/bin/env python3
from __future__ import annotations

import re
from typing import Any

EVENT_SCHEMA_VERSION = 1

CORE_VARIANTS = {
    "energy_routing",
    "path_expedition_routing",
    "flow_network_restoration",
}
VARIANTS = CORE_VARIANTS | {"selected_meta", "unknown"}
DEVICE_TIERS = {"low", "mid", "high", "unknown"}
ORIENTATIONS = {"portrait", "landscape", "unknown"}
ENTRY_POINTS = {"fresh_launch", "restart", "variant_select"}
PRESENTATION_TYPES = {"text", "gesture", "highlight", "animation", "other"}
RETRY_SOURCES = {"fail_screen", "manual_restart", "other"}
QUIT_DESTINATIONS = {"prototype_menu", "app_exit", "island", "other"}
INTERACTION_TYPES = {"tap", "drag", "path_start", "path_end", "other"}
INVALID_REASONS = {"out_of_bounds", "blocked", "wrong_target", "rule_violation", "no_effect", "other"}
REWARD_SOURCES = {"level_complete", "meta_step", "other"}
CLAIM_MODES = {"explicit", "auto"}
RESOURCE_TYPES = {"scrap", "energy", "other"}
CHANGE_TYPES = {"repair", "power_on", "reveal", "unlock_visual", "other"}
CHANGE_CAUSES = {"generator_repair", "area_unlock", "story_step", "other"}
UNLOCK_SOURCES = {"generator_repair", "story_step", "other"}
OFFER_CONTEXTS = {"post_level", "post_reward", "post_island_change", "post_77_discovery"}
END_REASONS = {"user_exit", "prototype_complete", "moderator_end", "app_background_timeout", "other"}

KNOWN_EVENTS = {
    "prototype_start",
    "tutorial_exposed",
    "level_start",
    "level_complete",
    "level_fail",
    "level_retry",
    "level_quit",
    "invalid_interaction",
    "reward_shown",
    "reward_claimed",
    "resource_spend",
    "generator_repair",
    "island_change",
    "area_unlock",
    "robot_77_discovered",
    "next_puzzle_offered",
    "next_puzzle_clicked",
    "session_end",
}

LEVEL_EVENTS = {
    "level_start",
    "level_complete",
    "level_fail",
    "level_retry",
    "level_quit",
    "invalid_interaction",
    "next_puzzle_offered",
    "next_puzzle_clicked",
}

SNAKE_CASE = re.compile(r"^[a-z0-9]+(?:_[a-z0-9]+)*$")


def _is_int(value: Any) -> bool:
    return isinstance(value, int) and not isinstance(value, bool)


def _required(event: dict[str, Any], name: str, errors: list[str]) -> Any:
    if name not in event:
        errors.append(f"missing required field/property {name!r}")
        return None
    return event[name]


def _text(event: dict[str, Any], name: str, errors: list[str], *, nullable: bool = False) -> None:
    value = _required(event, name, errors)
    if name not in event:
        return
    if value is None and nullable:
        return
    if not isinstance(value, str) or not value.strip():
        errors.append(f"{name} must be a non-empty string" + (" or null" if nullable else ""))


def _optional_text(event: dict[str, Any], name: str, errors: list[str]) -> None:
    if name not in event or event[name] is None:
        return
    if not isinstance(event[name], str) or not event[name].strip():
        errors.append(f"{name} must be a non-empty string or null when present")


def _integer(event: dict[str, Any], name: str, minimum: int, errors: list[str]) -> None:
    value = _required(event, name, errors)
    if name not in event:
        return
    if not _is_int(value) or value < minimum:
        errors.append(f"{name} must be an integer >= {minimum}")


def _optional_integer(event: dict[str, Any], name: str, minimum: int, errors: list[str]) -> None:
    if name not in event or event[name] is None:
        return
    value = event[name]
    if not _is_int(value) or value < minimum:
        errors.append(f"{name} must be null or an integer >= {minimum}")


def _enum(event: dict[str, Any], name: str, allowed: set[str], errors: list[str], *, nullable: bool = False) -> None:
    value = _required(event, name, errors)
    if name not in event:
        return
    if value is None and nullable:
        return
    if not isinstance(value, str) or value not in allowed:
        suffix = " or null" if nullable else ""
        errors.append(f"{name} must be one of {sorted(allowed)}{suffix}")


def _snake(event: dict[str, Any], name: str, errors: list[str]) -> None:
    value = _required(event, name, errors)
    if name not in event:
        return
    if not isinstance(value, str) or SNAKE_CASE.fullmatch(value) is None:
        errors.append(f"{name} must be a lowercase snake_case value")


def _level_required(event: dict[str, Any], errors: list[str]) -> None:
    level_id = event.get("level_id")
    revision = event.get("level_revision")
    if not isinstance(level_id, str) or not level_id.strip():
        errors.append("level_id must be a non-empty string for this event")
    if not _is_int(revision) or revision < 1:
        errors.append("level_revision must be an integer >= 1 for this event")


def validate_event(event: Any) -> list[str]:
    errors: list[str] = []
    if not isinstance(event, dict):
        return ["event must be a JSON object"]

    schema = _required(event, "event_schema_version", errors)
    if "event_schema_version" in event and (not _is_int(schema) or schema != EVENT_SCHEMA_VERSION):
        errors.append(f"event_schema_version must equal {EVENT_SCHEMA_VERSION}")

    name = _required(event, "event_name", errors)
    if "event_name" in event and (not isinstance(name, str) or name not in KNOWN_EVENTS):
        errors.append(f"unknown event_name {name!r}")

    timestamp = _required(event, "timestamp_utc_ms", errors)
    if "timestamp_utc_ms" in event and (not _is_int(timestamp) or timestamp < 0):
        errors.append("timestamp_utc_ms must be a non-negative integer")

    _text(event, "session_id", errors)
    _text(event, "playtest_id", errors)
    _text(event, "build_version", errors)
    _enum(event, "prototype_variant", VARIANTS, errors)
    _enum(event, "device_tier", DEVICE_TIERS, errors)
    _enum(event, "screen_orientation", ORIENTATIONS, errors)

    if "level_id" not in event:
        errors.append("missing required field/property 'level_id'")
    if "level_revision" not in event:
        errors.append("missing required field/property 'level_revision'")
    if "level_id" in event and "level_revision" in event:
        level_id = event["level_id"]
        revision = event["level_revision"]
        if (level_id is None) != (revision is None):
            errors.append("level_id and level_revision must both be null or both be present")
        elif level_id is not None:
            if not isinstance(level_id, str) or not level_id.strip():
                errors.append("level_id must be a non-empty string when present")
            if not _is_int(revision) or revision < 1:
                errors.append("level_revision must be an integer >= 1 when present")

    if not isinstance(name, str) or name not in KNOWN_EVENTS:
        return errors
    if name in LEVEL_EVENTS:
        _level_required(event, errors)

    if name == "prototype_start":
        _enum(event, "entry_point", ENTRY_POINTS, errors)
        _enum(event, "core_variant", CORE_VARIANTS, errors, nullable=True)
    elif name == "tutorial_exposed":
        _text(event, "tutorial_step_id", errors)
        _integer(event, "exposure_index", 1, errors)
        _enum(event, "presentation_type", PRESENTATION_TYPES, errors)
        _optional_integer(event, "auto_advance_ms", 0, errors)
    elif name == "level_start":
        _integer(event, "attempt_index", 1, errors)
        _enum(event, "core_variant", CORE_VARIANTS, errors)
        _integer(event, "level_sequence_index", 1, errors)
    elif name == "level_complete":
        _integer(event, "attempt_index", 1, errors)
        _integer(event, "duration_ms", 0, errors)
        _integer(event, "valid_interaction_count", 0, errors)
        _integer(event, "invalid_interaction_count", 0, errors)
        _enum(event, "core_variant", CORE_VARIANTS, errors)
        _optional_integer(event, "move_count", 0, errors)
        _optional_integer(event, "path_action_count", 0, errors)
    elif name == "level_fail":
        _integer(event, "attempt_index", 1, errors)
        _integer(event, "duration_ms", 0, errors)
        _snake(event, "fail_reason", errors)
        _enum(event, "core_variant", CORE_VARIANTS, errors)
    elif name == "level_retry":
        _integer(event, "previous_attempt_index", 1, errors)
        _integer(event, "new_attempt_index", 1, errors)
        _enum(event, "retry_source", RETRY_SOURCES, errors)
    elif name == "level_quit":
        _integer(event, "attempt_index", 1, errors)
        _integer(event, "duration_ms", 0, errors)
        _enum(event, "quit_destination", QUIT_DESTINATIONS, errors)
    elif name == "invalid_interaction":
        _enum(event, "interaction_type", INTERACTION_TYPES, errors)
        _enum(event, "invalid_reason", INVALID_REASONS, errors)
        _integer(event, "attempt_index", 1, errors)
        _optional_text(event, "board_element_id", errors)
    elif name == "reward_shown":
        _text(event, "reward_id", errors)
        _enum(event, "reward_source", REWARD_SOURCES, errors)
        _integer(event, "scrap_amount", 0, errors)
        _integer(event, "energy_amount", 0, errors)
    elif name == "reward_claimed":
        _text(event, "reward_id", errors)
        _enum(event, "claim_mode", CLAIM_MODES, errors)
        _integer(event, "scrap_amount", 0, errors)
        _integer(event, "energy_amount", 0, errors)
    elif name == "resource_spend":
        _enum(event, "resource_type", RESOURCE_TYPES, errors)
        _integer(event, "amount", 1, errors)
        _text(event, "sink_id", errors)
        _integer(event, "balance_after", 0, errors)
    elif name == "generator_repair":
        _integer(event, "repair_stage", 1, errors)
        _integer(event, "scrap_spent", 0, errors)
        _integer(event, "energy_spent", 0, errors)
        _integer(event, "time_since_session_start_ms", 0, errors)
    elif name == "island_change":
        _text(event, "change_id", errors)
        _enum(event, "change_type", CHANGE_TYPES, errors)
        _enum(event, "caused_by", CHANGE_CAUSES, errors)
    elif name == "area_unlock":
        _text(event, "area_id", errors)
        _enum(event, "unlock_source", UNLOCK_SOURCES, errors)
    elif name == "robot_77_discovered":
        _text(event, "discovery_id", errors)
        _integer(event, "time_since_session_start_ms", 0, errors)
        _integer(event, "levels_completed_before_discovery", 0, errors)
    elif name == "next_puzzle_offered":
        _enum(event, "offer_context", OFFER_CONTEXTS, errors)
        _text(event, "next_level_id", errors)
        _integer(event, "offer_sequence_index", 1, errors)
    elif name == "next_puzzle_clicked":
        _enum(event, "offer_context", OFFER_CONTEXTS, errors)
        _text(event, "next_level_id", errors)
        _integer(event, "ms_since_offer", 0, errors)
        _integer(event, "offer_sequence_index", 1, errors)
    elif name == "session_end":
        _integer(event, "duration_ms", 0, errors)
        _integer(event, "levels_started", 0, errors)
        _integer(event, "levels_completed", 0, errors)
        _enum(event, "end_reason", END_REASONS, errors)
        _optional_text(event, "last_visible_step", errors)

    return errors


def validate_events(events: list[dict[str, Any]]) -> list[str]:
    errors: list[str] = []
    for index, event in enumerate(events, start=1):
        for error in validate_event(event):
            errors.append(f"event {index}: {error}")
    return errors
