using System.Collections.Generic;
using NUnit.Framework;
using Project77.Analytics;

namespace Project77.Tests
{
    public sealed class PrototypeAnalyticsTests
    {
        [Test]
        public void HappyPathLevelStart_ProducesSchemaValidEvent()
        {
            var context = new PrototypeAnalyticsContext(
                sessionId: "session-001",
                playtestId: "p0-batch-001",
                buildVersion: "test-build",
                prototypeVariant: "energy_routing",
                deviceTier: "mid",
                screenOrientation: "portrait");
            var analyticsEvent = new PrototypeAnalyticsEvent(
                PrototypeAnalyticsEventName.LevelStart,
                timestampUtcMs: 1_000,
                context,
                levelId: "A-001",
                levelRevision: 1,
                properties: new Dictionary<string, object>
                {
                    ["attempt_index"] = 1,
                    ["core_variant"] = "energy_routing",
                    ["level_sequence_index"] = 1
                });
            var sink = new InMemoryPrototypeAnalyticsSink();

            sink.Track(analyticsEvent);

            Assert.That(sink.Events.Count, Is.EqualTo(1));
            Assert.That(PrototypeAnalyticsValidator.Validate(analyticsEvent), Is.Empty);
            Assert.That(sink.Events[0].EventName, Is.EqualTo("level_start"));
            Assert.That(sink.Events[0].LevelId, Is.EqualTo("A-001"));
            Assert.That(sink.Events[0].LevelRevision, Is.EqualTo(1));
        }

        [Test]
        public void UnknownEnumValue_IsRejectedInsteadOfNormalized()
        {
            var context = new PrototypeAnalyticsContext(
                "session-001",
                "p0-batch-001",
                "test-build",
                "energy_routing",
                "super_phone",
                "portrait");
            var analyticsEvent = new PrototypeAnalyticsEvent(
                PrototypeAnalyticsEventName.PrototypeStart,
                1_000,
                context,
                null,
                null,
                new Dictionary<string, object>());

            var errors = PrototypeAnalyticsValidator.Validate(analyticsEvent);

            Assert.That(string.Join("; ", errors), Does.Contain("device_tier"));
        }
    }
}
