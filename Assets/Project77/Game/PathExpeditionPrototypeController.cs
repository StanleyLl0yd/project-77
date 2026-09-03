using System;
using System.Collections.Generic;
using Project77.Analytics;
using Project77.Puzzle;
using Project77.Puzzle.PathExpeditionRouting;
using UnityEngine;

namespace Project77.Game
{
    public sealed class PathExpeditionPrototypeController : MonoBehaviour
    {
        private const int FirstLevel = 1;
        private const int LastLevel = 10;
        private const int MaxStartDelay = 6;

        private readonly PathExpeditionRunner runner = new PathExpeditionRunner();
        private readonly Dictionary<string, List<GridCell>> plannedRoutes = new Dictionary<string, List<GridCell>>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> startDelays = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<GridCell> dragPath = new List<GridCell>();
        private readonly InMemoryPrototypeAnalyticsSink analytics = new InMemoryPrototypeAnalyticsSink();

        private PrototypeAnalyticsContext analyticsContext;
        private PathExpeditionPayload payload;
        private int levelNumber = FirstLevel;
        private int attemptIndex = 1;
        private int validInteractionCount;
        private int invalidInteractionCount;
        private string activeAgentId;
        private string feedback = "Plan every route, then launch the expedition.";
        private long levelStartMs;
        private long offerTimestampMs;
        private bool continuationOffered;
        private bool awaitingRetry;
        private bool setComplete;

        private void Start()
        {
            analyticsContext = new PrototypeAnalyticsContext(
                Guid.NewGuid().ToString("N"),
                "local_debug",
                Application.version,
                PrototypeVariant.PathExpeditionRouting,
                "unknown",
                Screen.width >= Screen.height ? "landscape" : "portrait");

            Track(
                PrototypeAnalyticsEventName.PrototypeStart,
                null,
                null,
                new Dictionary<string, object>
                {
                    ["entry_point"] = "variant_select",
                    ["core_variant"] = PrototypeVariant.PathExpeditionRouting
                });

            LoadLevel(FirstLevel);
        }

        private void Update()
        {
            if (setComplete || awaitingRetry || runner.Status != PuzzleRunStatus.Active)
            {
                return;
            }

            if (TryGetPointerDown(out var downPosition))
            {
                BeginDrag(downPosition);
            }

            if (activeAgentId != null && TryGetPointerPosition(out var currentPosition))
            {
                ContinueDrag(currentPosition);
            }

            if (activeAgentId != null && TryGetPointerUp(out var upPosition))
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
            DrawDelayControls();
            DrawBottomControls();
        }

        private void LoadLevel(int number)
        {
            levelNumber = number;
            var level = PathExpeditionJsonLoader.LoadResource($"B-{number:000}");
            payload = (PathExpeditionPayload)level.Payload;
            runner.Load(level);
            runner.Start();
            plannedRoutes.Clear();
            startDelays.Clear();
            foreach (var agent in payload.Agents)
            {
                startDelays[agent.Id] = 0;
            }
            activeAgentId = null;
            dragPath.Clear();
            attemptIndex = 1;
            validInteractionCount = 0;
            invalidInteractionCount = 0;
            continuationOffered = false;
            awaitingRetry = false;
            levelStartMs = NowMs();
            feedback = "Draw a route from each explorer to its matching goal.";
            TrackLevelStart();
        }

