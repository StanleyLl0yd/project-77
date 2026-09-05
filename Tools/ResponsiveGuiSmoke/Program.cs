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

        Console.WriteLine("Responsive GUI scale smoke passed for compact, 720p, Joy 4-class, high-density, landscape, and unknown-DPI profiles.");
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
}
