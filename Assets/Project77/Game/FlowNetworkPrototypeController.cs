using System;
using System.Collections.Generic;
using Project77.Analytics;
using Project77.Puzzle;
using Project77.Puzzle.FlowNetworkRestoration;
using UnityEngine;

namespace Project77.Game
{
    public sealed class FlowNetworkPrototypeController : MonoBehaviour
    {
        private const int FirstLevel = 1;
        private const int LastLevel = 10;

        private readonly FlowNetworkRunner runner = new FlowNetworkRunner();
        private readonly InMemoryPrototypeAnalyticsSink analytics = new InMemoryPrototypeAnalyticsSink();

        private PrototypeAnalyticsContext analyticsContext;
        private FlowNetworkPayload payload;
        private int levelNumber = FirstLevel;
        private int attemptIndex = 1;
        private int validInteractionCount;
        private int invalidInteractionCount;
        private long levelStartMs;
        private long offerTimestampMs;
        private bool continuationOffered;
        private bool setComplete;
        private string feedback = "Tap rotatable relay tiles to restore the network.";

        private void Start()
        {
            analyticsContext = new PrototypeAnalyticsContext(
                Guid.NewGuid().ToString("N"),
                "local_debug",
                Application.version,
                PrototypeVariant.FlowNetworkRestoration,
                "unknown",
                PrototypeGuiLayout.Width >= PrototypeGuiLayout.Height ? "landscape" : "portrait");

            Track(
                PrototypeAnalyticsEventName.PrototypeStart,
                null,
                null,
                new Dictionary<string, object>
                {
                    ["entry_point"] = "variant_select",
                    ["core_variant"] = PrototypeVariant.FlowNetworkRestoration
                });
            LoadLevel(FirstLevel);
        }

        private void Update()
        {
            if (setComplete || continuationOffered || runner.Status != PuzzleRunStatus.Active)
            {
                return;
            }

            if (TryGetPointerDown(out var position))
            {
                HandleTap(position);
            }
        }

        private void OnGUI()
        {
            PrototypeGuiLayout.Begin();
            try
            {
                DrawHeader();
                if (setComplete)
                {
                    DrawSetComplete();
                    return;
                }
                DrawNetwork();
                DrawControls();
            }
            finally
            {
                PrototypeGuiLayout.End();
            }
        }

        private void LoadLevel(int number)
        {
            levelNumber = number;
            var level = FlowNetworkJsonLoader.LoadResource($"C-{number:000}");
            payload = (FlowNetworkPayload)level.Payload;
            runner.Load(level);
            runner.Start();
            attemptIndex = 1;
            validInteractionCount = 0;
            invalidInteractionCount = 0;
            continuationOffered = false;
            levelStartMs = NowMs();
            feedback = "Tap rotatable relay tiles. Power every target; keep hazards dark.";
            TrackLevelStart();
        }

        private void HandleTap(Vector2 screenPosition)
        {
            if (!TryScreenToCell(screenPosition, out var cell))
            {
                return;
            }

            var tile = FindTile(cell);
            if (tile == null)
            {
                invalidInteractionCount++;
                feedback = "Empty network cell.";
                TrackInvalid("tap", "wrong_target");
                return;
            }

            if (!tile.Rotatable)
            {
                invalidInteractionCount++;
                feedback = "That node is fixed.";
                TrackInvalid("tap", "wrong_target");
                return;
            }

            var result = runner.Apply(new FlowNetworkRotateAction(tile.Id));
            if (!result.Accepted)
            {
                invalidInteractionCount++;
                feedback = $"Rotation rejected: {result.Reason}";
                TrackInvalid("tap", "rule_violation");
                return;
            }

            validInteractionCount++;
            feedback = $"{tile.Id} rotated to state {runner.GetRotation(tile.Id)}.";
            if (runner.Status == PuzzleRunStatus.Succeeded)
            {
                feedback = "Network restored safely.";
                TrackLevelComplete();
                OfferContinuation();
            }
        }

        private void RestartLevel()
        {
            if (!PrototypeAttemptPolicy.CanRestartAttempt(runner.Status, continuationOffered))
            {
                return;
            }

            var previousAttempt = attemptIndex;
            attemptIndex++;
            runner.Restart();
            continuationOffered = false;
            validInteractionCount = 0;
            invalidInteractionCount = 0;
            levelStartMs = NowMs();
            feedback = "Network reset.";
            Track(
                PrototypeAnalyticsEventName.LevelRetry,
                runner.Level.Id,
                runner.Level.Revision,
                new Dictionary<string, object>
                {
                    ["previous_attempt_index"] = previousAttempt,
                    ["new_attempt_index"] = attemptIndex,
                    ["retry_source"] = "manual_restart"
                });
            TrackLevelStart();
        }

