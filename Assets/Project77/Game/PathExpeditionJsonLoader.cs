using System;
using Project77.Puzzle;
using Project77.Puzzle.PathExpeditionRouting;
using UnityEngine;

namespace Project77.Game
{
    public static class PathExpeditionJsonLoader
    {
        [Serializable]
        private sealed class CellDto
        {
            public int x;
            public int y;
        }

        [Serializable]
        private sealed class AgentDto
        {
            public string id;
            public CellDto start;
            public CellDto goal;
        }

        [Serializable]
        private sealed class PayloadDto
        {
            public int width;
            public int height;
            public AgentDto[] agents;
            public CellDto[] blocked;
        }

        [Serializable]
        private sealed class LevelDto
        {
            public int schemaVersion;
            public string id;
            public int revision;
            public string variant;
            public string difficultyTag;
            public PayloadDto payload;
        }

        public static PrototypeLevelDefinition LoadResource(string levelId)
        {
            var resourcePath = $"Prototype/PathExpedition/{levelId}";
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Path / Expedition level resource not found: {resourcePath}.");
            }

            return Parse(asset.text);
        }

        public static PrototypeLevelDefinition Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Level JSON is empty.", nameof(json));
            }

            var dto = JsonUtility.FromJson<LevelDto>(json);
            if (dto == null || dto.payload == null)
            {
                throw new ArgumentException("Path / Expedition JSON has no payload.", nameof(json));
            }

            var agentDtos = dto.payload.agents ?? Array.Empty<AgentDto>();
            var agents = new PathExpeditionAgent[agentDtos.Length];
            for (var index = 0; index < agentDtos.Length; index++)
            {
                var agent = agentDtos[index];
                if (agent == null || agent.start == null || agent.goal == null)
                {
                    throw new ArgumentException($"Path / Expedition agent {index} is incomplete.", nameof(json));
                }

                agents[index] = new PathExpeditionAgent(
                    agent.id,
                    new GridCell(agent.start.x, agent.start.y),
                    new GridCell(agent.goal.x, agent.goal.y));
            }

            var blockedDtos = dto.payload.blocked ?? Array.Empty<CellDto>();
            var blocked = new GridCell[blockedDtos.Length];
            for (var index = 0; index < blockedDtos.Length; index++)
            {
                var cell = blockedDtos[index];
                if (cell == null)
                {
                    throw new ArgumentException($"Blocked cell {index} is null.", nameof(json));
                }

                blocked[index] = new GridCell(cell.x, cell.y);
            }

            var level = new PrototypeLevelDefinition(
                dto.schemaVersion,
                dto.id,
                dto.revision,
                dto.variant,
                dto.difficultyTag,
                new PathExpeditionPayload(dto.payload.width, dto.payload.height, agents, blocked));

            var errors = PathExpeditionLevelValidator.Validate(level);
            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join("; ", errors), nameof(json));
            }

            return level;
        }
    }
}
