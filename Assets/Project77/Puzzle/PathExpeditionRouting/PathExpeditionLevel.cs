using System;
using System.Collections.Generic;

namespace Project77.Puzzle.PathExpeditionRouting
{
    public sealed class PathExpeditionAgent
    {
        public PathExpeditionAgent(string id, GridCell start, GridCell goal)
        {
            Id = id;
            Start = start;
            Goal = goal;
        }

        public string Id { get; }
        public GridCell Start { get; }
        public GridCell Goal { get; }
    }

    public sealed class PathExpeditionPayload : IPrototypeLevelPayload
    {
        public PathExpeditionPayload(
            int width,
            int height,
            IReadOnlyList<PathExpeditionAgent> agents,
            IReadOnlyList<GridCell> blockedCells)
        {
            Width = width;
            Height = height;
            Agents = agents ?? Array.Empty<PathExpeditionAgent>();
            BlockedCells = blockedCells ?? Array.Empty<GridCell>();
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<PathExpeditionAgent> Agents { get; }
        public IReadOnlyList<GridCell> BlockedCells { get; }

        public bool Contains(GridCell cell)
        {
            return cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
        }
    }

    public static class PathExpeditionLevelValidator
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

            if (level.Variant != PrototypeVariant.PathExpeditionRouting)
            {
                errors.Add("Path / Expedition level must use path_expedition_routing variant.");
            }

            if (!(level.Payload is PathExpeditionPayload payload))
            {
                errors.Add("Path / Expedition level requires PathExpeditionPayload.");
                return errors;
            }

            if (payload.Width < 2 || payload.Width > 12 || payload.Height < 2 || payload.Height > 12)
            {
                errors.Add("Board dimensions must be between 2 and 12 cells per axis for Prototype B.");
            }

            if (payload.Agents.Count == 0)
            {
                errors.Add("At least one expedition agent is required.");
            }

            var agentIds = new HashSet<string>(StringComparer.Ordinal);
            var endpoints = new HashSet<GridCell>();
            foreach (var agent in payload.Agents)
            {
                if (agent == null)
                {
                    errors.Add("Expedition agent cannot be null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(agent.Id))
                {
                    errors.Add("Expedition agent id is required.");
                }
                else if (!agentIds.Add(agent.Id))
                {
                    errors.Add($"Duplicate expedition agent id: '{agent.Id}'.");
                }

                if (!payload.Contains(agent.Start) || !payload.Contains(agent.Goal))
                {
                    errors.Add($"Agent '{agent.Id}' has start/goal outside the board.");
                }

                if (agent.Start.Equals(agent.Goal))
                {
                    errors.Add($"Agent '{agent.Id}' start and goal must differ.");
                }

                if (!endpoints.Add(agent.Start) || !endpoints.Add(agent.Goal))
                {
                    errors.Add($"Agent '{agent.Id}' reuses another start/goal cell.");
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
                    errors.Add($"Blocked cell {cell} overlaps an agent start/goal.");
                }
            }

            return errors;
        }
    }
}