        private void OfferContinuation()
        {
            continuationOffered = true;
            offerTimestampMs = NowMs();
            Track(
                PrototypeAnalyticsEventName.NextPuzzleOffered,
                runner.Level.Id,
                runner.Level.Revision,
                new Dictionary<string, object>
                {
                    ["offer_context"] = "post_level",
                    ["next_level_id"] = levelNumber < LastLevel ? $"C-{levelNumber + 1:000}" : "C-END",
                    ["offer_sequence_index"] = levelNumber
                });
        }

        private void NextLevel()
        {
            if (!continuationOffered)
            {
                return;
            }

            Track(
                PrototypeAnalyticsEventName.NextPuzzleClicked,
                runner.Level.Id,
                runner.Level.Revision,
                new Dictionary<string, object>
                {
                    ["offer_context"] = "post_level",
                    ["next_level_id"] = levelNumber < LastLevel ? $"C-{levelNumber + 1:000}" : "C-END",
                    ["ms_since_offer"] = (int)Math.Max(0, NowMs() - offerTimestampMs),
                    ["offer_sequence_index"] = levelNumber
                });

            if (levelNumber >= LastLevel)
            {
                setComplete = true;
                return;
            }
            LoadLevel(levelNumber + 1);
        }

        private void TrackLevelStart()
        {
            Track(
                PrototypeAnalyticsEventName.LevelStart,
                runner.Level.Id,
                runner.Level.Revision,
                new Dictionary<string, object>
                {
                    ["attempt_index"] = attemptIndex,
                    ["core_variant"] = PrototypeVariant.FlowNetworkRestoration,
                    ["level_sequence_index"] = levelNumber
                });
        }

        private void TrackLevelComplete()
        {
            Track(
                PrototypeAnalyticsEventName.LevelComplete,
                runner.Level.Id,
                runner.Level.Revision,
                new Dictionary<string, object>
                {
                    ["attempt_index"] = attemptIndex,
                    ["duration_ms"] = (int)Math.Max(0, NowMs() - levelStartMs),
                    ["valid_interaction_count"] = validInteractionCount,
                    ["invalid_interaction_count"] = invalidInteractionCount,
                    ["core_variant"] = PrototypeVariant.FlowNetworkRestoration,
                    ["path_action_count"] = 0
                });
        }

        private void TrackInvalid(string interactionType, string invalidReason)
        {
            Track(
                PrototypeAnalyticsEventName.InvalidInteraction,
                runner.Level?.Id,
                runner.Level?.Revision,
                new Dictionary<string, object>
                {
                    ["interaction_type"] = interactionType,
                    ["invalid_reason"] = invalidReason,
                    ["attempt_index"] = attemptIndex
                });
        }

        private void Track(string eventName, string levelId, int? levelRevision, IReadOnlyDictionary<string, object> properties)
        {
            analytics.Track(new PrototypeAnalyticsEvent(eventName, NowMs(), analyticsContext, levelId, levelRevision, properties));
        }

        private void DrawHeader()
        {
            var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, wordWrap = true };
            var bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            GUI.Label(new Rect(20f, 12f, PrototypeGuiLayout.Width - 40f, 32f), "Project 77 — Prototype C: Flow / Network Restoration", titleStyle);
            GUI.Label(new Rect(20f, 46f, PrototypeGuiLayout.Width - 40f, 46f), setComplete ? "Initial 10-level set complete." : $"Level C-{levelNumber:000} · {feedback}", bodyStyle);
        }

        private void DrawNetwork()
        {
            var cellSize = GetCellSize();
            var boardRect = GetBoardRect(cellSize);
            foreach (var tile in payload.Tiles)
            {
                DrawTile(tile, boardRect, cellSize);
            }
        }

        private void DrawTile(FlowNetworkTile tile, Rect boardRect, float cellSize)
        {
            var rect = GetCellRect(tile.Cell, boardRect, cellSize);
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = TileColor(tile);
            GUI.Box(rect, RoleLabel(tile));
            GUI.backgroundColor = previous;

            var mask = runner.GetConnectorMask(tile.Id);
            var centerX = rect.x + rect.width * 0.5f;
            var centerY = rect.y + rect.height * 0.5f;
            var thickness = Mathf.Max(5f, cellSize * 0.1f);
            var half = rect.width * 0.42f;
            previous = GUI.backgroundColor;
            GUI.backgroundColor = runner.IsPowered(tile.Id) ? new Color(0.96f, 0.78f, 0.24f) : new Color(0.25f, 0.28f, 0.32f);
            if ((mask & FlowNetworkDirection.North) != 0)
                GUI.Box(new Rect(centerX - thickness * 0.5f, centerY - half, thickness, half), string.Empty);
            if ((mask & FlowNetworkDirection.East) != 0)
                GUI.Box(new Rect(centerX, centerY - thickness * 0.5f, half, thickness), string.Empty);
            if ((mask & FlowNetworkDirection.South) != 0)
                GUI.Box(new Rect(centerX - thickness * 0.5f, centerY, thickness, half), string.Empty);
            if ((mask & FlowNetworkDirection.West) != 0)
                GUI.Box(new Rect(centerX - half, centerY - thickness * 0.5f, half, thickness), string.Empty);
            GUI.backgroundColor = previous;
        }

