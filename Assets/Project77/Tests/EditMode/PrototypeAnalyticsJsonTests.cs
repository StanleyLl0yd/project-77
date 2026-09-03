using System;
using System.Collections.Generic;
using NUnit.Framework;
using Project77.Analytics;

namespace Project77.Tests
{
    public sealed class PrototypeAnalyticsJsonTests
    {
        [Test]
        public void Serialize_ProducesFlatJsonLineWithEnvelopeAndProperties()
        {
            var analyticsEvent = CreateLevelStartEvent();

            var json = PrototypeAnalyticsJson.Serialize(analyticsEvent);

            Assert.That(json, Does.Contain("\"event_schema_version\":1"));
            Assert.That(json, Does.Contain("\"event_name\":\"level_start\""));
            Assert.That(json, Does.Contain("\"session_id\":\"session-001\""));
            Assert.That(json, Does.Contain("\"level_id\":\"A-001\""));
            Assert.That(json, Does.Contain("\"attempt_index\":1"));
            Assert.That(json, Does.Contain("\"level_sequence_index\":1"));
        }

        [Test]
        public void MissingRequiredEventProperty_IsRejected()
        {
            var context = CreateContext();
            var analyticsEvent = new PrototypeAnalyticsEvent(
                PrototypeAnalyticsEventName.LevelStart,
                1_000,
                context,
                "A-001",
                1,
                new Dictionary<string, object>
                {
                    ["attempt_index"] = 1,
                    ["core_variant"] = "energy_routing"
                });

            var errors = PrototypeAnalyticsValidator.Validate(analyticsEvent);

            Assert.That(string.Join("; ", errors), Does.Contain("level_sequence_index"));
        }

        [Test]
        public void DuplicateTerminalEventForSameAttempt_IsRejected()
        {
            var context = CreateContext();
            var sink = new InMemoryPrototypeAnalyticsSink();
            sink.Track(new PrototypeAnalyticsEvent(
                PrototypeAnalyticsEventName.LevelComplete,
                2_000,
                context,
                "A-001",
                1,
                new Dictionary<string, object>
                {
                    ["attempt_index"] = 1,
                    ["duration_ms"] = 1_000,
                    ["valid_interaction_count"] = 1,
                    ["invalid_interaction_count"] = 0,
                    ["core_variant"] = "energy_routing"
                }));

            var rejected = false;
            try
            {
                sink.Track(new PrototypeAnalyticsEvent(
                    PrototypeAnalyticsEventName.LevelQuit,
                    2_100,
                    context,
                    "A-001",
                    1,
                    new Dictionary<string, object>
                    {
                        ["attempt_index"] = 1,
                        ["duration_ms"] = 1_100,
                        ["quit_destination"] = "app_exit"
                    }));
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }

            Assert.That(rejected, Is.True);
        }

        private static PrototypeAnalyticsEvent CreateLevelStartEvent()
        {
            return new PrototypeAnalyticsEvent(
                PrototypeAnalyticsEventName.LevelStart,
                1_000,
                CreateContext(),
                "A-001",
                1,
                new Dictionary<string, object>
                {
                    ["attempt_index"] = 1,
                    ["core_variant"] = "energy_routing",
                    ["level_sequence_index"] = 1
                });
        }

        private static PrototypeAnalyticsContext CreateContext()
        {
            return new PrototypeAnalyticsContext(
                "session-001",
                "p0-batch-001",
                "test-build",
                "energy_routing",
                "mid",
                "portrait");
        }
    }
}