        private void BeginDrag(Vector2 screenPosition)
        {
            if (!TryScreenToCell(screenPosition, out var cell))
            {
                return;
            }

            var agentId = FindAgentAtStart(cell);
            if (agentId == null)
            {
                invalidInteractionCount++;
                feedback = "Start a route on an explorer's start node.";
                TrackInvalid("path_start", "wrong_target");
                return;
            }

            activeAgentId = agentId;
            dragPath.Clear();
            dragPath.Add(cell);
            feedback = $"Planning {agentId}…";
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
            var agentId = activeAgentId;
            activeAgentId = null;
            var agent = FindAgent(agentId);
            if (agent == null || dragPath.Count < 2 || !dragPath[dragPath.Count - 1].Equals(agent.Goal))
            {
                dragPath.Clear();
                invalidInteractionCount++;
                feedback = "Route must finish on that explorer's goal.";
                TrackInvalid("path_end", "wrong_target");
                return;
            }

            var route = new List<GridCell>(dragPath);
            dragPath.Clear();
            plannedRoutes[agentId] = route;
            var result = SubmitRoute(agentId);
            if (!result.Accepted)
            {
                plannedRoutes.Remove(agentId);
                invalidInteractionCount++;
                feedback = $"Invalid route: {result.Reason}";
                TrackInvalid("path_end", MapInvalidReason(result.Reason));
                return;
            }

            validInteractionCount++;
            feedback = plannedRoutes.Count == payload.Agents.Count
                ? "All routes planned. Launch when ready."
                : "Route accepted. Plan the remaining explorers.";
        }

        private PuzzleActionResult SubmitRoute(string agentId)
        {
            if (!plannedRoutes.TryGetValue(agentId, out var baseRoute))
            {
                return PuzzleActionResult.Reject(PathExpeditionInvalidReason.BadEndpoint);
            }

            var delay = startDelays.TryGetValue(agentId, out var value) ? value : 0;
            var timedRoute = new List<GridCell>(baseRoute.Count + delay);
            for (var index = 0; index < delay; index++)
            {
                timedRoute.Add(baseRoute[0]);
            }
            timedRoute.AddRange(baseRoute);
            return runner.Apply(new PathExpeditionSetRouteAction(agentId, timedRoute));
        }

        private void ChangeDelay(string agentId, int delta)
        {
            if (!plannedRoutes.ContainsKey(agentId) || awaitingRetry)
            {
                return;
            }

            var next = Mathf.Clamp(startDelays[agentId] + delta, 0, MaxStartDelay);
            if (next == startDelays[agentId])
            {
                return;
            }

            startDelays[agentId] = next;
            var result = SubmitRoute(agentId);
            if (!result.Accepted)
            {
                feedback = $"Delay update rejected: {result.Reason}";
                return;
            }
            feedback = $"{agentId} start delay: {next}.";
        }

        private void CommitPlan()
        {
            if (plannedRoutes.Count != payload.Agents.Count || awaitingRetry || continuationOffered)
            {
                return;
            }

            var result = runner.Apply(new PathExpeditionCommitAction());
            if (!result.Accepted)
            {
                invalidInteractionCount++;
                feedback = result.Reason == PathExpeditionInvalidReason.IncompletePlan
                    ? "Plan every explorer before launch."
                    : $"Launch rejected: {result.Reason}";
                return;
            }

            if (runner.Status == PuzzleRunStatus.Succeeded)
            {
                feedback = "Expedition reached every goal safely.";
                TrackLevelComplete();
                OfferContinuation();
                return;
            }

            if (runner.Status == PuzzleRunStatus.Failed)
            {
                awaitingRetry = true;
                feedback = runner.LastFailReason == PathExpeditionFailReason.SwapCollision
                    ? "Head-on collision. Adjust a route or start delay."
                    : "Explorers collide at the same time. Adjust a route or start delay.";
                Track(
                    PrototypeAnalyticsEventName.LevelFail,
                    runner.Level.Id,
                    runner.Level.Revision,
                    new Dictionary<string, object>
                    {
                        ["attempt_index"] = attemptIndex,
                        ["duration_ms"] = (int)Math.Max(0, NowMs() - levelStartMs),
                        ["fail_reason"] = runner.LastFailReason,
                        ["core_variant"] = PrototypeVariant.PathExpeditionRouting
                    });
            }
        }

