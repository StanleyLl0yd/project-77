using System;
using System.Collections.Generic;
using Project77.Analytics;
using Project77.Puzzle;
using Project77.Puzzle.EnergyRouting;
using UnityEngine;

namespace Project77.Game
{
    public sealed class EnergyRoutingPrototypeController : MonoBehaviour
    {
        private const int FirstLevel = 1;
        private const int LastLevel = 10;

        private readonly EnergyRoutingRunner runner = new EnergyRoutingRunner();
        private readonly List<GridCell> dragPath = new List<GridCell>();
        private readonly InMemoryPrototypeAnalyticsSink analytics = new InMemoryPrototypeAnalyticsSink();

        private PrototypeAnalyticsContext analyticsContext;
        private EnergyRoutingPayload payload;
        private int levelNumber = FirstLevel;
        private int attemptIndex = 1;
        private int validInteractionCount;
        private int invalidInteractionCount;
        private string activePairId;
        private string feedback = "Connect matching nodes without crossing paths.";
        private long levelStartMs;
        private long offerTimestampMs;
        private bool continuationOffered;
        private bool setComplete;

        private void Start()
        {
            analyticsContext = new PrototypeAnalyticsContext(
                Guid.NewGuid().ToString("N"),
                "local_debug",
                Application.version,
                PrototypeVariant.EnergyRouting,
                "unknown",
                Screen.width >= Screen.height ? "landscape" : "portrait");

            Track(
                PrototypeAnalyticsEventName.PrototypeStart,
                null,
                null,
                new Dictionary<string, object>
                {
                    ["entry_point"] = "fresh_launch",
                    ["core_variant"] = PrototypeVariant.EnergyRouting
                });

            LoadLevel(FirstLevel);
        }

