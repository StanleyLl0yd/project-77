using System;
using System.Collections.Generic;

namespace Project77.Puzzle.EnergyRouting
{
    public static class EnergyRoutingInvalidReason
    {
        public const string NotActive = "not_active";
        public const string WrongAction = "wrong_action";
        public const string UnknownPair = "unknown_pair";
        public const string BadEndpoint = "bad_endpoint";
        public const string OutOfBounds = "out_of_bounds";
        public const string Blocked = "blocked";
        public const string NonContiguous = "non_contiguous";
        public const string SelfIntersection = "self_intersection";
        public const string WrongTarget = "wrong_target";
        public const string Crossing = "crossing";
    }

    public sealed class EnergyRoutingPathAction : IPuzzleAction
    {
        public EnergyRoutingPathAction(string pairId, IReadOnlyList<GridCell> path)
        {
            PairId = pairId;
            Path = path ?? Array.Empty<GridCell>();
        }

        public string PairId { get; }
        public IReadOnlyList<GridCell> Path { get; }
    }

    public sealed class EnergyRoutingRunner : IPuzzleRunner
    {
        private static readonly IReadOnlyList<GridCell> EmptyPath = Array.Empty<GridCell>();

        private readonly Dictionary<string, EnergyRoutingPair> pairs =
            new Dictionary<string, EnergyRoutingPair>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<GridCell>> paths =
            new Dictionary<string, List<GridCell>>(StringComparer.Ordinal);
        private readonly HashSet<GridCell> blocked = new HashSet<GridCell>();
        private readonly Dictionary<GridCell, string> endpointOwners = new Dictionary<GridCell, string>();

        private EnergyRoutingPayload payload;

        public string Variant => PrototypeVariant.EnergyRouting;
        public PrototypeLevelDefinition Level { get; private set; }
        public PuzzleRunStatus Status { get; private set; } = PuzzleRunStatus.NotLoaded;

        public void Load(PrototypeLevelDefinition level)
        {
            var errors = EnergyRoutingLevelValidator.Validate(level);
            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join("; ", errors), nameof(level));
            }

            Level = level;
            payload = (EnergyRoutingPayload)level.Payload;
            RebuildStaticState();
            paths.Clear();
            Status = PuzzleRunStatus.Ready;
        }

        public void Start()
        {
            if (Status != PuzzleRunStatus.Ready)
            {
                throw new InvalidOperationException("Energy Routing runner is not ready.");
            }

            Status = PuzzleRunStatus.Active;
        }

        public PuzzleActionResult Apply(IPuzzleAction action)
        {
            if (Status != PuzzleRunStatus.Active)
            {
                return PuzzleActionResult.Reject(EnergyRoutingInvalidReason.NotActive);
            }

            if (!(action is EnergyRoutingPathAction pathAction))
            {
                return PuzzleActionResult.Reject(EnergyRoutingInvalidReason.WrongAction);
            }

            if (string.IsNullOrWhiteSpace(pathAction.PairId) || !pairs.TryGetValue(pathAction.PairId, out var pair))
            {
                return PuzzleActionResult.Reject(EnergyRoutingInvalidReason.UnknownPair);
            }

            var validation = ValidatePath(pair, pathAction.Path);
            if (!validation.Accepted)
            {
                return validation;
            }

            paths[pair.Id] = new List<GridCell>(pathAction.Path);
            if (paths.Count == pairs.Count)
            {
                Status = PuzzleRunStatus.Succeeded;
            }

            return PuzzleActionResult.Accept();
        }

        public void Restart()
        {
            if (Level == null)
            {
                throw new InvalidOperationException("No Energy Routing level is loaded.");
            }

            paths.Clear();
            Status = PuzzleRunStatus.Active;
        }

        public IReadOnlyList<GridCell> GetPath(string pairId)
        {
            return pairId != null && paths.TryGetValue(pairId, out var path) ? path : EmptyPath;
        }

        private void RebuildStaticState()
        {
            pairs.Clear();
            blocked.Clear();
            endpointOwners.Clear();

            foreach (var pair in payload.Pairs)
            {
                pairs.Add(pair.Id, pair);
                endpointOwners.Add(pair.Start, pair.Id);
                endpointOwners.Add(pair.End, pair.Id);
            }

            foreach (var cell in payload.BlockedCells)
            {
                blocked.Add(cell);
            }
        }

        private PuzzleActionResult ValidatePath(EnergyRoutingPair pair, IReadOnlyList<GridCell> path)
        {
            if (path == null || path.Count < 2)
            {
                return PuzzleActionResult.Reject(EnergyRoutingInvalidReason.BadEndpoint);
            }

            var first = path[0];
            var last = path[path.Count - 1];
            var forward = first.Equals(pair.Start) && last.Equals(pair.End);
            var reverse = first.Equals(pair.End) && last.Equals(pair.Start);
            if (!forward && !reverse)
            {
                return PuzzleActionResult.Reject(EnergyRoutingInvalidReason.BadEndpoint);
            }

            var ownCells = new HashSet<GridCell>();
            for (var index = 0; index < path.Count; index++)
            {
                var cell = path[index];
                if (!payload.Contains(cell))
                {
                    return PuzzleActionResult.Reject(EnergyRoutingInvalidReason.OutOfBounds);
                }

                if (blocked.Contains(cell))
                {
                    return PuzzleActionResult.Reject(EnergyRoutingInvalidReason.Blocked);
                }

                if (!ownCells.Add(cell))
                {
                    return PuzzleActionResult.Reject(EnergyRoutingInvalidReason.SelfIntersection);
                }

                if (index > 0 && !path[index - 1].IsOrthogonallyAdjacentTo(cell))
                {
                    return PuzzleActionResult.Reject(EnergyRoutingInvalidReason.NonContiguous);
                }

                if (endpointOwners.TryGetValue(cell, out var owner))
                {
                    var isPathEnd = index == 0 || index == path.Count - 1;
                    if (owner != pair.Id || !isPathEnd)
                    {
                        return PuzzleActionResult.Reject(EnergyRoutingInvalidReason.WrongTarget);
                    }
                }

                if (IsOccupiedByAnotherPath(pair.Id, cell))
                {
                    return PuzzleActionResult.Reject(EnergyRoutingInvalidReason.Crossing);
                }
            }

            return PuzzleActionResult.Accept();
        }

        private bool IsOccupiedByAnotherPath(string pairId, GridCell cell)
        {
            foreach (var entry in paths)
            {
                if (entry.Key == pairId)
                {
                    continue;
                }

                if (entry.Value.Contains(cell))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
