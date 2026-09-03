using System;
using System.Collections.Generic;

namespace Project77.Puzzle.PathExpeditionRouting
{
    public static class PathExpeditionInvalidReason
    {
        public const string NotActive = "not_active";
        public const string WrongAction = "wrong_action";
        public const string UnknownAgent = "unknown_agent";
        public const string BadEndpoint = "bad_endpoint";
        public const string OutOfBounds = "out_of_bounds";
        public const string Blocked = "blocked";
        public const string NonContiguous = "non_contiguous";
        public const string SelfIntersection = "self_intersection";
        public const string WrongTarget = "wrong_target";
        public const string IncompletePlan = "incomplete_plan";
    }

    public static class PathExpeditionFailReason
    {
        public const string SameCellCollision = "collision_same_cell";
        public const string SwapCollision = "collision_swap";
    }

    public sealed class PathExpeditionSetRouteAction : IPuzzleAction
    {
        public PathExpeditionSetRouteAction(string agentId, IReadOnlyList<GridCell> route)
        {
            AgentId = agentId;
            Route = route ?? Array.Empty<GridCell>();
        }

        public string AgentId { get; }
        public IReadOnlyList<GridCell> Route { get; }
    }

    public sealed class PathExpeditionCommitAction : IPuzzleAction
    {
    }

    public sealed class PathExpeditionRunner : IPuzzleRunner
    {
        private static readonly IReadOnlyList<GridCell> EmptyRoute = Array.Empty<GridCell>();

        private readonly Dictionary<string, PathExpeditionAgent> agents =
            new Dictionary<string, PathExpeditionAgent>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<GridCell>> routes =
            new Dictionary<string, List<GridCell>>(StringComparer.Ordinal);
        private readonly HashSet<GridCell> blocked = new HashSet<GridCell>();
        private readonly Dictionary<GridCell, string> endpointOwners = new Dictionary<GridCell, string>();

        private PathExpeditionPayload payload;

        public string Variant => PrototypeVariant.PathExpeditionRouting;
        public PrototypeLevelDefinition Level { get; private set; }
        public PuzzleRunStatus Status { get; private set; } = PuzzleRunStatus.NotLoaded;
        public string LastFailReason { get; private set; } = string.Empty;

        public void Load(PrototypeLevelDefinition level)
        {
            var errors = PathExpeditionLevelValidator.Validate(level);
            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join("; ", errors), nameof(level));
            }