        private void RetryPlan()
        {
            if (!awaitingRetry)
            {
                return;
            }

            var previousAttempt = attemptIndex;
            attemptIndex++;
            runner.Restart();
            foreach (var agent in payload.Agents)
            {
                if (plannedRoutes.ContainsKey(agent.Id))
                {
                    var result = SubmitRoute(agent.Id);
                    if (!result.Accepted)
                    {
                        throw new InvalidOperationException($"Stored route for '{agent.Id}' became invalid: {result.Reason}.");
                    }
                }
            }

            awaitingRetry = false;
            levelStartMs = NowMs();
            Track(
                PrototypeAnalyticsEventName.LevelRetry,
                runner.Level.Id,
                runner.Level.Revision,
                new Dictionary<string, object>
                {
                    ["previous_attempt_index"] = previousAttempt,
                    ["new_attempt_index"] = attemptIndex,
                    ["retry_source"] = "fail_screen"
                });
            TrackLevelStart();
            feedback = "Plan restored. Adjust route or delay, then launch again.";
        }

        private void ResetPlan()
        {
            var previousAttempt = attemptIndex;
            attemptIndex++;
            runner.Restart();
            plannedRoutes.Clear();
            foreach (var agent in payload.Agents)
            {
                startDelays[agent.Id] = 0;
            }
            awaitingRetry = false;
            continuationOffered = false;
            activeAgentId = null;
            dragPath.Clear();
            levelStartMs = NowMs();
            feedback = "Plan cleared.";
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
                    ["next_level_id"] = levelNumber < LastLevel ? $"B-{levelNumber + 1:000}" : "B-END",
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
                    ["next_level_id"] = levelNumber < LastLevel ? $"B-{levelNumber + 1:000}" : "B-END",
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
                    ["core_variant"] = PrototypeVariant.PathExpeditionRouting,
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
                    ["core_variant"] = PrototypeVariant.PathExpeditionRouting,
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

        private void Track(string eventName, string levelId, int? levelRevision, IReadOnlyDictionary<string, object> properties)
        {
            analytics.Track(new PrototypeAnalyticsEvent(eventName, NowMs(), analyticsContext, levelId, levelRevision, properties));
        }

        private void DrawHeader()
        {
            var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            var bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            GUI.Label(new Rect(20f, 12f, Screen.width - 40f, 32f), "Project 77 — Prototype B: Path / Expedition Routing", titleStyle);
            GUI.Label(new Rect(20f, 44f, Screen.width - 40f, 24f), setComplete ? "Initial 10-level set complete." : $"Level B-{levelNumber:000} · {feedback}", bodyStyle);
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
                    var previous = GUI.backgroundColor;
                    GUI.backgroundColor = IsBlocked(cell) ? new Color(0.22f, 0.22f, 0.24f) : new Color(0.48f, 0.5f, 0.54f);
                    GUI.Box(GetCellRect(cell, boardRect, cellSize), string.Empty);
                    GUI.backgroundColor = previous;
                }
            }

            foreach (var agent in payload.Agents)
            {
                if (plannedRoutes.TryGetValue(agent.Id, out var route))
                {
                    DrawPath(agent.Id, route, boardRect, cellSize, 0.58f);
                }
            }
            if (activeAgentId != null)
            {
                DrawPath(activeAgentId, dragPath, boardRect, cellSize, 0.42f);
            }
            foreach (var agent in payload.Agents)
            {
                DrawMarker(agent.Id, agent.Start, "S", boardRect, cellSize);
                DrawMarker(agent.Id, agent.Goal, "G", boardRect, cellSize);
            }
        }

        private void DrawDelayControls()
        {
            if (payload == null)
            {
                return;
            }
            var y = Screen.height - 122f;
            var x = 20f;
            foreach (var agent in payload.Agents)
            {
                if (!plannedRoutes.ContainsKey(agent.Id))
                {
                    continue;
                }
                var label = $"{agent.Id}: wait {startDelays[agent.Id]}";
                GUI.Label(new Rect(x, y, 110f, 28f), label);
                if (GUI.Button(new Rect(x + 110f, y, 34f, 28f), "-"))
                {
                    ChangeDelay(agent.Id, -1);
                }
                if (GUI.Button(new Rect(x + 148f, y, 34f, 28f), "+"))
                {
                    ChangeDelay(agent.Id, 1);
                }
                x += 200f;
            }
        }

