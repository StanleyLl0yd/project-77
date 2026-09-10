using System;
using Project77.Game;

internal static class Program
{
    private static void Main()
    {
        AssertScale("invalid dimensions", 0, 0, 0f, 1f);
        AssertScale("legacy 480p phone", 480, 854, 240f, 1.5f);
        AssertScale("720p phone", 720, 1280, 320f, 2f);
        AssertScale("Joy 4 class portrait", 1080, 2340, 400f, 2.5f);
        AssertScale("modern high-density portrait cap", 1080, 2400, 480f, 2.5f);
        AssertScale("landscape readability cap", 2340, 1080, 400f, 1.85f);
        AssertScale("unknown-DPI 720p fallback", 720, 1440, 0f, 1.5f);

        AssertEqual("red endpoint one", EnergyRoutingPresentation.EndpointLabel("red", 1), "R1");
        AssertEqual("red endpoint two", EnergyRoutingPresentation.EndpointLabel("red", 2), "R2");
        AssertEqual("blue endpoint one", EnergyRoutingPresentation.EndpointLabel("blue", 1), "B1");

        var firstInstruction = EnergyRoutingPresentation.Instruction(1, 0, 0);
        AssertContains("first-level touch target", firstInstruction, "touch R1");
        AssertContains("first-level drag gesture", firstInstruction, "keep your finger down");
        AssertContains("first-level matching target", firstInstruction, "R2");

        var blockedInstruction = EnergyRoutingPresentation.Instruction(3, 1, 0);
        AssertContains("blocked-cell rule", blockedInstruction, "X squares are blocked");

        var clearLegend = EnergyRoutingPresentation.Legend(new[] { "red", "blue" }, false);
        AssertContains("pair redundancy", clearLegend, "R1 <-> R2");
        AssertContains("second pair redundancy", clearLegend, "B1 <-> B2");
        AssertContains("explicit no obstacles", clearLegend, "No blocked squares");

        var blockedLegend = EnergyRoutingPresentation.Legend(new[] { "red" }, true);
        AssertContains("explicit obstacle legend", blockedLegend, "X = BLOCKED");

        Console.WriteLine(
            "Responsive GUI and Energy Routing first-use presentation smoke passed for phone profiles, explicit drag guidance, redundant pair labels, and blocked-cell semantics.");
    }

    private static void AssertScale(string name, int width, int height, float dpi, float expected)
    {
        var actual = PrototypeGuiLayout.CalculateScale(width, height, dpi);
        if (Math.Abs(actual - expected) > 0.001f)
        {
            throw new InvalidOperationException(
                $"{name}: expected scale {expected:0.###}, got {actual:0.###} for {width}x{height} @ {dpi:0.#} dpi.");
        }
    }

    private static void AssertEqual(string name, string actual, string expected)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
        }
    }

    private static void AssertContains(string name, string actual, string expectedFragment)
    {
        if (actual == null || actual.IndexOf(expectedFragment, StringComparison.Ordinal) < 0)
        {
            throw new InvalidOperationException(
                $"{name}: expected fragment '{expectedFragment}' in '{actual ?? "<null>"}'.");
        }
    }
}
