using NUnit.Framework;
using Project77.Puzzle;
using Project77.Puzzle.PathExpeditionRouting;

namespace Project77.Tests
{
    public sealed class PathExpeditionRunnerTests
    {
        [Test]
        public void ParallelRoutes_CommitSuccessfully()
        {
            var runner = CreateRunner(new[]
            {
                new PathExpeditionAgent("red", new GridCell(0, 0), new GridCell(3, 0)),
                new PathExpeditionAgent("blue", new GridCell(0, 3), new GridCell(3, 3))
            });

            Assert.That(runner.Apply(new PathExpeditionSetRouteAction("red", new[]
            {
                new GridCell(0, 0), new GridCell(1, 0), new GridCell(2, 0), new GridCell(3, 0)
            })).Accepted, Is.True);
            Assert.That(runner.Apply(new PathExpeditionSetRouteAction("blue", new[]
            {
                new GridCell(0, 3), new GridCell(1, 3), new GridCell(2, 3), new GridCell(3, 3)
            })).Accepted, Is.True);

            var result = runner.Apply(new PathExpeditionCommitAction());

            Assert.That(result.Accepted, Is.True);
            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Succeeded));
        }

        [Test]
        public void SimultaneousCrossing_FailsWithCollision()
        {
            var runner = CreateRunner(new[]
            {
                new PathExpeditionAgent("red", new GridCell(0, 1), new GridCell(2, 1)),
                new PathExpeditionAgent("blue", new GridCell(1, 0), new GridCell(1, 2))
            });
            runner.Apply(new PathExpeditionSetRouteAction("red", new[]
            {
                new GridCell(0, 1), new GridCell(1, 1), new GridCell(2, 1)
            }));
            runner.Apply(new PathExpeditionSetRouteAction("blue", new[]
            {
                new GridCell(1, 0), new GridCell(1, 1), new GridCell(1, 2)
            }));

            runner.Apply(new PathExpeditionCommitAction());

            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Failed));
            Assert.That(runner.LastFailReason, Is.EqualTo(PathExpeditionFailReason.SameCellCollision));
        }

        [Test]
        public void WaitStep_CanResolveTemporalCrossing()
        {
            var runner = CreateRunner(new[]
            {
                new PathExpeditionAgent("red", new GridCell(0, 1), new GridCell(2, 1)),
                new PathExpeditionAgent("blue", new GridCell(1, 0), new GridCell(1, 2))
            });
            runner.Apply(new PathExpeditionSetRouteAction("red", new[]
            {
                new GridCell(0, 1), new GridCell(1, 1), new GridCell(2, 1)
            }));
            runner.Apply(new PathExpeditionSetRouteAction("blue", new[]
            {
                new GridCell(1, 0), new GridCell(1, 0), new GridCell(1, 1), new GridCell(1, 2)
            }));

            runner.Apply(new PathExpeditionCommitAction());

            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Succeeded));
        }

        private static PathExpeditionRunner CreateRunner(PathExpeditionAgent[] agents)
        {
            var level = new PrototypeLevelDefinition(
                1,
                "B-TEST",
                1,
                PrototypeVariant.PathExpeditionRouting,
                "test",
                new PathExpeditionPayload(4, 4, agents, new GridCell[0]));
            var runner = new PathExpeditionRunner();
            runner.Load(level);
            runner.Start();
            return runner;
        }
    }
}
