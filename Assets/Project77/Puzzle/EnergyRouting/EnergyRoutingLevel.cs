using System;
using System.Collections.Generic;

namespace Project77.Puzzle.EnergyRouting
{
    public sealed class EnergyRoutingPair
    {
        public EnergyRoutingPair(string id, GridCell start, GridCell end)
        {
            Id = id;
            Start = start;
            End = end;
        }

        public string Id { get; }
        public GridCell Start { get; }
        public GridCell End { get; }
    }

    public sealed class EnergyRoutingPayload : IPrototypeLevelPayload
    {
        public EnergyRoutingPayload(
            int width,
            int height,
            IReadOnlyList<EnergyRoutingPair> pairs,
            IReadOnlyList<GridCell> blockedCells)
        {
            Width = width;
            Height = height;
            Pairs = pairs ?? Array.Empty<EnergyRoutingPair>();
            BlockedCells = blockedCells ?? Array.Empty<GridCell>();
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<EnergyRoutingPair> Pairs { get; }
        public IReadOnlyList<GridCell> BlockedCells { get; }

        public bool Contains(GridCell cell)
        {
            return cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
        }
    }

    public static class EnergyRoutingLevelValidator
    {
        public static IReadOnlyList<string> Validate(PrototypeLevelDefinition level)
        {
            var errors = new List<string>();
            var common = PrototypeLevelValidator.Validate(level);
            foreach (var error in common.Errors)
            {
                errors.Add(error);
            }

            if (level == null)
            {
                return errors;
            }

            if (level.Variant != PrototypeVariant.EnergyRouting)
            {
                errors.Add("Energy Routing level must use the energy_routing variant.");
            }

            if (!(level.Payload is EnergyRoutingPayload payload))
            {
                errors.Add("Energy Routing level requires EnergyRoutingPayload.");
                return errors;
            }

            if (payload.Width < 2 || payload.Width > 12 || payload.Height < 2 || payload.Height > 12)
            {
                errors.Add("Board dimensions must be between 2 and 12 cells per axis for Prototype A.");
            }

            if (payload.Pairs.Count == 0)
            {
                errors.Add("At least one routing pair is required.");
            }

            var pairIds = new HashSet<string>(StringComparer.Ordinal);
            var endpoints = new HashSet<GridCell>();
            foreach (var pair in payload.Pairs)
            {
                if (pair == null)
                {
                    errors.Add("Routing pair cannot be null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(pair.Id))
                {
                    errors.Add("Routing pair id is required.");
                }
                else if (!pairIds.Add(pair.Id))
                {
                    errors.Add($"Duplicate routing pair id: '{pair.Id}'.");
                }

                if (!payload.Contains(pair.Start) || !payload.Contains(pair.End))
                {
                    errors.Add($"Pair '{pair.Id}' has an endpoint outside the board.");
                }

                if (pair.Start.Equals(pair.End))
                {
                    errors.Add($"Pair '{pair.Id}' endpoints must be different.");
                }

                if (!endpoints.Add(pair.Start) || !endpoints.Add(pair.End))
                {
                    errors.Add($"Pair '{pair.Id}' reuses an endpoint cell.");
                }
            }

            var blocked = new HashSet<GridCell>();
            foreach (var cell in payload.BlockedCells)
            {
                if (!payload.Contains(cell))
                {
                    errors.Add($"Blocked cell {cell} is outside the board.");
                }

                if (!blocked.Add(cell))
                {
                    errors.Add($"Blocked cell {cell} is duplicated.");
                }

                if (endpoints.Contains(cell))
                {
                    errors.Add($"Blocked cell {cell} overlaps a routing endpoint.");
                }
            }

            return errors;
        }
    }
}
