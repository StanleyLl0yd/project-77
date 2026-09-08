using System;
using System.Collections.Generic;
using Project77.Analytics;
using Project77.Meta;
using Project77.Puzzle;
using Project77.Puzzle.EnergyRouting;
using Project77.Puzzle.FlowNetworkRestoration;
using Project77.Puzzle.PathExpeditionRouting;

internal static class Program
{
    private static void Main()
    {
        EnergyRoutingSmoke();
        PathExpeditionSmoke();
        FlowNetworkSmoke();
        AnalyticsSmoke();
        MetaSmoke();
        Console.WriteLine("Project 77 pure-domain smoke checks passed.");
    }

    private static void EnergyRoutingSmoke()
    {
        var level = new PrototypeLevelDefinition(
            1,
            "A-SMOKE",
            1,
            PrototypeVariant.EnergyRouting,
            "smoke",
            new EnergyRoutingPayload(
                3,
                3,
                new[] { new EnergyRoutingPair("red", new GridCell(0, 0), new GridCell(2, 0)) },
                Array.Empty<GridCell>()));
        var runner = new EnergyRoutingRunner();
        runner.Load(level);
        runner.Start();
        var result = runner.Apply(new EnergyRoutingPathAction("red", new[]
        {
            new GridCell(0, 0), new GridCell(1, 0), new GridCell(2, 0)
        }));
        Assert(result.Accepted && runner.Status == PuzzleRunStatus.Succeeded, "Energy Routing smoke failed.");
    }

    private static void PathExpeditionSmoke()
    {
        var level = new PrototypeLevelDefinition(
            1,
            "B-SMOKE",
            1,
            PrototypeVariant.PathExpeditionRouting,
            "smoke",
            new PathExpeditionPayload(
                3,
                3,
                new[]
                {
                    new PathExpeditionAgent("red", new GridCell(0, 0), new GridCell(2, 0)),
                    new PathExpeditionAgent("blue", new GridCell(0, 2), new GridCell(2, 2))
                },
                Array.Empty<GridCell>()));
        var runner = new PathExpeditionRunner();
        runner.Load(level);
        runner.Start();
        Assert(runner.Apply(new PathExpeditionSetRouteAction("red", new[]
        {
            new GridCell(0, 0), new GridCell(1, 0), new GridCell(2, 0)
        })).Accepted, "Path red route rejected.");
        Assert(runner.Apply(new PathExpeditionSetRouteAction("blue", new[]
        {
            new GridCell(0, 2), new GridCell(1, 2), new GridCell(2, 2)
        })).Accepted, "Path blue route rejected.");
        Assert(runner.Apply(new PathExpeditionCommitAction()).Accepted && runner.Status == PuzzleRunStatus.Succeeded, "Path / Expedition smoke failed.");
    }

    private static void FlowNetworkSmoke()
    {
        var level = new PrototypeLevelDefinition(
            1,
            "C-SMOKE",
            1,
            PrototypeVariant.FlowNetworkRestoration,
            "smoke",
            new FlowNetworkPayload(
                3,
                3,
                new[]
                {
                    new FlowNetworkTile("source", new GridCell(0, 1), FlowNetworkRole.Source, FlowNetworkDirection.East, false, 0),
                    new FlowNetworkTile("relay", new GridCell(1, 1), FlowNetworkRole.Relay, FlowNetworkDirection.North | FlowNetworkDirection.South, true, 0),
                    new FlowNetworkTile("target", new GridCell(2, 1), FlowNetworkRole.Target, FlowNetworkDirection.West, false, 0)
                }));
        var runner = new FlowNetworkRunner();
        runner.Load(level);
        runner.Start();
        Assert(runner.Status == PuzzleRunStatus.Active, "Flow network should not start solved.");
        Assert(runner.Apply(new FlowNetworkRotateAction("relay")).Accepted && runner.Status == PuzzleRunStatus.Succeeded, "Flow / Network smoke failed.");
    }

    private static void AnalyticsSmoke()
    {
        var context = new PrototypeAnalyticsContext(
            "session-smoke",
            "playtest-smoke",
            "domain-smoke",
            PrototypeVariant.EnergyRouting,
            "unknown",
            "unknown");
        var analyticsEvent = new PrototypeAnalyticsEvent(
            PrototypeAnalyticsEventName.LevelStart,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            context,
            "A-SMOKE",
            1,
            new Dictionary<string, object>
            {
                ["attempt_index"] = 1,
                ["core_variant"] = PrototypeVariant.EnergyRouting,
                ["level_sequence_index"] = 1
            });
        var sink = new InMemoryPrototypeAnalyticsSink();
        sink.Track(analyticsEvent);
        Assert(sink.Events.Count == 1, "Analytics smoke failed.");
    }

    private static void MetaSmoke()
    {
        var meta = new PrototypeMetaProgression();
        Assert(meta.ClaimReward(meta.RewardForLevel(1)), "First P1 reward claim failed.");
        Assert(meta.ClaimReward(meta.RewardForLevel(2)), "Second P1 reward claim failed.");
        Assert(meta.CanRepairGenerator, "P1 generator should be repairable after two rewards.");
        Assert(meta.RepairGenerator().Accepted, "P1 generator repair failed.");
        Assert(meta.UnlockFirstArea(), "P1 area unlock failed.");
        Assert(meta.DiscoverRobot77(), "P1 robot 77 discovery failed.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
