using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Project77.Analytics
{
    public static class PrototypeAnalyticsJson
    {
        private static readonly HashSet<string> EnvelopeNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "event_schema_version",
            "event_name",
            "timestamp_utc_ms",
            "session_id",
            "playtest_id",
            "build_version",
            "prototype_variant",
            "level_id",
            "level_revision",
            "device_tier",
            "screen_orientation"
        };

        public static string Serialize(PrototypeAnalyticsEvent analyticsEvent)
        {
            var errors = PrototypeAnalyticsValidator.Validate(analyticsEvent);
            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join("; ", errors), nameof(analyticsEvent));
            }

            var builder = new StringBuilder(512);
            builder.Append('{');
            var first = true;

            Append(builder, ref first, "event_schema_version", analyticsEvent.EventSchemaVersion);
            Append(builder, ref first, "event_name", analyticsEvent.EventName);
            Append(builder, ref first, "timestamp_utc_ms", analyticsEvent.TimestampUtcMs);
            Append(builder, ref first, "session_id", analyticsEvent.Context.SessionId);
            Append(builder, ref first, "playtest_id", analyticsEvent.Context.PlaytestId);
            Append(builder, ref first, "build_version", analyticsEvent.Context.BuildVersion);
            Append(builder, ref first, "prototype_variant", analyticsEvent.Context.PrototypeVariant);
            Append(builder, ref first, "level_id", analyticsEvent.LevelId);
            Append(builder, ref first, "level_revision", analyticsEvent.LevelRevision);
            Append(builder, ref first, "device_tier", analyticsEvent.Context.DeviceTier);
            Append(builder, ref first, "screen_orientation", analyticsEvent.Context.ScreenOrientation);

            var propertyNames = new List<string>(analyticsEvent.Properties.Keys);
            propertyNames.Sort(StringComparer.Ordinal);
            foreach (var propertyName in propertyNames)
            {
                if (EnvelopeNames.Contains(propertyName))
                {
                    throw new ArgumentException(
                        $"Analytics property '{propertyName}' collides with the event envelope.",
                        nameof(analyticsEvent));
                }

                Append(builder, ref first, propertyName, analyticsEvent.Properties[propertyName]);
            }

            builder.Append('}');
            return builder.ToString();
        }

        private static void Append(StringBuilder builder, ref bool first, string name, object value)
        {
            if (!first)
            {
                builder.Append(',');
            }

            first = false;
            AppendString(builder, name);
            builder.Append(':');
            AppendValue(builder, value);
        }

        private static void AppendValue(StringBuilder builder, object value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            if (value is string text)
            {
                AppendString(builder, text);
                return;
            }

            if (value is bool boolean)
            {
                builder.Append(boolean ? "true" : "false");
                return;
            }

            if (value is int intValue)
            {
                builder.Append(intValue.ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (value is long longValue)
            {
                builder.Append(longValue.ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (value is short shortValue)
            {
                builder.Append(shortValue.ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (value is byte byteValue)
            {
                builder.Append(byteValue.ToString(CultureInfo.InvariantCulture));
                return;
            }

            throw new NotSupportedException(
                $"Analytics JSON value type '{value.GetType().FullName}' is not supported.");
        }

        private static void AppendString(StringBuilder builder, string value)
        {
            builder.Append('"');
            if (value != null)
            {
                foreach (var character in value)
                {
                    switch (character)
                    {
                        case '"':
                            builder.Append("\\\"");
                            break;
                        case '\\':
                            builder.Append("\\\\");
                            break;
                        case '\b':
                            builder.Append("\\b");
                            break;
                        case '\f':
                            builder.Append("\\f");
                            break;
                        case '\n':
                            builder.Append("\\n");
                            break;
                        case '\r':
                            builder.Append("\\r");
                            break;
                        case '\t':
                            builder.Append("\\t");
                            break;
                        default:
                            if (character < 0x20)
                            {
                                builder.Append("\\u");
                                builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                builder.Append(character);
                            }
                            break;
                    }
                }
            }

            builder.Append('"');
        }
    }

    public sealed class JsonLinesPrototypeAnalyticsSink : IPrototypeAnalyticsSink
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
        private readonly object sync = new object();

        public JsonLinesPrototypeAnalyticsSink(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Telemetry path is required.", nameof(path));
            }

            Path = path;
            var directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public string Path { get; }

        public void Track(PrototypeAnalyticsEvent analyticsEvent)
        {
            var line = PrototypeAnalyticsJson.Serialize(analyticsEvent);
            lock (sync)
            {
                File.AppendAllText(Path, line + Environment.NewLine, Utf8NoBom);
            }
        }
    }
}