        private void DrawControls()
        {
            var y = PrototypeGuiLayout.Height - 72f;
            if (PrototypeAttemptPolicy.CanRestartAttempt(runner.Status, continuationOffered) &&
                GUI.Button(new Rect(20f, y, 142f, 50f), "Reset"))
            {
                RestartLevel();
            }
            if (continuationOffered && GUI.Button(new Rect(PrototypeGuiLayout.Width - 172f, y, 152f, 50f), "Next level"))
            {
                NextLevel();
            }
        }

        private void DrawSetComplete()
        {
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, wordWrap = true };
            GUI.Label(new Rect(30f, 100f, PrototypeGuiLayout.Width - 60f, PrototypeGuiLayout.Height - 200f), "Prototype C initial set complete.\nThis is a greybox P0 comparison build.", style);
            if (GUI.Button(new Rect(PrototypeGuiLayout.Width * 0.5f - 90f, PrototypeGuiLayout.Height - 88f, 180f, 52f), "Restart set"))
            {
                setComplete = false;
                LoadLevel(FirstLevel);
            }
        }

        private float GetCellSize()
        {
            var widthFit = (PrototypeGuiLayout.Width - 40f) / payload.Width;
            var heightFit = (PrototypeGuiLayout.Height - 230f) / payload.Height;
            return Mathf.Clamp(Mathf.Min(widthFit, heightFit), 44f, 100f);
        }

        private Rect GetBoardRect(float cellSize)
        {
            var width = payload.Width * cellSize;
            var height = payload.Height * cellSize;
            return new Rect((PrototypeGuiLayout.Width - width) * 0.5f, 104f + (PrototypeGuiLayout.Height - 230f - height) * 0.5f, width, height);
        }

        private Rect GetCellRect(GridCell cell, Rect boardRect, float cellSize)
        {
            var guiRow = payload.Height - 1 - cell.Y;
            return new Rect(boardRect.x + cell.X * cellSize + 2f, boardRect.y + guiRow * cellSize + 2f, cellSize - 4f, cellSize - 4f);
        }

        private bool TryScreenToCell(Vector2 screenPosition, out GridCell cell)
        {
            var cellSize = GetCellSize();
            var boardRect = GetBoardRect(cellSize);
            var guiPosition = PrototypeGuiLayout.ScreenToGui(screenPosition);
            if (!boardRect.Contains(guiPosition))
            {
                cell = default;
                return false;
            }
            var x = Mathf.FloorToInt((guiPosition.x - boardRect.x) / cellSize);
            var guiRow = Mathf.FloorToInt((guiPosition.y - boardRect.y) / cellSize);
            var y = payload.Height - 1 - guiRow;
            cell = new GridCell(x, y);
            return payload.Contains(cell);
        }

        private FlowNetworkTile FindTile(GridCell cell)
        {
            foreach (var tile in payload.Tiles)
            {
                if (tile.Cell.Equals(cell)) return tile;
            }
            return null;
        }

        private Color TileColor(FlowNetworkTile tile)
        {
            if (tile.Role == FlowNetworkRole.Hazard)
                return runner.IsPowered(tile.Id) ? new Color(0.82f, 0.2f, 0.18f) : new Color(0.35f, 0.18f, 0.18f);
            if (tile.Role == FlowNetworkRole.Target)
                return runner.IsPowered(tile.Id) ? new Color(0.28f, 0.7f, 0.38f) : new Color(0.22f, 0.35f, 0.25f);
            if (tile.Role == FlowNetworkRole.Source)
                return new Color(0.85f, 0.64f, 0.18f);
            return tile.Rotatable ? new Color(0.34f, 0.43f, 0.58f) : new Color(0.3f, 0.32f, 0.36f);
        }

        private static string RoleLabel(FlowNetworkTile tile)
        {
            if (tile.Role == FlowNetworkRole.Source) return "S";
            if (tile.Role == FlowNetworkRole.Target) return "T";
            if (tile.Role == FlowNetworkRole.Hazard) return "!";
            return tile.Rotatable ? "↻" : string.Empty;
        }

        private static bool TryGetPointerDown(out Vector2 position)
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                position = touch.position;
                return touch.phase == TouchPhase.Began;
            }
            position = Input.mousePosition;
            return Input.GetMouseButtonDown(0);
        }

        private static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
