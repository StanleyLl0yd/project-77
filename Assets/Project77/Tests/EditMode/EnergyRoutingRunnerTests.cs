using NUnit.Framework;
using Project77.Puzzle;
using Project77.Puzzle.EnergyRouting;

namespace Project77.Tests
{
    public sealed class EnergyRoutingRunnerTests
    {
        [Test]
        public void ValidPaths_CompleteLevelDeterministically()
        {
            var runner = CreateRunner();

            var first = runner.Apply(new EnergyRoutingPathAction("red", new[]
            {
                new GridCell(0, 0),
                new GridCell(1, 0),
                new GridCell(2, 0),
                new GridCell(3, 0)
            }));
            var second = runner.Apply(new EnergyRoutingPathAction("blue", new[]
            {
                new GridCell(0, 3),
                new GridCell(1, 3),
                new GridCell(2, 3),
                new GridCell(3, 3)
            }));

            Assert.That(first.Accepted, Is.True);
            Assert.That(second.Accepted, Is.True);
            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Succeeded));
        }

        [Test]
        public void PathThroughBlockedCell_IsRejectedWithoutChangingState()
        {
            var level = CreateLevel(
                blocked: new[] { new GridCell(1, 0) },
                pairs: new[] { new EnergyRoutingPair("red", new GridCell(0, 0), new GridCell(3, 0)) });
            var runner = new EnergyRoutingRunner();
            runner.Load(level);
            runner.Start();

            var result = runner.Apply(new EnergyRoutingPathAction("red", new[]
            {
                new GridCell(0, 0),
                new GridCell(1, 0),
                new GridCell(2, 0),
                new GridCell(3, 0)
            }));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(EnergyRoutingInvalidReason.Blocked));
            Assert.That(runner.GetPath("red"), Is.Empty);
            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Active));
        }

        [Test]
        public void CrossingExistingPath_IsRejected()
        {
            var level = CreateLevel(
                blocked: new GridCell[0],
                pairs: new[]
                {
                    new EnergyRoutingPair("red", new GridCell(0, 1), new GridCell(3, 1)),
                    new EnergyRoutingPair("blue", new GridCell(2, 0), new GridCell(2, 3))
                });
            var runner = new EnergyRoutingRunner();
            runner.Load(level);
            runner.Start();
            Assert.That(runner.Apply(new EnergyRoutingPathAction("red", new[]
            {
                new GridCell(0, 1), new GridCell(1, 1), new GridCell(2, 1), new GridCell(3, 1)
            })).Accepted, Is.True);

            var result = runner.Apply(new EnergyRoutingPathAction("blue", new[]
            {
                new GridCell(2, 0), new GridCell(2, 1), new GridCell(2, 2), new GridCell(2, 3)
            }));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(EnergyRoutingInvalidReason.Crossing));
        }

        [Test]
        public void Restart_ClearsAcceptedPathsAndRestoresActiveState()
        {
            var runner = CreateRunner();
            Assert.That(runner.Apply(new EnergyRoutingPathAction("red", new[]
            {
                new GridCell(0, 0), new GridCell(1, 0), new GridCell(2, 0), new GridCell(3, 0)
            })).Accepted, Is.True);

            runner.Restart();

            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Active));
            Assert.That(runner.GetPath("red"), Is.Empty);
        }

        private static EnergyRoutingRunner CreateRunner()
        {
            var runner = new EnergyRoutingRunner();
            runner.Load(CreateLevel(
                blocked: new GridCell[0],
                pairs: new[]
                {
                    new EnergyRoutingPair("red", new GridCell(0, 0), new GridCell(3, 0)),
                    new EnergyRoutingPair("blue", new GridCell(0, 3), new GridCell(3, 3))
                }));
            runner.Start();
            return runner;
        }

        private static PrototypeLevelDefinition CreateLevel(
            GridCell[] blocked,
            EnergyRoutingPair[] pairs)
        {
            return new PrototypeLevelDefinition(
                PrototypeLevelDefinition.CurrentSchemaVersion,
                "A-TEST",
                1,
                PrototypeVariant.EnergyRouting,
                "test",
                new EnergyRoutingPayload(4, 4, pairs, blocked));
        }
    }
}
