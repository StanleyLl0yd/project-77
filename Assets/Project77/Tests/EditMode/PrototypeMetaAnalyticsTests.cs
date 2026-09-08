using System.Collections.Generic;
using NUnit.Framework;
using Project77.Analytics;
using Project77.Puzzle;

namespace Project77.Tests
{
    public sealed class PrototypeMetaAnalyticsTests
    {
        [Test]
        public void SelectedMetaHappyPath_ValidatesFullP1EventChain()
        {
            var context = new PrototypeAnalyticsContext(
                "p1-session",
                "p1-test",
                "p1-build",
                PrototypeVariant.SelectedMeta,
                "unknown",
                "portrait");
            var sink = new InMemoryPrototypeAnalyticsSink();

            Track(sink, context, PrototypeAnalyticsEventName.RewardShown, "A-002", 1, new Dictionary<string, object>
            {
                ["reward_id"] = "p1-level-002",
                ["reward_source"] = "level_complete",
                ["scrap_amount"] = 2,
                ["energy_amount"] = 1
            });
            Track(sink, context, PrototypeAnalyticsEventName.RewardClaimed, "A-002", 1, new Dictionary<string, object>
            {
                ["reward_id"] = "p1-level-002",
                ["claim_mode"] = "explicit",
                ["scrap_amount"] = 2,
                ["energy_amount"] = 1
            });
            Track(sink, context, PrototypeAnalyticsEventName.ResourceSpend, null, null, new Dictionary<string, object>
            {
                ["resource_type"] = "scrap",
                ["amount"] = 4,
                ["sink_id"] = "island_generator",
                ["balance_after"] = 0
            });
            Track(sink, context, PrototypeAnalyticsEventName.ResourceSpend, null, null, new Dictionary<string, object>
            {
                ["resource_type"] = "energy",
                ["amount"] = 2,
                ["sink_id"] = "island_generator",
                ["balance_after"] = 0
            });
            Track(sink, context, PrototypeAnalyticsEventName.GeneratorRepair, null, null, new Dictionary<string, object>
            {
                ["repair_stage"] = 1,
                ["scrap_spent"] = 4,
                ["energy_spent"] = 2,
                ["time_since_session_start_ms"] = 45000
            });
            Track(sink, context, PrototypeAnalyticsEventName.IslandChange, null, null, new Dictionary<string, object>
            {
                ["change_id"] = "generator_power_on",
                ["change_type"] = "power_on",
                ["caused_by"] = "generator_repair"
            });
            Track(sink, context, PrototypeAnalyticsEventName.AreaUnlock, null, null, new Dictionary<string, object>
            {
                ["area_id"] = "generator_annex",
                ["unlock_source"] = "generator_repair"
            });
            Track(sink, context, PrototypeAnalyticsEventName.Robot77Discovered, null, null, new Dictionary<string, object>
            {
                ["discovery_id"] = "robot_77_generator_annex",
                ["time_since_session_start_ms"] = 52000,
                ["levels_completed_before_discovery"] = 2
            });
            Track(sink, context, PrototypeAnalyticsEventName.NextPuzzleOffered, "A-002", 1, new Dictionary<string, object>
            {
                ["offer_context"] = "post_77_discovery",
                ["next_level_id"] = "A-003",
                ["offer_sequence_index"] = 2
            });
            Track(sink, context, PrototypeAnalyticsEventName.NextPuzzleClicked, "A-002", 1, new Dictionary<string, object>
            {
                ["offer_context"] = "post_77_discovery",
                ["next_level_id"] = "A-003",
                ["ms_since_offer"] = 3200,
                ["offer_sequence_index"] = 2
            });

            Assert.That(sink.Events.Count, Is.EqualTo(10));
        }

        private static void Track(
            IPrototypeAnalyticsSink sink,
            PrototypeAnalyticsContext context,
            string eventName,
            string levelId,
            int? levelRevision,
            IReadOnlyDictionary<string, object> properties)
        {
            sink.Track(new PrototypeAnalyticsEvent(
                eventName,
                1000,
                context,
                levelId,
                levelRevision,
                properties));
        }
    }
}
