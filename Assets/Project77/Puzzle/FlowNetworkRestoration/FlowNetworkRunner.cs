using System;
using System.Collections.Generic;

namespace Project77.Puzzle.FlowNetworkRestoration
{
    public static class FlowNetworkInvalidReason
    {
        public const string NotActive = "not_active";
        public const string WrongAction = "wrong_action";
        public const string UnknownTile = "unknown_tile";
        public const string FixedTile = "fixed_tile";
    }

    public sealed class FlowNetworkRotateAction : IPuzzleAction
    {
        public FlowNetworkRotateAction(string tileId)
        {
            TileId = tileId;
        }

        public string TileId { get; }
    }

    public sealed class FlowNetworkRunner : IPuzzleRunner
    {
        private readonly Dictionary<string, FlowNetworkTile> tilesById = new Dictionary<string, FlowNetworkTile>(StringComparer.Ordinal);
        private readonly Dictionary<GridCell, FlowNetworkTile> tilesByCell = new Dictionary<GridCell, FlowNetworkTile>();
        private readonly Dictionary<string, int> rotations = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> powered = new HashSet<string>(StringComparer.Ordinal);

        private FlowNetworkPayload payload;

        public string Variant => PrototypeVariant.FlowNetworkRestoration;
        public PrototypeLevelDefinition Level { get; private set; }
        public PuzzleRunStatus Status { get; private set; } = PuzzleRunStatus.NotLoaded;

        public void Load(PrototypeLevelDefinition level)
        {
            var errors = FlowNetworkLevelValidator.Validate(level);
            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join("; ", errors), nameof(level));
            }

            Level = level;
            payload = (FlowNetworkPayload)level.Payload;
            RebuildState();
            Status = PuzzleRunStatus.Ready;
        }

        public void Start()
        {
            if (Status != PuzzleRunStatus.Ready)
            {
                throw new InvalidOperationException("Flow / Network runner is not ready.");
            }

            Status = PuzzleRunStatus.Active;
            RecomputePower();
            if (IsSolved())
            {
                Status = PuzzleRunStatus.Succeeded;
            }
        }

        public PuzzleActionResult Apply(IPuzzleAction action)
        {
            if (Status != PuzzleRunStatus.Active)
            {
                return PuzzleActionResult.Reject(FlowNetworkInvalidReason.NotActive);
            }

            if (!(action is FlowNetworkRotateAction rotateAction))
            {
                return PuzzleActionResult.Reject(FlowNetworkInvalidReason.WrongAction);
            }

            if (string.IsNullOrWhiteSpace(rotateAction.TileId) || !tilesById.TryGetValue(rotateAction.TileId, out var tile))
            {
                return PuzzleActionResult.Reject(FlowNetworkInvalidReason.UnknownTile);
            }

            if (!tile.Rotatable)
            {
                return PuzzleActionResult.Reject(FlowNetworkInvalidReason.FixedTile);
            }

            rotations[tile.Id] = (rotations[tile.Id] + 1) % 4;
            RecomputePower();
            if (IsSolved())
            {
                Status = PuzzleRunStatus.Succeeded;
            }
            return PuzzleActionResult.Accept();
        }

        public void Restart()
        {
            if (Level == null)
            {
                throw new InvalidOperationException("No Flow / Network level is loaded.");
            }

            rotations.Clear();
            foreach (var tile in payload.Tiles)
            {
                rotations[tile.Id] = tile.InitialRotation;
            }
            Status = PuzzleRunStatus.Active;
            RecomputePower();
            if (IsSolved())
            {
                Status = PuzzleRunStatus.Succeeded;
            }
        }

        public int GetRotation(string tileId)
        {
            return tileId != null && rotations.TryGetValue(tileId, out var value) ? value : 0;
        }

        public int GetConnectorMask(string tileId)
        {
            if (tileId == null || !tilesById.TryGetValue(tileId, out var tile))
            {
                return 0;
            }
            return FlowNetworkDirection.RotateClockwise(tile.ConnectorMask, GetRotation(tileId));
        }

        public bool IsPowered(string tileId)
        {
            return tileId != null && powered.Contains(tileId);
        }

        private void RebuildState()
        {
            tilesById.Clear();
            tilesByCell.Clear();
            rotations.Clear();
            powered.Clear();
            foreach (var tile in payload.Tiles)
            {
                tilesById.Add(tile.Id, tile);
                tilesByCell.Add(tile.Cell, tile);
                rotations.Add(tile.Id, tile.InitialRotation);
            }
        }

        private void RecomputePower()
        {
            powered.Clear();
            var queue = new Queue<FlowNetworkTile>();
            foreach (var tile in payload.Tiles)
            {
                if (tile.Role == FlowNetworkRole.Source)
                {
                    powered.Add(tile.Id);
                    queue.Enqueue(tile);
                }
            }

            while (queue.Count > 0)
            {
                var tile = queue.Dequeue();
                var mask = GetConnectorMask(tile.Id);
                TryPowerNeighbor(tile, mask, FlowNetworkDirection.North, new GridCell(tile.Cell.X, tile.Cell.Y + 1), FlowNetworkDirection.South, queue);
                TryPowerNeighbor(tile, mask, FlowNetworkDirection.East, new GridCell(tile.Cell.X + 1, tile.Cell.Y), FlowNetworkDirection.West, queue);
                TryPowerNeighbor(tile, mask, FlowNetworkDirection.South, new GridCell(tile.Cell.X, tile.Cell.Y - 1), FlowNetworkDirection.North, queue);
                TryPowerNeighbor(tile, mask, FlowNetworkDirection.West, new GridCell(tile.Cell.X - 1, tile.Cell.Y), FlowNetworkDirection.East, queue);
            }
        }

        private void TryPowerNeighbor(
            FlowNetworkTile source,
            int sourceMask,
            int sourceDirection,
            GridCell neighborCell,
            int neighborDirection,
            Queue<FlowNetworkTile> queue)
        {
            if ((sourceMask & sourceDirection) == 0 || !tilesByCell.TryGetValue(neighborCell, out var neighbor))
            {
                return;
            }
            if ((GetConnectorMask(neighbor.Id) & neighborDirection) == 0 || powered.Contains(neighbor.Id))
            {
                return;
            }
            powered.Add(neighbor.Id);
            queue.Enqueue(neighbor);
        }

        private bool IsSolved()
        {
            foreach (var tile in payload.Tiles)
            {
                if (tile.Role == FlowNetworkRole.Target && !powered.Contains(tile.Id))
                {
                    return false;
                }
                if (tile.Role == FlowNetworkRole.Hazard && powered.Contains(tile.Id))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
