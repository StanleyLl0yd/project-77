using NUnit.Framework;
using Project77.Puzzle;
using Project77.Puzzle.FlowNetworkRestoration;

namespace Project77.Tests
{
    public sealed class FlowNetworkRunnerTests
    {
        [Test]
        public void RotateStraightTile_PowersTargetAndCompletesLevel()
        {
            var runner = CreateRunner(new[]
            {
                new FlowNetworkTile("source", new GridCell(0, 1), FlowNetworkRole.Source, FlowNetworkDirection.East, false, 0),
                new FlowNetworkTile("relay", new GridCell(1, 1), FlowNetworkRole.Relay, FlowNetworkDirection.North | FlowNetworkDirection.South, true, 0),
                new FlowNetworkTile("target", new GridCell(2, 1), FlowNetworkRole.Target, FlowNetworkDirection.West, false, 0)
            });

            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Active));
            Assert.That(runner.IsPowered("target"), Is.False);

            var result = runner.Apply(new FlowNetworkRotateAction("relay"));

            Assert.That(result.Accepted, Is.True);
            Assert.That(runner.IsPowered("target"), Is.True);
            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Succeeded));
        }

        [Test]
        public void HazardPowered_PreventsSuccessUntilBranchRotatesAway()
        {
            var runner = CreateRunner(new[]
            {
                new FlowNetworkTile("source", new GridCell(0, 1), FlowNetworkRole.Source, FlowNetworkDirection.East, false, 0),
                new FlowNetworkTile("switch", new GridCell(1, 1), FlowNetworkRole.Relay, FlowNetworkDirection.South | FlowNetworkDirection.West, true, 0),
                new FlowNetworkTile("hazard", new GridCell(1, 0), FlowNetworkRole.Hazard, FlowNetworkDirection.North, false, 0),
                new FlowNetworkTile("target", new GridCell(1, 2), FlowNetworkRole.Target, FlowNetworkDirection.South, false, 0)
            });

            Assert.That(runner.IsPowered("hazard"), Is.True);
            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Active));

            runner.Apply(new FlowNetworkRotateAction("switch"));

            Assert.That(runner.IsPowered("hazard"), Is.False);
            Assert.That(runner.IsPowered("target"), Is.True);
            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Succeeded));
        }

        [Test]
        public void Restart_RestoresInitialRotationsAndPowerState()
        {
            var runner = CreateRunner(new[]
            {
                new FlowNetworkTile("source", new GridCell(0, 1), FlowNetworkRole.Source, FlowNetworkDirection.East, false, 0),
                new FlowNetworkTile("relay", new GridCell(1, 1), FlowNetworkRole.Relay, FlowNetworkDirection.North | FlowNetworkDirection.South, true, 0),
                new FlowNetworkTile("target", new GridCell(2, 1), FlowNetworkRole.Target, FlowNetworkDirection.West, false, 0)
            });
            runner.Apply(new FlowNetworkRotateAction("relay"));
            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Succeeded));

            runner.Restart();

            Assert.That(runner.GetRotation("relay"), Is.EqualTo(0));
            Assert.That(runner.IsPowered("target"), Is.False);
            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Active));
        }

        private static FlowNetworkRunner CreateRunner(FlowNetworkTile[] tiles)
        {
            var level = new PrototypeLevelDefinition(
                1,
                "C-TEST",
                1,
                PrototypeVariant.FlowNetworkRestoration,
                "test",
                new FlowNetworkPayload(4, 4, tiles));
            var runner = new FlowNetworkRunner();
            runner.Load(level);
            runner.Start();
            return runner;
        }
    }
}