            Level = level;
            payload = (PathExpeditionPayload)level.Payload;
            RebuildStaticState();
            routes.Clear();
            LastFailReason = string.Empty;
            Status = PuzzleRunStatus.Ready;
        }

        public void Start()
        {
            if (Status != PuzzleRunStatus.Ready)
            {
                throw new InvalidOperationException("Path / Expedition runner is not ready.");
            }

            Status = PuzzleRunStatus.Active;
        }

        public PuzzleActionResult Apply(IPuzzleAction action)
        {
            if (Status != PuzzleRunStatus.Active)
            {
                return PuzzleActionResult.Reject(PathExpeditionInvalidReason.NotActive);
            }

            if (action is PathExpeditionSetRouteAction setRoute)
            {
                return ApplyRoute(setRoute);
            }

            if (action is PathExpeditionCommitAction)
            {
                return CommitPlan();
            }

            return PuzzleActionResult.Reject(PathExpeditionInvalidReason.WrongAction);
        }

        public void Restart()
        {
            if (Level == null)
            {
                throw new InvalidOperationException("No Path / Expedition level is loaded.");
            }

            routes.Clear();
            LastFailReason = string.Empty;
            Status = PuzzleRunStatus.Active;
        }

        public IReadOnlyList<GridCell> GetRoute(string agentId)
        {
            return agentId != null && routes.TryGetValue(agentId, out var route) ? route : EmptyRoute;
        }

        private PuzzleActionResult ApplyRoute(PathExpeditionSetRouteAction action)
        {
            if (string.IsNullOrWhiteSpace(action.AgentId) || !agents.TryGetValue(action.AgentId, out var agent))
            {
                return PuzzleActionResult.Reject(PathExpeditionInvalidReason.UnknownAgent);
            }

            var validation = ValidateRoute(agent, action.Route);
            if (!validation.Accepted)
            {
                return validation;
            }

            routes[agent.Id] = new List<GridCell>(action.Route);
            return PuzzleActionResult.Accept();
        }

        private PuzzleActionResult CommitPlan()
        {
            if (routes.Count != agents.Count)
            {
                return PuzzleActionResult.Reject(PathExpeditionInvalidReason.IncompletePlan);
            }

            var maxSteps = 0;
            foreach (var route in routes.Values)
            {
                maxSteps = Math.Max(maxSteps, route.Count);
            }

            for (var step = 0; step < maxSteps; step++)
            {
                var positions = new Dictionary<GridCell, string>();
                foreach (var entry in routes)
                {
                    var position = PositionAt(entry.Value, step);
                    if (positions.TryGetValue(position, out _))
                    {
                        LastFailReason = PathExpeditionFailReason.SameCellCollision;
                        Status = PuzzleRunStatus.Failed;
                        return PuzzleActionResult.Accept();
                    }
                    positions.Add(position, entry.Key);
                }

                if (step == 0)
                {
                    continue;
                }

                foreach (var first in routes)
                {
                    var firstPrevious = PositionAt(first.Value, step - 1);
                    var firstCurrent = PositionAt(first.Value, step);
                    foreach (var second in routes)
                    {
                        if (string.CompareOrdinal(first.Key, second.Key) >= 0)
                        {
                            continue;
                        }

                        var secondPrevious = PositionAt(second.Value, step - 1);
                        var secondCurrent = PositionAt(second.Value, step);
                        if (firstPrevious.Equals(secondCurrent) && secondPrevious.Equals(firstCurrent))
                        {
                            LastFailReason = PathExpeditionFailReason.SwapCollision;
                            Status = PuzzleRunStatus.Failed;
                            return PuzzleActionResult.Accept();
                        }
                    }
                }
            }

            LastFailReason = string.Empty;
            Status = PuzzleRunStatus.Succeeded;
            return PuzzleActionResult.Accept();
        }

        private PuzzleActionResult ValidateRoute(PathExpeditionAgent agent, IReadOnlyList<GridCell> route)
        {
            if (route == null || route.Count < 2 || !route[0].Equals(agent.Start) || !route[route.Count - 1].Equals(agent.Goal))
            {
                return PuzzleActionResult.Reject(PathExpeditionInvalidReason.BadEndpoint);
            }

            var visited = new HashSet<GridCell> { route[0] };
            for (var index = 0; index < route.Count; index++)
            {
                var cell = route[index];
                if (!payload.Contains(cell))
                {
                    return PuzzleActionResult.Reject(PathExpeditionInvalidReason.OutOfBounds);
                }
                if (blocked.Contains(cell))
                {
                    return PuzzleActionResult.Reject(PathExpeditionInvalidReason.Blocked);
                }

                if (endpointOwners.TryGetValue(cell, out var owner) && owner != agent.Id)
                {
                    return PuzzleActionResult.Reject(PathExpeditionInvalidReason.WrongTarget);
                }

                if (index == 0)
                {
                    continue;
                }

                var previous = route[index - 1];
                if (!cell.Equals(previous) && !previous.IsOrthogonallyAdjacentTo(cell))
                {
                    return PuzzleActionResult.Reject(PathExpeditionInvalidReason.NonContiguous);
                }

                if (!cell.Equals(previous) && !visited.Add(cell))
                {
                    return PuzzleActionResult.Reject(PathExpeditionInvalidReason.SelfIntersection);
                }
            }

            return PuzzleActionResult.Accept();
        }

        private void RebuildStaticState()
        {
            agents.Clear();
            blocked.Clear();
            endpointOwners.Clear();
            foreach (var agent in payload.Agents)
            {
                agents.Add(agent.Id, agent);
                endpointOwners.Add(agent.Start, agent.Id);
                endpointOwners.Add(agent.Goal, agent.Id);
            }
            foreach (var cell in payload.BlockedCells)
            {
                blocked.Add(cell);
            }
        }

        private static GridCell PositionAt(IReadOnlyList<GridCell> route, int step)
        {
            return route[Math.Min(step, route.Count - 1)];
        }
    }
}
