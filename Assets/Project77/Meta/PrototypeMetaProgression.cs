using System;
using System.Collections.Generic;

namespace Project77.Meta
{
    public sealed class PrototypeLevelReward
    {
        public PrototypeLevelReward(string rewardId, int scrapAmount, int energyAmount)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                throw new ArgumentException("Reward id is required.", nameof(rewardId));
            }
            if (scrapAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scrapAmount));
            }
            if (energyAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(energyAmount));
            }

            RewardId = rewardId;
            ScrapAmount = scrapAmount;
            EnergyAmount = energyAmount;
        }

        public string RewardId { get; }
        public int ScrapAmount { get; }
        public int EnergyAmount { get; }
    }

    public sealed class GeneratorRepairResult
    {
        private GeneratorRepairResult(bool accepted, int scrapSpent, int energySpent)
        {
            Accepted = accepted;
            ScrapSpent = scrapSpent;
            EnergySpent = energySpent;
        }

        public bool Accepted { get; }
        public int ScrapSpent { get; }
        public int EnergySpent { get; }

        public static GeneratorRepairResult Reject()
        {
            return new GeneratorRepairResult(false, 0, 0);
        }

        public static GeneratorRepairResult Accept(int scrapSpent, int energySpent)
        {
            return new GeneratorRepairResult(true, scrapSpent, energySpent);
        }
    }

    public sealed class PrototypeMetaProgression
    {
        public const int GeneratorScrapCost = 4;
        public const int GeneratorEnergyCost = 2;
        public const string GeneratorSinkId = "island_generator";
        public const string FirstAreaId = "generator_annex";
        public const string Robot77DiscoveryId = "robot_77_generator_annex";

        private readonly HashSet<string> claimedRewardIds =
            new HashSet<string>(StringComparer.Ordinal);

        public int Scrap { get; private set; }
        public int Energy { get; private set; }
        public bool GeneratorRepaired { get; private set; }
        public bool AreaUnlocked { get; private set; }
        public bool Robot77Discovered { get; private set; }

        public bool CanRepairGenerator =>
            !GeneratorRepaired &&
            Scrap >= GeneratorScrapCost &&
            Energy >= GeneratorEnergyCost;

        public PrototypeLevelReward RewardForLevel(int levelSequenceIndex)
        {
            if (levelSequenceIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(levelSequenceIndex));
            }

            return new PrototypeLevelReward(
                $"p1-level-{levelSequenceIndex:000}",
                scrapAmount: 2,
                energyAmount: 1);
        }

        public bool ClaimReward(PrototypeLevelReward reward)
        {
            if (reward == null)
            {
                throw new ArgumentNullException(nameof(reward));
            }

            if (!claimedRewardIds.Add(reward.RewardId))
            {
                return false;
            }

            Scrap += reward.ScrapAmount;
            Energy += reward.EnergyAmount;
            return true;
        }

        public GeneratorRepairResult RepairGenerator()
        {
            if (!CanRepairGenerator)
            {
                return GeneratorRepairResult.Reject();
            }

            Scrap -= GeneratorScrapCost;
            Energy -= GeneratorEnergyCost;
            GeneratorRepaired = true;
            return GeneratorRepairResult.Accept(GeneratorScrapCost, GeneratorEnergyCost);
        }

        public bool UnlockFirstArea()
        {
            if (!GeneratorRepaired || AreaUnlocked)
            {
                return false;
            }

            AreaUnlocked = true;
            return true;
        }

        public bool DiscoverRobot77()
        {
            if (!AreaUnlocked || Robot77Discovered)
            {
                return false;
            }

            Robot77Discovered = true;
            return true;
        }
    }
}