        private void DrawBottomControls()
        {
            var y = Screen.height - 64f;
            if (GUI.Button(new Rect(20f, y, 120f, 40f), "Reset plan"))
            {
                ResetPlan();
            }

            if (awaitingRetry)
            {
                if (GUI.Button(new Rect(Screen.width * 0.5f - 70f, y, 140f, 40f), "Retry plan"))
                {
                    RetryPlan();
                }
                return;
            }

            if (!continuationOffered && GUI.Button(new Rect(Screen.width * 0.5f - 70f, y, 140f, 40f), "Launch"))
            {
                CommitPlan();
            }

            if (continuationOffered && GUI.Button(new Rect(Screen.width - 170f, y, 150f, 40f), "Next level"))
            {
                NextLevel();
            }
        }

        private void DrawSetComplete()
        {
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, wordWrap = true };
            GUI.Label(new Rect(30f, 100f, Screen.width - 60f, Screen.height - 200f), "Prototype B initial set complete.\nThis is a greybox P0 comparison build.", style);
            if (GUI.Button(new Rect(Screen.width * 0.5f - 90f, Screen.height - 80f, 180f, 44f), "Restart set"))
            {
                setComplete = false;
                LoadLevel(FirstLevel);
            }
        }

        private void DrawPath(string agentId, IReadOnlyList<GridCell> path, Rect boardRect, float cellSize, float insetFactor)
        {
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = AgentColor(agentId);
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

        private void DrawMarker(string agentId, GridCell cell, string suffix, Rect boardRect, float cellSize)
        {
            var rect = GetCellRect(cell, boardRect, cellSize);
            var inset = cellSize * 0.17f;
            rect.x += inset;
            rect.y += inset;
            rect.width -= inset * 2f;
            rect.height -= inset * 2f;
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = AgentColor(agentId);
            GUI.Box(rect, agentId.Substring(0, 1).ToUpperInvariant() + suffix);
            GUI.backgroundColor = previous;
        }

        private float GetCellSize()
        {
            var widthFit = (Screen.width - 40f) / payload.Width;
            var heightFit = (Screen.height - 230f) / payload.Height;
            return Mathf.Clamp(Mathf.Min(widthFit, heightFit), 34f, 82f);
        }

        private Rect GetBoardRect(float cellSize)
        {
            var width = payload.Width * cellSize;
            var height = payload.Height * cellSize;
            return new Rect((Screen.width - width) * 0.5f, 78f + (Screen.height - 230f - height) * 0.5f, width, height);
        }

        private Rect GetCellRect(GridCell cell, Rect boardRect, float cellSize)
        {
            var guiRow = payload.Height - 1 - cell.Y;
            return new Rect(boardRect.x + cell.X * cellSize, boardRect.y + guiRow * cellSize, cellSize - 2f, cellSize - 2f);
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

        private PathExpeditionAgent FindAgent(string id)
        {
            foreach (var agent in payload.Agents)
            {
                if (agent.Id == id)
                {
                    return agent;
                }
            }
            return null;
        }

        private string FindAgentAtStart(GridCell cell)
        {
            foreach (var agent in payload.Agents)
            {
                if (agent.Start.Equals(cell))
                {
                    return agent.Id;
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
            if (reason == PathExpeditionInvalidReason.OutOfBounds) return "out_of_bounds";
            if (reason == PathExpeditionInvalidReason.Blocked) return "blocked";
            if (reason == PathExpeditionInvalidReason.WrongTarget || reason == PathExpeditionInvalidReason.BadEndpoint) return "wrong_target";
            return "rule_violation";
        }

        private static Color AgentColor(string id)
        {
            switch (id)
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
