using System;
using Project77.Puzzle;
using Project77.Puzzle.FlowNetworkRestoration;
using UnityEngine;

namespace Project77.Game
{
    public static class FlowNetworkJsonLoader
    {
        [Serializable]
        private sealed class TileDto
        {
            public string id;
            public int x;
            public int y;
            public string role;
            public int mask;
            public bool rotatable;
            public int initialRotation;
        }

        [Serializable]
        private sealed class PayloadDto
        {
            public int width;
            public int height;
            public TileDto[] tiles;
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
            var resourcePath = $"Prototype/FlowNetwork/{levelId}";
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Flow / Network level resource not found: {resourcePath}.");
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
                throw new ArgumentException("Flow / Network JSON has no payload.", nameof(json));
            }

            var tileDtos = dto.payload.tiles ?? Array.Empty<TileDto>();
            var tiles = new FlowNetworkTile[tileDtos.Length];
            for (var index = 0; index < tileDtos.Length; index++)
            {
                var tile = tileDtos[index];
                if (tile == null)
                {
                    throw new ArgumentException($"Flow / Network tile {index} is null.", nameof(json));
                }
                tiles[index] = new FlowNetworkTile(
                    tile.id,
                    new GridCell(tile.x, tile.y),
                    tile.role,
                    tile.mask,
                    tile.rotatable,
                    tile.initialRotation);
            }

            var level = new PrototypeLevelDefinition(
                dto.schemaVersion,
                dto.id,
                dto.revision,
                dto.variant,
                dto.difficultyTag,
                new FlowNetworkPayload(dto.payload.width, dto.payload.height, tiles));

            var errors = FlowNetworkLevelValidator.Validate(level);
            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join("; ", errors), nameof(json));
            }
            return level;
        }
    }
}
