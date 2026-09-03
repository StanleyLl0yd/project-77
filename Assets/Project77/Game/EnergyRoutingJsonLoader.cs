using System;
using Project77.Puzzle;
using Project77.Puzzle.EnergyRouting;
using UnityEngine;

namespace Project77.Game
{
    public static class EnergyRoutingJsonLoader
    {
        [Serializable]
        private sealed class CellDto
        {
            public int x;
            public int y;
        }

        [Serializable]
        private sealed class PairDto
        {
            public string id;
            public CellDto start;
            public CellDto end;
        }

        [Serializable]
        private sealed class PayloadDto
        {
            public int width;
            public int height;
            public PairDto[] pairs;
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
            var resourcePath = $"Prototype/EnergyRouting/{levelId}";
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Energy Routing level resource not found: {resourcePath}.");
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
                throw new ArgumentException("Energy Routing JSON has no payload.", nameof(json));
            }

            var pairDtos = dto.payload.pairs ?? Array.Empty<PairDto>();
            var pairs = new EnergyRoutingPair[pairDtos.Length];
            for (var index = 0; index < pairDtos.Length; index++)
            {
                var pair = pairDtos[index];
                if (pair == null || pair.start == null || pair.end == null)
                {
                    throw new ArgumentException($"Energy Routing pair {index} is incomplete.", nameof(json));
                }

                pairs[index] = new EnergyRoutingPair(
                    pair.id,
                    new GridCell(pair.start.x, pair.start.y),
                    new GridCell(pair.end.x, pair.end.y));
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
                new EnergyRoutingPayload(dto.payload.width, dto.payload.height, pairs, blocked));

            var errors = EnergyRoutingLevelValidator.Validate(level);
            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join("; ", errors), nameof(json));
            }

            return level;
        }
    }
}
