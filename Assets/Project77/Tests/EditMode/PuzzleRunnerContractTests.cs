using System;
using NUnit.Framework;
using Project77.Puzzle;

namespace Project77.Tests
{
    public sealed class PuzzleRunnerContractTests
    {
        private sealed class TestPayload : IPrototypeLevelPayload
        {
        }

        private sealed class TestAction : IPuzzleAction
        {
            public TestAction(bool completes)
            {
                Completes = completes;
            }

            public bool Completes { get; }
        }

        private sealed class FakeRunner : IPuzzleRunner
        {
            public string Variant => PrototypeVariant.EnergyRouting;
            public PrototypeLevelDefinition Level { get; private set; }
            public PuzzleRunStatus Status { get; private set; } = PuzzleRunStatus.NotLoaded;

            public void Load(PrototypeLevelDefinition level)
            {
                if (!PrototypeLevelValidator.Validate(level).IsValid)
                {
                    throw new ArgumentException("Invalid level.", nameof(level));
                }

                Level = level;
                Status = PuzzleRunStatus.Ready;
            }

            public void Start()
            {
                if (Status != PuzzleRunStatus.Ready)
                {
                    throw new InvalidOperationException("Runner is not ready.");
                }

                Status = PuzzleRunStatus.Active;
            }

            public PuzzleActionResult Apply(IPuzzleAction action)
            {
                if (Status != PuzzleRunStatus.Active)
                {
                    return PuzzleActionResult.Reject("not_active");
                }

                if (!(action is TestAction testAction))
                {
                    return PuzzleActionResult.Reject("wrong_action");
                }

                if (testAction.Completes)
                {
                    Status = PuzzleRunStatus.Succeeded;
                }

                return PuzzleActionResult.Accept();
            }

            public void Restart()
            {
                if (Level == null)
                {
                    throw new InvalidOperationException("No level loaded.");
                }

                Status = PuzzleRunStatus.Ready;
                Start();
            }
        }

        [Test]
        public void SharedRunnerBoundary_CoversLoadStartApplyAndRestart()
        {
            var level = new PrototypeLevelDefinition(
                PrototypeLevelDefinition.CurrentSchemaVersion,
                "A-001",
                1,
                PrototypeVariant.EnergyRouting,
                "intro",
                new TestPayload());
            var runner = new FakeRunner();

            runner.Load(level);
            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Ready));

            runner.Start();
            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Active));

            Assert.That(runner.Apply(new TestAction(false)).Accepted, Is.True);
            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Active));

            Assert.That(runner.Apply(new TestAction(true)).Accepted, Is.True);
            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Succeeded));

            runner.Restart();
            Assert.That(runner.Status, Is.EqualTo(PuzzleRunStatus.Active));
        }
    }
}
