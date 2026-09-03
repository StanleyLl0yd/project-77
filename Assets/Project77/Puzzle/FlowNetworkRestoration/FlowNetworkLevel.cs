using System;
using System.Collections.Generic;

namespace Project77.Puzzle.FlowNetworkRestoration
{
    public static class FlowNetworkRole
    {
        public const string Source = "source";
        public const string Target = "target";
        public const string Hazard = "hazard";
        public const string Relay = "relay";

        public static bool IsKnown(string role)
        {
            return role == Source || role == Target || role == Hazard || role == Relay;
        }
    }

    public static class FlowNetworkDirection
    {
        public const int North = 1;
        public const int East = 2;
        public const int South = 4;
        public const int West = 8;

        public static int RotateClockwise(int mask, int quarterTurns)
        {
            var turns = ((quarterTurns % 4) + 4) % 4;
            for (var index = 0; index < turns; index++)
            {
                var rotated = 0;
                if ((mask & North) != 0) rotated |= East;
                if ((mask & East) != 0) rotated |= South;
                if ((mask & South) != 0) rotated |= West;
                if ((mask & West) != 0) rotated |= North;
                mask = rotated;
            }
            return mask;
        }
    }

    public sealed class FlowNetworkTile
    {
        public FlowNetworkTile(
            string id,
            GridCell cell,
            string role,
            int connectorMask,
            bool rotatable,
            int initialRotation)
        {
            Id = id;
            Cell = cell;
            Role = role;
            ConnectorMask = connectorMask;
            Rotatable = rotatable;
            InitialRotation = initialRotation;
        }

        public string Id { get; }
        public GridCell Cell { get; }
        public string Role { get; }
        public int ConnectorMask { get; }
        public bool Rotatable { get; }
        public int InitialRotation { get; }
    }

    public sealed class FlowNetworkPayload : IPrototypeLevelPayload
    {
        public FlowNetworkPayload(int width, int height, IReadOnlyList<FlowNetworkTile> tiles)
        {
            Width = width;
            Height = height;
            Tiles = tiles ?? Array.Empty<FlowNetworkTile>();
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<FlowNetworkTile> Tiles { get; }

        public bool Contains(GridCell cell)
        {
            return cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
        }
    }

    public static class FlowNetworkLevelValidator
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

            if (level.Variant != PrototypeVariant.FlowNetworkRestoration)
            {
                errors.Add("Flow / Network level must use flow_network_restoration variant.");
            }

            if (!(level.Payload is FlowNetworkPayload payload))
            {
                errors.Add("Flow / Network level requires FlowNetworkPayload.");
                return errors;
            }

            if (payload.Width < 2 || payload.Width > 12 || payload.Height < 2 || payload.Height > 12)
            {
                errors.Add("Board dimensions must be between 2 and 12 cells per axis for Prototype C.");
            }

            if (payload.Tiles.Count == 0)
            {
                errors.Add("At least one network tile is required.");
                return errors;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var cells = new HashSet<GridCell>();
            var sourceCount = 0;
            var targetCount = 0;
            foreach (var tile in payload.Tiles)
            {
                if (tile == null)
                {
                    errors.Add("Network tile cannot be null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(tile.Id))
                {
                    errors.Add("Network tile id is required.");
                }
                else if (!ids.Add(tile.Id))
                {
                    errors.Add($"Duplicate network tile id: '{tile.Id}'.");
                }

                if (!payload.Contains(tile.Cell))
                {
                    errors.Add($"Tile '{tile.Id}' is outside the board.");
                }
                if (!cells.Add(tile.Cell))
                {
                    errors.Add($"Multiple network tiles occupy {tile.Cell}.");
                }
                if (!FlowNetworkRole.IsKnown(tile.Role))
                {
                    errors.Add($"Tile '{tile.Id}' has unknown role '{tile.Role}'.");
                }
                if (tile.Role == FlowNetworkRole.Source) sourceCount++;
                if (tile.Role == FlowNetworkRole.Target) targetCount++;
                if (tile.ConnectorMask < 1 || tile.ConnectorMask > 15)
                {
                    errors.Add($"Tile '{tile.Id}' connector mask must use N/E/S/W bits 1..15.");
                }
                if (tile.InitialRotation < 0 || tile.InitialRotation > 3)
                {
                    errors.Add($"Tile '{tile.Id}' initial rotation must be 0..3.");
                }
            }

            if (sourceCount == 0) errors.Add("At least one source tile is required.");
            if (targetCount == 0) errors.Add("At least one target tile is required.");
            return errors;
        }
    }
}
