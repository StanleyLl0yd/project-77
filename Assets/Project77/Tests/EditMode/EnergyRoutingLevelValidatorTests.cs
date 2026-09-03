using NUnit.Framework;
using Project77.Puzzle;
using Project77.Puzzle.EnergyRouting;

namespace Project77.Tests
{
    public sealed class EnergyRoutingLevelValidatorTests
    {
        [Test]
        public void EndpointOnBlockedCell_IsRejected()
        {
            var endpoint = new GridCell(0, 0);
            var level = new PrototypeLevelDefinition(
                1,
                "A-TEST",
                1,
                PrototypeVariant.EnergyRouting,
                "test",
                new EnergyRoutingPayload(
                    4,
                    4,
                    new[] { new EnergyRoutingPair("red", endpoint, new GridCell(3, 0)) },
                    new[] { endpoint }));

            var errors = EnergyRoutingLevelValidator.Validate(level);

            Assert.That(string.Join("; ", errors), Does.Contain("overlaps a routing endpoint"));
        }

        [Test]
        public void DuplicateEndpoint_IsRejected()
        {
            var shared = new GridCell(0, 0);
            var level = new PrototypeLevelDefinition(
                1,
                "A-TEST",
                1,
                PrototypeVariant.EnergyRouting,
                "test",
                new EnergyRoutingPayload(
                    4,
                    4,
                    new[]
                    {
                        new EnergyRoutingPair("red", shared, new GridCell(3, 0)),
                        new EnergyRoutingPair("blue", shared, new GridCell(0, 3))
                    },
                    new GridCell[0]));

            var errors = EnergyRoutingLevelValidator.Validate(level);

            Assert.That(string.Join("; ", errors), Does.Contain("reuses an endpoint cell"));
        }
    }
}
