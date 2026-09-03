import re
import unittest
from pathlib import Path
import sys

TOOLS = Path(__file__).resolve().parents[1]
ROOT = TOOLS.parent
sys.path.insert(0, str(TOOLS))

import p0_event_schema as schema


class P0ContractParityTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.csharp = (ROOT / "Assets/Project77/Analytics/PrototypeAnalytics.cs").read_text(encoding="utf-8")
        cls.contract = (ROOT / "Docs/30_PROTOTYPE_ANALYTICS_CONTRACT.md").read_text(encoding="utf-8")

    def _csharp_set(self, name: str) -> set[str]:
        match = re.search(
            rf"private static readonly HashSet<string> {re.escape(name)} = Set\((.*?)\);",
            self.csharp,
            re.DOTALL,
        )
        self.assertIsNotNone(match, f"Cannot find C# analytics set {name}")
        return set(re.findall(r'"([^"]+)"', match.group(1)))

    def test_event_names_match_csharp_and_contract_document(self) -> None:
        event_name_block = self.csharp.split("private static readonly HashSet<string> KnownNames", 1)[0]
        csharp_events = set(
            re.findall(r'public const string\s+\w+\s*=\s*"([a-z0-9_]+)";', event_name_block)
        )
        document_events = set(re.findall(r"^### `([a-z0-9_]+)`\s*$", self.contract, re.MULTILINE))

        self.assertEqual(schema.KNOWN_EVENTS, csharp_events)
        self.assertEqual(schema.KNOWN_EVENTS, document_events)

    def test_event_schema_version_matches_csharp(self) -> None:
        match = re.search(r"CurrentSchemaVersion\s*=\s*(\d+);", self.csharp)
        self.assertIsNotNone(match)
        self.assertEqual(schema.EVENT_SCHEMA_VERSION, int(match.group(1)))

    def test_python_enum_sets_match_csharp_validator(self) -> None:
        parity = {
            "DeviceTiers": schema.DEVICE_TIERS,
            "Orientations": schema.ORIENTATIONS,
            "Variants": schema.VARIANTS,
            "CoreVariants": schema.CORE_VARIANTS,
            "EntryPoints": schema.ENTRY_POINTS,
            "PresentationTypes": schema.PRESENTATION_TYPES,
            "RetrySources": schema.RETRY_SOURCES,
            "QuitDestinations": schema.QUIT_DESTINATIONS,
            "InteractionTypes": schema.INTERACTION_TYPES,
            "InvalidReasons": schema.INVALID_REASONS,
            "RewardSources": schema.REWARD_SOURCES,
            "ClaimModes": schema.CLAIM_MODES,
            "ResourceTypes": schema.RESOURCE_TYPES,
            "ChangeTypes": schema.CHANGE_TYPES,
            "ChangeCauses": schema.CHANGE_CAUSES,
            "UnlockSources": schema.UNLOCK_SOURCES,
            "OfferContexts": schema.OFFER_CONTEXTS,
            "EndReasons": schema.END_REASONS,
        }
        for csharp_name, python_values in parity.items():
            with self.subTest(enum_set=csharp_name):
                self.assertEqual(python_values, self._csharp_set(csharp_name))


if __name__ == "__main__":
    unittest.main()
