import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


class P1ExternalUxTests(unittest.TestCase):
    def test_runtime_selector_exposes_only_selected_p1_path(self):
        text = (ROOT / "Assets/Project77/Game/PrototypeVariantSelector.cs").read_text(encoding="utf-8")
        runtime_text = re.sub(
            r"#if UNITY_EDITOR.*?#endif",
            "",
            text,
            flags=re.DOTALL,
        )

        self.assertIn("Start playtest", runtime_text)
        self.assertIn("TryBeginSelectedMeta", runtime_text)
        self.assertNotIn("A — Energy Routing only", runtime_text)
        self.assertNotIn("B — Path / Expedition Routing", runtime_text)
        self.assertNotIn("C — Flow / Network Restoration", runtime_text)
        self.assertNotIn("P0 debug variants", runtime_text)

    def test_energy_routing_has_redundant_non_color_semantics(self):
        controller = (ROOT / "Assets/Project77/Game/EnergyRoutingPrototypeController.cs").read_text(encoding="utf-8")
        presentation = (ROOT / "Assets/Project77/Game/EnergyRoutingPresentation.cs").read_text(encoding="utf-8")

        self.assertIn("EnergyRoutingPresentation.EndpointLabel", controller)
        self.assertIn('GUI.Label(rect, "X"', controller)
        self.assertIn("X = BLOCKED", presentation)
        self.assertIn('builder.Append(code).Append("1 <-> ").Append(code).Append(\'2\')', presentation)
        self.assertIn("keep your finger down", presentation)
        self.assertIn("up, down, left or right", presentation)
        self.assertIn("No blocked squares", presentation)

    def test_first_level_is_single_pair_onboarding(self):
        path = ROOT / "Assets/Project77/Content/Resources/Prototype/EnergyRouting/A-001.json"
        level = json.loads(path.read_text(encoding="utf-8"))

        self.assertEqual(level["id"], "A-001")
        self.assertGreaterEqual(level["revision"], 2)
        self.assertEqual(level["difficultyTag"], "intro")
        self.assertEqual(len(level["payload"]["pairs"]), 1)
        self.assertEqual(level["payload"]["pairs"][0]["id"], "red")
        self.assertEqual(level["payload"]["blocked"], [])

    def test_board_is_anchored_below_guidance_instead_of_floating_mid_screen(self):
        controller = (ROOT / "Assets/Project77/Game/EnergyRoutingPrototypeController.cs").read_text(encoding="utf-8")

        self.assertIn("GetBoardTop() + 12f", controller)
        self.assertNotIn("boardTop + (availableHeight - height) * 0.5f", controller)

    def test_interrupted_session_data_can_be_recovered_on_next_setup(self):
        selector = (ROOT / "Assets/Project77/Game/PrototypeVariantSelector.cs").read_text(encoding="utf-8")

        self.assertIn('DataFolderName = "Project77Playtests"', selector)
        self.assertIn("RefreshPreviousSessionFiles();", selector)
        self.assertIn("HasPreviousSessionData", selector)
        self.assertIn("Moderator: previous session data", selector)
        self.assertIn("FindLatestSessionPair(directory, out previousEventsPath, out previousMetadataPath);", selector)
        self.assertIn('EventsSuffix = "_events.jsonl"', selector)
        self.assertIn('MetadataSuffix = "_metadata.json"', selector)
        self.assertIn("stem + MetadataSuffix", selector)
        self.assertIn("File.Exists(metadataPath)", selector)
        self.assertIn("File.GetLastWriteTimeUtc(eventPath)", selector)
        self.assertIn("IsReadableFile(previousEventsPath) && IsReadableFile(previousMetadataPath)", selector)
        self.assertNotIn('FindLatest(directory, "*_events.jsonl")', selector)
        self.assertNotIn('FindLatest(directory, "*_metadata.json")', selector)
        self.assertIn('CopyPreviousFile(previousEventsPath, "Previous events copied")', selector)
        self.assertIn('CopyPreviousFile(previousMetadataPath, "Previous metadata copied")', selector)
        self.assertIn("GUIUtility.systemCopyBuffer = File.ReadAllText(path);", selector)
        self.assertIn("+ recoveryHeight", selector)

    def test_selected_meta_preserves_fresh_launch_entry_point(self):
        runtime = (ROOT / "Assets/Project77/Game/PrototypePlaytestRuntime.cs").read_text(encoding="utf-8")
        controller = (ROOT / "Assets/Project77/Game/EnergyRoutingPrototypeController.cs").read_text(encoding="utf-8")

        self.assertIn('["entry_point"] = "fresh_launch"', controller)
        self.assertRegex(
            runtime,
            r"analyticsEvent\.EventName == PrototypeAnalyticsEventName\.PrototypeStart\s*&&\s*"
            r"selectedVariant != PrototypeVariant\.SelectedMeta",
        )
        self.assertIn('["entry_point"] = "variant_select"', runtime)
        self.assertIn('WritePrototypeStart("restart");', runtime)

    def test_external_copy_uses_player_language_not_internal_phase_labels(self):
        selector = (ROOT / "Assets/Project77/Game/PrototypeVariantSelector.cs").read_text(encoding="utf-8")
        runtime_text = re.sub(
            r"#if UNITY_EDITOR.*?#endif",
            "",
            selector,
            flags=re.DOTALL,
        )

        self.assertNotIn("Prototype A", runtime_text)
        self.assertNotIn("Prototype B", runtime_text)
        self.assertNotIn("Prototype C", runtime_text)
        self.assertNotIn("P1", runtime_text)
        self.assertIn("Playtest build", runtime_text)


if __name__ == "__main__":
    unittest.main()
