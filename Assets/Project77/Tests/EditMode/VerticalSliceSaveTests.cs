using System;
using System.IO;
using NUnit.Framework;
using Project77.Save;

namespace Project77.Tests
{
    public sealed class VerticalSliceSaveTests
    {
        [Test]
        public void Codec_RoundTripIsDeterministicAndPreservesState()
        {
            var state = BuildState();
            var first = VerticalSliceSaveCodec.Serialize(state);

            Assert.That(
                VerticalSliceSaveCodec.TryDeserialize(first, out var decoded, out var error),
                Is.True,
                error);

            var second = VerticalSliceSaveCodec.Serialize(decoded);
            Assert.That(second, Is.EqualTo(first));
            Assert.That(decoded.PlayerId, Is.EqualTo(state.PlayerId));
            Assert.That(decoded.EnergyRoutingLevelIndex, Is.EqualTo(4));
            Assert.That(decoded.Scrap, Is.EqualTo(3));
            Assert.That(decoded.Energy, Is.EqualTo(1));
            Assert.That(decoded.GeneratorRepaired, Is.True);
            Assert.That(decoded.AreaUnlocked, Is.True);
            Assert.That(decoded.Robot77Discovered, Is.True);
            Assert.That(decoded.NarrativeStep, Is.EqualTo(1));
            Assert.That(decoded.ClaimedRewardIds.Count, Is.EqualTo(2));
            Assert.That(decoded.ClaimedRewardIds, Does.Contain("p1-level-001"));
            Assert.That(decoded.ClaimedRewardIds, Does.Contain("p1-level-002"));
        }

        [Test]
        public void Codec_RejectsFutureSchema()
        {
            var payload = VerticalSliceSaveCodec.Serialize(BuildState())
                .Replace("schemaVersion=1", "schemaVersion=2");

            Assert.That(
                VerticalSliceSaveCodec.TryDeserialize(payload, out _, out var error),
                Is.False);
            Assert.That(error, Does.Contain("unsupported schemaVersion 2"));
        }

        [Test]
        public void Validator_RejectsInvalidCausalityAndDuplicateRewards()
        {
            var state = new VerticalSliceSaveState(
                VerticalSliceSaveState.CurrentSchemaVersion,
                Guid.NewGuid().ToString("N"),
                energyRoutingLevelIndex: 1,
                scrap: 0,
                energy: 0,
                generatorRepaired: false,
                areaUnlocked: true,
                robot77Discovered: true,
                narrativeStep: 1,
                hapticsEnabled: true,
                reducedMotionEnabled: false,
                claimedRewardIds: new[] { "reward-1", "reward-1" });

            var errors = VerticalSliceSaveValidator.Validate(state);
            Assert.That(errors, Does.Contain("area unlock requires generator repair"));
            Assert.That(errors, Does.Contain("duplicate claimed reward id: reward-1"));
        }

        [Test]
        public void Migrator_NormalizesCurrentSchema()
        {
            var canonical = VerticalSliceSaveCodec.Serialize(BuildState());

            Assert.That(
                VerticalSliceSaveMigrator.TryNormalizeToCurrent(
                    canonical.Replace("\n", "\r\n"),
                    out var normalized,
                    out var error),
                Is.True,
                error);
            Assert.That(normalized, Is.EqualTo(canonical));
        }

        [Test]
        public void Store_FallsBackToBackupWhenPrimaryIsCorrupt()
        {
            var directory = CreateTempDirectory();
            try
            {
                var store = new AtomicLocalSaveStore(directory);
                var first = BuildState(levelIndex: 2);
                var second = BuildState(levelIndex: 5);

                store.Save(first);
                store.Save(second);
                File.WriteAllText(store.PrimaryPath, "corrupt");

                var loaded = store.Load();
                Assert.That(loaded.Found, Is.True, loaded.Error);
                Assert.That(loaded.Source, Is.EqualTo("backup"));
                Assert.That(loaded.State.EnergyRoutingLevelIndex, Is.EqualTo(2));
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        [Test]
        public void Store_RecoversValidatedTemporarySaveAfterInterruptedReplace()
        {
            var directory = CreateTempDirectory();
            try
            {
                var store = new AtomicLocalSaveStore(directory);
                Directory.CreateDirectory(directory);
                File.WriteAllText(
                    store.TemporaryPath,
                    VerticalSliceSaveCodec.Serialize(BuildState(levelIndex: 7)));

                var loaded = store.Load();
                Assert.That(loaded.Found, Is.True, loaded.Error);
                Assert.That(loaded.Source, Is.EqualTo("temporary"));
                Assert.That(loaded.State.EnergyRoutingLevelIndex, Is.EqualTo(7));
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        [Test]
        public void Store_DoesNotReplaceGoodBackupWithCorruptPrimary()
        {
            var directory = CreateTempDirectory();
            try
            {
                var store = new AtomicLocalSaveStore(directory);
                store.Save(BuildState(levelIndex: 1));
                store.Save(BuildState(levelIndex: 2));
                File.WriteAllText(store.PrimaryPath, "corrupt");

                store.Save(BuildState(levelIndex: 3));
                File.WriteAllText(store.PrimaryPath, "corrupt again");

                var loaded = store.Load();
                Assert.That(loaded.Found, Is.True, loaded.Error);
                Assert.That(loaded.Source, Is.EqualTo("backup"));
                Assert.That(loaded.State.EnergyRoutingLevelIndex, Is.EqualTo(1));
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static VerticalSliceSaveState BuildState(int levelIndex = 4)
        {
            return new VerticalSliceSaveState(
                VerticalSliceSaveState.CurrentSchemaVersion,
                Guid.NewGuid().ToString("N"),
                levelIndex,
                scrap: 3,
                energy: 1,
                generatorRepaired: true,
                areaUnlocked: true,
                robot77Discovered: true,
                narrativeStep: 1,
                hapticsEnabled: true,
                reducedMotionEnabled: false,
                claimedRewardIds: new[] { "p1-level-002", "p1-level-001" });
        }

        private static string CreateTempDirectory()
        {
            return Path.Combine(
                Path.GetTempPath(),
                "project77-save-tests-" + Guid.NewGuid().ToString("N"));
        }

        private static void DeleteDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
