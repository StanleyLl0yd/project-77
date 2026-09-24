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

    public sealed class PrototypeMetaProgressionState
    {
        private readonly IReadOnlyList<string> claimedRewardIds;

        public PrototypeMetaProgressionState(
            int scrap,
            int energy,
            bool generatorRepaired,
            bool areaUnlocked,
            bool robot77Discovered,
            IEnumerable<string> claimedRewardIds)
        {
            if (scrap < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scrap));
            }
            if (energy < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(energy));
            }
            if (areaUnlocked && !generatorRepaired)
            {
                throw new ArgumentException("Area unlock requires generator repair.");
            }
            if (robot77Discovered && !areaUnlocked)
            {
                throw new ArgumentException("Robot 77 discovery requires area unlock.");
            }

            var rewards = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rewardId in claimedRewardIds ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(rewardId))
                {
                    throw new ArgumentException("Claimed reward ids cannot be blank.", nameof(claimedRewardIds));
                }
                if (!seen.Add(rewardId))
                {
                    throw new ArgumentException(
                        $"Duplicate claimed reward id '{rewardId}'.",
                        nameof(claimedRewardIds));
                }

                rewards.Add(rewardId);
            }

            rewards.Sort(StringComparer.Ordinal);
            Scrap = scrap;
            Energy = energy;
            GeneratorRepaired = generatorRepaired;
            AreaUnlocked = areaUnlocked;
            Robot77Discovered = robot77Discovered;
            this.claimedRewardIds = rewards.AsReadOnly();
        }

        public int Scrap { get; }
        public int Energy { get; }
        public bool GeneratorRepaired { get; }
        public bool AreaUnlocked { get; }
        public bool Robot77Discovered { get; }
        public IReadOnlyList<string> ClaimedRewardIds => claimedRewardIds;
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

        public PrototypeMetaProgression()
        {
        }

        private PrototypeMetaProgression(PrototypeMetaProgressionState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Scrap = state.Scrap;
            Energy = state.Energy;
            GeneratorRepaired = state.GeneratorRepaired;
            AreaUnlocked = state.AreaUnlocked;
            Robot77Discovered = state.Robot77Discovered;
            foreach (var rewardId in state.ClaimedRewardIds)
            {
                claimedRewardIds.Add(rewardId);
            }
        }

        public bool CanRepairGenerator =>
            !GeneratorRepaired &&
            Scrap >= GeneratorScrapCost &&
            Energy >= GeneratorEnergyCost;

        public static PrototypeMetaProgression Restore(PrototypeMetaProgressionState state)
        {
            return new PrototypeMetaProgression(state);
        }

        public PrototypeMetaProgressionState CaptureState()
        {
            return new PrototypeMetaProgressionState(
                Scrap,
                Energy,
                GeneratorRepaired,
                AreaUnlocked,
                Robot77Discovered,
                claimedRewardIds);
        }

        public bool HasClaimedReward(string rewardId)
        {
            return !string.IsNullOrWhiteSpace(rewardId) &&
                claimedRewardIds.Contains(rewardId);
        }

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