        private void Update()
        {
            if (setComplete || continuationOffered || runner.Status != PuzzleRunStatus.Active)
            {
                return;
            }

            if (TryGetPointerDown(out var downPosition))
            {
                BeginDrag(downPosition);
            }

            if (activePairId != null && TryGetPointerPosition(out var currentPosition))
            {
                ContinueDrag(currentPosition);
            }

            if (activePairId != null && TryGetPointerUp(out var upPosition))
            {
                ContinueDrag(upPosition);
                EndDrag();
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            if (setComplete)
            {
                DrawSetComplete();
                return;
            }

            DrawBoard();
            DrawControls();
        }

        private void LoadLevel(int number)
        {
            levelNumber = number;
            var levelId = $"A-{number:000}";
            var level = EnergyRoutingJsonLoader.LoadResource(levelId);
            payload = (EnergyRoutingPayload)level.Payload;
            runner.Load(level);
            runner.Start();
            attemptIndex = 1;
            validInteractionCount = 0;
            invalidInteractionCount = 0;
            activePairId = null;
            dragPath.Clear();
            continuationOffered = false;
            levelStartMs = NowMs();
            feedback = "Connect matching nodes without crossing paths.";

            Track(
                PrototypeAnalyticsEventName.LevelStart,
                level.Id,
                level.Revision,
                new Dictionary<string, object>
                {
                    ["attempt_index"] = attemptIndex,
                    ["core_variant"] = PrototypeVariant.EnergyRouting,
                    ["level_sequence_index"] = levelNumber
                });
        }

        private void BeginDrag(Vector2 screenPosition)
        {
            if (!TryScreenToCell(screenPosition, out var cell))
            {
                return;
            }

            var pairId = FindPairAtEndpoint(cell);
            if (pairId == null)
            {
                invalidInteractionCount++;
                feedback = "Start on a colored endpoint.";
                TrackInvalid("path_start", "wrong_target");
                return;
            }

            activePairId = pairId;
            dragPath.Clear();
            dragPath.Add(cell);
            feedback = $"Routing {pairId}…";
        }

        private void ContinueDrag(Vector2 screenPosition)
        {
            if (!TryScreenToCell(screenPosition, out var cell) || dragPath.Count == 0)
            {
                return;
            }

            var last = dragPath[dragPath.Count - 1];
            if (cell.Equals(last))
            {
                return;
            }

            if (dragPath.Count >= 2 && cell.Equals(dragPath[dragPath.Count - 2]))
            {
                dragPath.RemoveAt(dragPath.Count - 1);
                return;
            }

            if (last.IsOrthogonallyAdjacentTo(cell) && !dragPath.Contains(cell))
            {
                dragPath.Add(cell);
            }
        }

        private void EndDrag()
        {
            var pairId = activePairId;
            activePairId = null;
            var result = runner.Apply(new EnergyRoutingPathAction(pairId, dragPath.ToArray()));
            dragPath.Clear();

            if (!result.Accepted)
            {
                invalidInteractionCount++;
                feedback = $"Invalid route: {result.Reason}";
                TrackInvalid("path_end", MapInvalidReason(result.Reason));
                return;
            }

            validInteractionCount++;
            feedback = runner.Status == PuzzleRunStatus.Succeeded ? "Network restored." : "Route accepted.";
            if (runner.Status == PuzzleRunStatus.Succeeded)
            {
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
            activePairId = null;
            dragPath.Clear();
            continuationOffered = false;
            validInteractionCount = 0;
            invalidInteractionCount = 0;
            levelStartMs = NowMs();
            feedback = "Level restarted.";

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
            Track(
                PrototypeAnalyticsEventName.LevelStart,
                runner.Level.Id,
                runner.Level.Revision,
                new Dictionary<string, object>
                {
                    ["attempt_index"] = attemptIndex,
                    ["core_variant"] = PrototypeVariant.EnergyRouting,
                    ["level_sequence_index"] = levelNumber
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
                    ["next_level_id"] = levelNumber < LastLevel ? $"A-{levelNumber + 1:000}" : "A-END",
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
                    ["next_level_id"] = levelNumber < LastLevel ? $"A-{levelNumber + 1:000}" : "A-END",
                    ["offer_sequence_index"] = levelNumber
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
                    ["core_variant"] = PrototypeVariant.EnergyRouting,
                    ["path_action_count"] = validInteractionCount + invalidInteractionCount
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

        private void Track(
            string eventName,
            string levelId,
            int? levelRevision,
            IReadOnlyDictionary<string, object> properties)
        {
            analytics.Track(new PrototypeAnalyticsEvent(
                eventName,
                NowMs(),
                analyticsContext,
                levelId,
                levelRevision,
                properties));
        }

        private void DrawHeader()
        {
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            var bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14
            };

            GUI.Label(new Rect(20f, 14f, Screen.width - 40f, 34f), "Project 77 — Prototype A: Energy Routing", titleStyle);
            GUI.Label(
                new Rect(20f, 48f, Screen.width - 40f, 26f),
                setComplete ? "Initial 10-level set complete." : $"Level A-{levelNumber:000} · {feedback}",
                bodyStyle);
        }

        private void DrawBoard()
        {
            var cellSize = GetCellSize();
            var boardRect = GetBoardRect(cellSize);
            for (var y = 0; y < payload.Height; y++)
            {
                for (var x = 0; x < payload.Width; x++)
                {
                    var cell = new GridCell(x, y);
                    var rect = GetCellRect(cell, boardRect, cellSize);
                    var previous = GUI.backgroundColor;
                    GUI.backgroundColor = IsBlocked(cell) ? new Color(0.22f, 0.22f, 0.24f) : new Color(0.48f, 0.5f, 0.54f);
                    GUI.Box(rect, string.Empty);
                    GUI.backgroundColor = previous;
                }
            }

            foreach (var pair in payload.Pairs)
            {
                DrawPath(pair.Id, runner.GetPath(pair.Id), boardRect, cellSize, 0.72f);
            }

            if (activePairId != null)
            {
                DrawPath(activePairId, dragPath, boardRect, cellSize, 0.48f);
            }

            foreach (var pair in payload.Pairs)
            {
                DrawEndpoint(pair.Id, pair.Start, boardRect, cellSize);
                DrawEndpoint(pair.Id, pair.End, boardRect, cellSize);
            }
        }

        private void DrawControls()
        {
            var y = Screen.height - 64f;
            if (PrototypeAttemptPolicy.CanRestartAttempt(runner.Status, continuationOffered) &&
                GUI.Button(new Rect(20f, y, 130f, 40f), "Restart"))
            {
                RestartLevel();
            }

            if (continuationOffered && GUI.Button(new Rect(Screen.width - 170f, y, 150f, 40f), "Next level"))
            {
                NextLevel();
            }
        }

        private void DrawSetComplete()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                wordWrap = true
            };
            GUI.Label(
                new Rect(30f, 100f, Screen.width - 60f, Screen.height - 200f),
                "Prototype A initial set complete.\nThis is a greybox build for P0 comparison, not production gameplay.",
                style);

            if (GUI.Button(new Rect(Screen.width * 0.5f - 90f, Screen.height - 80f, 180f, 44f), "Restart set"))
            {
                setComplete = false;
                LoadLevel(FirstLevel);
            }
        }

        private void DrawPath(string pairId, IReadOnlyList<GridCell> path, Rect boardRect, float cellSize, float insetFactor)
        {
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = PairColor(pairId);
            var inset = cellSize * (1f - insetFactor) * 0.5f;
            for (var index = 0; index < path.Count; index++)
            {
                var rect = GetCellRect(path[index], boardRect, cellSize);
                rect.x += inset;
                rect.y += inset;
                rect.width -= inset * 2f;
                rect.height -= inset * 2f;
                GUI.Box(rect, string.Empty);
            }
            GUI.backgroundColor = previous;
        }

        private void DrawEndpoint(string pairId, GridCell cell, Rect boardRect, float cellSize)
        {
            var rect = GetCellRect(cell, boardRect, cellSize);
            var inset = cellSize * 0.2f;
            rect.x += inset;
            rect.y += inset;
            rect.width -= inset * 2f;
            rect.height -= inset * 2f;

            var previous = GUI.backgroundColor;
            GUI.backgroundColor = PairColor(pairId);
            GUI.Box(rect, pairId.Substring(0, 1).ToUpperInvariant());
            GUI.backgroundColor = previous;
        }

        private float GetCellSize()
        {
            var widthFit = (Screen.width - 40f) / payload.Width;
            var heightFit = (Screen.height - 180f) / payload.Height;
            return Mathf.Clamp(Mathf.Min(widthFit, heightFit), 36f, 86f);
        }

        private Rect GetBoardRect(float cellSize)
        {
            var width = payload.Width * cellSize;
            var height = payload.Height * cellSize;
            return new Rect((Screen.width - width) * 0.5f, 88f + (Screen.height - 180f - height) * 0.5f, width, height);
        }

        private Rect GetCellRect(GridCell cell, Rect boardRect, float cellSize)
        {
            var guiRow = payload.Height - 1 - cell.Y;
            return new Rect(
                boardRect.x + cell.X * cellSize,
                boardRect.y + guiRow * cellSize,
                cellSize - 2f,
                cellSize - 2f);
        }

        private bool TryScreenToCell(Vector2 screenPosition, out GridCell cell)
        {
            var cellSize = GetCellSize();
            var boardRect = GetBoardRect(cellSize);
            var guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
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

        private string FindPairAtEndpoint(GridCell cell)
        {
            foreach (var pair in payload.Pairs)
            {
                if (pair.Start.Equals(cell) || pair.End.Equals(cell))
                {
                    return pair.Id;
                }
            }

            return null;
        }

        private bool IsBlocked(GridCell cell)
        {
            foreach (var blocked in payload.BlockedCells)
            {
                if (blocked.Equals(cell))
                {
                    return true;
                }
            }

            return false;
        }

        private static string MapInvalidReason(string reason)
        {
            if (reason == EnergyRoutingInvalidReason.OutOfBounds)
            {
                return "out_of_bounds";
            }
            if (reason == EnergyRoutingInvalidReason.Blocked)
            {
                return "blocked";
            }
            if (reason == EnergyRoutingInvalidReason.BadEndpoint ||
                reason == EnergyRoutingInvalidReason.UnknownPair ||
                reason == EnergyRoutingInvalidReason.WrongTarget)
            {
                return "wrong_target";
            }
            return "rule_violation";
        }

        private static Color PairColor(string pairId)
        {
            switch (pairId)
            {
                case "red": return new Color(0.78f, 0.26f, 0.24f);
                case "blue": return new Color(0.24f, 0.46f, 0.82f);
                case "green": return new Color(0.25f, 0.68f, 0.38f);
                case "yellow": return new Color(0.86f, 0.72f, 0.24f);
                default: return new Color(0.66f, 0.42f, 0.8f);
            }
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

        private static bool TryGetPointerPosition(out Vector2 position)
        {
            if (Input.touchCount > 0)
            {
                position = Input.GetTouch(0).position;
                return true;
            }

            position = Input.mousePosition;
            return Input.GetMouseButton(0);
        }

        private static bool TryGetPointerUp(out Vector2 position)
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                position = touch.position;
                return touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            }

            position = Input.mousePosition;
            return Input.GetMouseButtonUp(0);
        }

        private static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
