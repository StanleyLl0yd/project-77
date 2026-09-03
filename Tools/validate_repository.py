#!/usr/bin/env python3
from __future__ import annotations

import json
import sys
from pathlib import Path

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
]

EXPECTED_EDITOR = "6000.3.22f1"
EXPECTED_URP_MAJOR_MINOR = "17.3."


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


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

    print(f"Repository validation passed: Unity {editor_version}, URP {urp}, Prototype 0.1 governance present.")


if __name__ == "__main__":
    main()
