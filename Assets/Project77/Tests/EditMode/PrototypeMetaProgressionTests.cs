using NUnit.Framework;
using Project77.Meta;

namespace Project77.Tests
{
    public sealed class PrototypeMetaProgressionTests
    {
        [Test]
        public void TwoLevelRewards_EnableGeneratorRepair()
        {
            var meta = new PrototypeMetaProgression();

            Assert.That(meta.ClaimReward(meta.RewardForLevel(1)), Is.True);
            Assert.That(meta.CanRepairGenerator, Is.False);

            Assert.That(meta.ClaimReward(meta.RewardForLevel(2)), Is.True);
            Assert.That(meta.Scrap, Is.EqualTo(4));
            Assert.That(meta.Energy, Is.EqualTo(2));
            Assert.That(meta.CanRepairGenerator, Is.True);
        }

        [Test]
        public void ClaimedReward_CannotBeAppliedTwice()
        {
            var meta = new PrototypeMetaProgression();
            var reward = meta.RewardForLevel(1);

            Assert.That(meta.ClaimReward(reward), Is.True);
            Assert.That(meta.ClaimReward(reward), Is.False);
            Assert.That(meta.Scrap, Is.EqualTo(2));
            Assert.That(meta.Energy, Is.EqualTo(1));
        }

        [Test]
        public void RepairUnlockDiscovery_FollowsRequiredOrder()
        {
            var meta = new PrototypeMetaProgression();
            meta.ClaimReward(meta.RewardForLevel(1));
            meta.ClaimReward(meta.RewardForLevel(2));

            Assert.That(meta.UnlockFirstArea(), Is.False);
            Assert.That(meta.DiscoverRobot77(), Is.False);

            var repair = meta.RepairGenerator();
            Assert.That(repair.Accepted, Is.True);
            Assert.That(repair.ScrapSpent, Is.EqualTo(PrototypeMetaProgression.GeneratorScrapCost));
            Assert.That(repair.EnergySpent, Is.EqualTo(PrototypeMetaProgression.GeneratorEnergyCost));
            Assert.That(meta.Scrap, Is.EqualTo(0));
            Assert.That(meta.Energy, Is.EqualTo(0));

            Assert.That(meta.UnlockFirstArea(), Is.True);
            Assert.That(meta.DiscoverRobot77(), Is.True);
            Assert.That(meta.GeneratorRepaired, Is.True);
            Assert.That(meta.AreaUnlocked, Is.True);
            Assert.That(meta.Robot77Discovered, Is.True);
        }
    }
}
