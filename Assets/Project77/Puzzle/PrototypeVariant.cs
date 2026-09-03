namespace Project77.Puzzle
{
    public static class PrototypeVariant
    {
        public const string EnergyRouting = "energy_routing";
        public const string PathExpeditionRouting = "path_expedition_routing";
        public const string FlowNetworkRestoration = "flow_network_restoration";
        public const string SelectedMeta = "selected_meta";
        public const string Unknown = "unknown";

        public static bool IsCoreVariant(string value)
        {
            return value == EnergyRouting ||
                   value == PathExpeditionRouting ||
                   value == FlowNetworkRestoration;
        }

        public static bool IsAnalyticsVariant(string value)
        {
            return IsCoreVariant(value) || value == SelectedMeta || value == Unknown;
        }
    }
}
