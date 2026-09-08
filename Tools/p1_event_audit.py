#!/usr/bin/env python3
from __future__ import annotations

from dataclasses import dataclass
from typing import Any

import p0_event_audit


@dataclass(frozen=True)
class P1AuditResult:
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


def audit_p1_event_sequence(events: list[dict[str, Any]]) -> P1AuditResult:
    core = p0_event_audit.audit_event_sequence(events)
    errors = list(core.errors)
    warnings = list(core.warnings)

    pending_reward: tuple[str, int, int] | None = None
    claimed_rewards: set[str] = set()
    scrap_balance = 0
    energy_balance = 0
    generator_spend = {"scrap": 0, "energy": 0}
    generator_repaired = False
    generator_change_seen = False
    area_unlocked = False
    area_change_seen = False
    robot_discovered = False
    prototype_complete = False

    for index, event in enumerate(events, start=1):
        name = event.get("event_name")
        prefix = f"event {index} ({name!r})"

        if name == "reward_shown":
            reward_id = event.get("reward_id")
            scrap = _integer(event, "scrap_amount")
            energy = _integer(event, "energy_amount")
            if pending_reward is not None:
                errors.append(f"{prefix}: another reward is still pending claim")
            if isinstance(reward_id, str) and scrap is not None and energy is not None:
                pending_reward = (reward_id, scrap, energy)
            continue

        if name == "reward_claimed":
            reward_id = event.get("reward_id")
            scrap = _integer(event, "scrap_amount")
            energy = _integer(event, "energy_amount")
            actual = (reward_id, scrap, energy)
            if pending_reward is None:
                errors.append(f"{prefix}: reward claimed without reward_shown")
            elif actual != pending_reward:
                errors.append(f"{prefix}: claimed reward {actual} does not match shown {pending_reward}")
            if isinstance(reward_id, str):
                if reward_id in claimed_rewards:
                    errors.append(f"{prefix}: reward {reward_id!r} claimed more than once")
                claimed_rewards.add(reward_id)
            if scrap is not None:
                scrap_balance += scrap
            if energy is not None:
                energy_balance += energy
            pending_reward = None
            continue

        if name == "resource_spend":
            resource = event.get("resource_type")
            amount = _integer(event, "amount")
            balance_after = _integer(event, "balance_after")
            sink_id = event.get("sink_id")
            if resource not in {"scrap", "energy"} or amount is None or balance_after is None:
                continue

            if resource == "scrap":
                if amount > scrap_balance:
                    errors.append(f"{prefix}: scrap spend {amount} exceeds tracked balance {scrap_balance}")
                scrap_balance -= amount
                if scrap_balance != balance_after:
                    errors.append(
                        f"{prefix}: scrap balance_after {balance_after} != tracked {scrap_balance}"
                    )
            else:
                if amount > energy_balance:
                    errors.append(f"{prefix}: energy spend {amount} exceeds tracked balance {energy_balance}")
                energy_balance -= amount
                if energy_balance != balance_after:
                    errors.append(
                        f"{prefix}: energy balance_after {balance_after} != tracked {energy_balance}"
                    )

            if sink_id == "island_generator":
                generator_spend[resource] += amount
            continue

        if name == "generator_repair":
            if generator_repaired:
                errors.append(f"{prefix}: duplicate generator_repair")
            scrap_spent = _integer(event, "scrap_spent")
            energy_spent = _integer(event, "energy_spent")
            if scrap_spent is not None and scrap_spent != generator_spend["scrap"]:
                errors.append(
                    f"{prefix}: scrap_spent {scrap_spent} != preceding generator resource_spend {generator_spend['scrap']}"
                )
            if energy_spent is not None and energy_spent != generator_spend["energy"]:
                errors.append(
                    f"{prefix}: energy_spent {energy_spent} != preceding generator resource_spend {generator_spend['energy']}"
                )
            generator_repaired = True
            continue

        if name == "island_change":
            change_type = event.get("change_type")
            caused_by = event.get("caused_by")
            if change_type == "power_on" and caused_by == "generator_repair":
                if not generator_repaired:
                    errors.append(f"{prefix}: generator power-on change precedes generator_repair")
                generator_change_seen = True
            if change_type == "unlock_visual" and caused_by == "area_unlock":
                if not area_unlocked:
                    errors.append(f"{prefix}: area unlock visual precedes area_unlock")
                area_change_seen = True
            continue

        if name == "area_unlock":
            if not generator_repaired:
                errors.append(f"{prefix}: area_unlock precedes generator_repair")
            if area_unlocked:
                errors.append(f"{prefix}: duplicate area_unlock")
            area_unlocked = True
            continue

        if name == "robot_77_discovered":
            if not area_unlocked:
                errors.append(f"{prefix}: robot 77 discovery precedes area_unlock")
            if robot_discovered:
                errors.append(f"{prefix}: duplicate robot_77_discovered")
            robot_discovered = True
            continue

        if name == "next_puzzle_offered" and event.get("offer_context") == "post_77_discovery":
            if not robot_discovered:
                errors.append(f"{prefix}: post_77_discovery offer precedes robot discovery")
            continue

        if name == "session_end" and event.get("end_reason") == "prototype_complete":
            prototype_complete = True

    if pending_reward is not None:
        warnings.append(f"session data ends with unclaimed reward {pending_reward[0]!r}")

    if prototype_complete:
        if not generator_repaired:
            errors.append("prototype_complete session is missing generator_repair")
        if not generator_change_seen:
            errors.append("prototype_complete session is missing generator power-on island_change")
        if not area_unlocked:
            errors.append("prototype_complete session is missing area_unlock")
        if not area_change_seen:
            errors.append("prototype_complete session is missing area unlock island_change")
        if not robot_discovered:
            errors.append("prototype_complete session is missing robot_77_discovered")

    return P1AuditResult(tuple(errors), tuple(warnings))
