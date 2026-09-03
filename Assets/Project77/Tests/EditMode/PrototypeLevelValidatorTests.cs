using NUnit.Framework;
using Project77.Puzzle;

namespace Project77.Tests
{
    public sealed class PrototypeLevelValidatorTests
    {
        private sealed class TestPayload : IPrototypeLevelPayload
        {
        }

        [Test]
        public void ValidDefinition_PreservesIdentityAndPassesValidation()
        {
            var level = CreateLevel("A-001", 3);

            var result = PrototypeLevelValidator.Validate(level);

            Assert.That(result.IsValid, Is.True, string.Join("; ", result.Errors));
            Assert.That(level.Id, Is.EqualTo("A-001"));
            Assert.That(level.Revision, Is.EqualTo(3));
            Assert.That(level.Variant, Is.EqualTo(PrototypeVariant.EnergyRouting));
        }

        [Test]
        public void InvalidDefinition_FailsLoudly()
        {
            var level = new PrototypeLevelDefinition(
                schemaVersion: 99,
                id: "bad id",
                revision: 0,
                variant: "not_a_variant",
                difficultyTag: string.Empty,
                payload: null);

            var result = PrototypeLevelValidator.Validate(level);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(5));
        }

        [Test]
        public void DuplicateActiveId_IsRejectedEvenWhenRevisionDiffers()
        {
            var result = PrototypeLevelValidator.ValidateSet(new[]
            {
                CreateLevel("A-001", 1),
                CreateLevel("A-001", 2)
            });

            Assert.That(result.IsValid, Is.False);
            Assert.That(string.Join("; ", result.Errors), Does.Contain("Duplicate active level id"));
        }

        private static PrototypeLevelDefinition CreateLevel(string id, int revision)
        {
            return new PrototypeLevelDefinition(
                PrototypeLevelDefinition.CurrentSchemaVersion,
                id,
                revision,
                PrototypeVariant.EnergyRouting,
                "intro",
                new TestPayload());
        }
    }
}
