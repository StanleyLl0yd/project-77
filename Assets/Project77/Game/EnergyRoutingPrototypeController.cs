using System;
using System.Collections.Generic;
using Project77.Analytics;
using Project77.Meta;
using Project77.Puzzle;
using Project77.Puzzle.EnergyRouting;
using UnityEngine;

namespace Project77.Game
{
    public sealed class EnergyRoutingPrototypeController : MonoBehaviour
    {
        private const int FirstLevel = 1;
        private const int LastLevel = 10;

        private enum PrototypeView
        {
            Intro,
            Puzzle,
            Reward,
            Island
        }

        private readonly EnergyRoutingRunner runner = new EnergyRoutingRunner();
        private readonly List<GridCell> dragPath = new List<GridCell>();
        private readonly InMemoryPrototypeAnalyticsSink analytics = new InMemoryPrototypeAnalyticsSink();
        private readonly PrototypeMetaProgression meta = new PrototypeMetaProgression();

        private PrototypeAnalyticsContext analyticsContext;
        private EnergyRoutingPayload payload;
        private PrototypeLevelReward pendingReward;
        private PrototypeView view = PrototypeView.Puzzle;
        private int levelNumber = FirstLevel;
        private int attemptIndex = 1;
        private int validInteractionCount;
        private int invalidInteractionCount;
        private string activePairId;
        private string feedback = "Connect matching nodes without crossing paths.";
        private string playtestId = "local_debug";
        private string continuationContext = "post_level";
        private long levelStartMs;
        private long sessionStartMs;
        private long offerTimestampMs;
        private bool continuationOffered;
        private bool setComplete;
        private bool metaLoopEnabled;

        public void ConfigureSelectedMeta(string anonymousPlaytestId)
        {
            metaLoopEnabled = true;
            if (!string.IsNullOrWhiteSpace(anonymousPlaytestId))
            {
                playtestId = anonymousPlaytestId;
            }
        }

        private void Start()
        {
            sessionStartMs = NowMs();
            analyticsContext = new PrototypeAnalyticsContext(
                Guid.NewGuid().ToString("N"),
                playtestId,
                Application.version,
                metaLoopEnabled ? PrototypeVariant.SelectedMeta : PrototypeVariant.EnergyRouting,
                "unknown",
                PrototypeGuiLayout.Width >= PrototypeGuiLayout.Height ? "landscape" : "portrait");

            Track(
                PrototypeAnalyticsEventName.PrototypeStart,
                null,
                null,
                new Dictionary<string, object>
                {
                    ["entry_point"] = "fresh_launch",
                    ["core_variant"] = PrototypeVariant.EnergyRouting
                });

            if (metaLoopEnabled)
            {
                view = PrototypeView.Intro;
                return;
            }

            LoadLevel(FirstLevel);
        }

        private void Update()
        {
            if (view != PrototypeView.Puzzle ||
                setComplete ||
                continuationOffered ||
                runner.Status != PuzzleRunStatus.Active)
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
            PrototypeGuiLayout.Begin();
            try
            {
                if (setComplete)
                {
                    DrawSetComplete();
                    return;
                }

                if (metaLoopEnabled && view == PrototypeView.Intro)
                {
                    DrawIntro();
                    return;
                }

                if (metaLoopEnabled && view == PrototypeView.Reward)
                {
                    DrawReward();
                    return;
                }

                if (metaLoopEnabled && view == PrototypeView.Island)
                {
                    DrawIsland();
                    return;
                }

                DrawHeader();
                DrawBoard();
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
            var levelId = $"A-{number:000}";
            var level = EnergyRoutingJsonLoader.LoadResource(levelId);
            payload = (EnergyRoutingPayload)level.Payload;
            runner.Load(level);
            runner.Start();
            view = PrototypeView.Puzzle;
            pendingReward = null;
            attemptIndex = 1;
            validInteractionCount = 0;
            invalidInteractionCount = 0;
            activePairId = null;
            dragPath.Clear();
            continuationOffered = false;
            continuationContext = "post_level";
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
                if (metaLoopEnabled)
                {
                    ShowReward();
                }
                else
                {
                    OfferContinuation("post_level");
                }
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

        private void ShowReward()
        {
            pendingReward = meta.RewardForLevel(levelNumber);
            view = PrototypeView.Reward;
            continuationOffered = false;

            Track(
                PrototypeAnalyticsEventName.RewardShown,
                runner.Level.Id,
                runner.Level.Revision,
                new Dictionary<string, object>
                {
                    ["reward_id"] = pendingReward.RewardId,
                    ["reward_source"] = "level_complete",
                    ["scrap_amount"] = pendingReward.ScrapAmount,
                    ["energy_amount"] = pendingReward.EnergyAmount
                });
        }

        private void ClaimReward()
        {
            if (pendingReward == null || !meta.ClaimReward(pendingReward))
            {
                return;
            }

            Track(
                PrototypeAnalyticsEventName.RewardClaimed,
                runner.Level.Id,
                runner.Level.Revision,
                new Dictionary<string, object>
                {
                    ["reward_id"] = pendingReward.RewardId,
                    ["claim_mode"] = "explicit",
                    ["scrap_amount"] = pendingReward.ScrapAmount,
                    ["energy_amount"] = pendingReward.EnergyAmount
                });

            pendingReward = null;
            view = PrototypeView.Island;
            continuationOffered = false;

            if ((!meta.GeneratorRepaired && !meta.CanRepairGenerator) ||
                meta.Robot77Discovered)
            {
                OfferContinuation("post_reward");
            }
        }

        private void RepairGenerator()
        {
            var result = meta.RepairGenerator();
            if (!result.Accepted)
            {
                return;
            }

            Track(
                PrototypeAnalyticsEventName.ResourceSpend,
                null,
                null,
                new Dictionary<string, object>
                {
                    ["resource_type"] = "scrap",
                    ["amount"] = result.ScrapSpent,
                    ["sink_id"] = PrototypeMetaProgression.GeneratorSinkId,
                    ["balance_after"] = meta.Scrap
                });
            Track(
                PrototypeAnalyticsEventName.ResourceSpend,
                null,
                null,
                new Dictionary<string, object>
                {
                    ["resource_type"] = "energy",
                    ["amount"] = result.EnergySpent,
                    ["sink_id"] = PrototypeMetaProgression.GeneratorSinkId,
                    ["balance_after"] = meta.Energy
                });
            Track(
                PrototypeAnalyticsEventName.GeneratorRepair,
                null,
                null,
                new Dictionary<string, object>
                {
                    ["repair_stage"] = 1,
                    ["scrap_spent"] = result.ScrapSpent,
                    ["energy_spent"] = result.EnergySpent,
                    ["time_since_session_start_ms"] = (int)Math.Max(0, NowMs() - sessionStartMs)
                });
            Track(
                PrototypeAnalyticsEventName.IslandChange,
                null,
                null,
                new Dictionary<string, object>
                {
                    ["change_id"] = "generator_power_on",
                    ["change_type"] = "power_on",
                    ["caused_by"] = "generator_repair"
                });
        }

        private void UnlockArea()
        {
            if (!meta.UnlockFirstArea())
            {
                return;
            }

            Track(
                PrototypeAnalyticsEventName.AreaUnlock,
                null,
                null,
                new Dictionary<string, object>
                {
                    ["area_id"] = PrototypeMetaProgression.FirstAreaId,
                    ["unlock_source"] = "generator_repair"
                });
            Track(
                PrototypeAnalyticsEventName.IslandChange,
                null,
                null,
                new Dictionary<string, object>
                {
                    ["change_id"] = "generator_annex_open",
                    ["change_type"] = "unlock_visual",
                    ["caused_by"] = "area_unlock"
                });
        }

        private void DiscoverRobot77()
        {
            if (!meta.DiscoverRobot77())
            {
                return;
            }

            Track(
                PrototypeAnalyticsEventName.Robot77Discovered,
                null,
                null,
                new Dictionary<string, object>
                {
                    ["discovery_id"] = PrototypeMetaProgression.Robot77DiscoveryId,
                    ["time_since_session_start_ms"] = (int)Math.Max(0, NowMs() - sessionStartMs),
                    ["levels_completed_before_discovery"] = levelNumber
                });

            OfferContinuation("post_77_discovery");
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
                    ["offer_context"] = continuationContext,
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

        private void OfferContinuation(string offerContext)
        {
            continuationOffered = true;
            continuationContext = offerContext;
            offerTimestampMs = NowMs();
            Track(
                PrototypeAnalyticsEventName.NextPuzzleOffered,
                runner.Level.Id,
                runner.Level.Revision,
                new Dictionary<string, object>
                {
                    ["offer_context"] = offerContext,
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
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            var bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                wordWrap = true
            };

            GUI.Label(
                new Rect(20f, 14f, PrototypeGuiLayout.Width - 40f, 36f),
                metaLoopEnabled
                    ? "Project 77 — P1: Energy Routing + Island"
                    : "Project 77 — Prototype A: Energy Routing",
                titleStyle);
            GUI.Label(
                new Rect(20f, 50f, PrototypeGuiLayout.Width - 40f, 46f),
                $"Level A-{levelNumber:000} · {feedback}",
                bodyStyle);
        }

        private void DrawIntro()
        {
            var width = PrototypeGuiLayout.ContentWidth(620f, 20f);
            var x = (PrototypeGuiLayout.Width - width) * 0.5f;
            var title = CenteredStyle(30, FontStyle.Bold);
            var body = CenteredStyle(20, FontStyle.Normal);

            GUI.Label(new Rect(x, 72f, width, 48f), "Project 77", title);
            GUI.Box(new Rect(x, 138f, width, 244f), string.Empty);
            GUI.Label(
                new Rect(x + 24f, 164f, width - 48f, 132f),
                "The island is silent. Its old generator is dead.\n\nA weak energy network still responds beneath the surface.",
                body);
            GUI.Label(
                new Rect(x + 24f, 304f, width - 48f, 42f),
                "Restore a route and see what wakes up.",
                body);

            if (GUI.Button(new Rect(x + 24f, 400f, width - 48f, 60f), "Begin restoration"))
            {
                LoadLevel(FirstLevel);
            }
        }

        private void DrawReward()
        {
            var width = PrototypeGuiLayout.ContentWidth(620f, 20f);
            var x = (PrototypeGuiLayout.Width - width) * 0.5f;
            var title = CenteredStyle(28, FontStyle.Bold);
            var body = CenteredStyle(20, FontStyle.Normal);

            GUI.Label(new Rect(x, 70f, width, 44f), "Puzzle complete", title);
            GUI.Box(new Rect(x, 132f, width, 220f), string.Empty);
            GUI.Label(new Rect(x + 20f, 154f, width - 40f, 40f), "Recovered resources", body);
            GUI.Label(
                new Rect(x + 20f, 206f, width - 40f, 70f),
                $"+{pendingReward.ScrapAmount} Scrap\n+{pendingReward.EnergyAmount} Energy",
                title);

            if (GUI.Button(new Rect(x + 20f, 292f, width - 40f, 52f), "Claim reward"))
            {
                ClaimReward();
            }
        }

        private void DrawIsland()
        {
            var width = PrototypeGuiLayout.ContentWidth(660f, 18f);
            var x = (PrototypeGuiLayout.Width - width) * 0.5f;
            var title = CenteredStyle(28, FontStyle.Bold);
            var body = CenteredStyle(18, FontStyle.Normal);
            var status = CenteredStyle(20, FontStyle.Bold);

            GUI.Label(new Rect(x, 28f, width, 44f), "Abandoned Island", title);
            GUI.Label(
                new Rect(x, 72f, width, 38f),
                $"Scrap {meta.Scrap}   ·   Energy {meta.Energy}",
                status);

            GUI.Box(new Rect(x, 124f, width, 252f), string.Empty);
            GUI.Label(
                new Rect(x + 20f, 144f, width - 40f, 34f),
                meta.GeneratorRepaired ? "GENERATOR: ONLINE" : "GENERATOR: DAMAGED",
                status);
            GUI.Label(
                new Rect(x + 20f, 184f, width - 40f, 34f),
                meta.AreaUnlocked ? "GENERATOR ANNEX: OPEN" : "GENERATOR ANNEX: SEALED",
                body);
            GUI.Label(
                new Rect(x + 20f, 222f, width - 40f, 72f),
                meta.Robot77Discovered
                    ? "77: damaged robot found. Its systems are dormant, but it reacted to restored power."
                    : "77: no contact",
                body);

            if (!meta.GeneratorRepaired && meta.CanRepairGenerator)
            {
                GUI.Label(
                    new Rect(x + 20f, 294f, width - 40f, 36f),
                    "The generator can be repaired with the resources you recovered.",
                    body);
                if (GUI.Button(
                        new Rect(x + 20f, 388f, width - 40f, 56f),
                        $"Repair generator — {PrototypeMetaProgression.GeneratorScrapCost} Scrap + {PrototypeMetaProgression.GeneratorEnergyCost} Energy"))
                {
                    RepairGenerator();
                }
                return;
            }

            if (!meta.GeneratorRepaired)
            {
                GUI.Label(
                    new Rect(x + 20f, 294f, width - 40f, 48f),
                    $"Repair requires {PrototypeMetaProgression.GeneratorScrapCost} Scrap and {PrototypeMetaProgression.GeneratorEnergyCost} Energy.",
                    body);
            }
            else if (!meta.AreaUnlocked)
            {
                GUI.Label(
                    new Rect(x + 20f, 294f, width - 40f, 48f),
                    "Power is back. A gate beside the generator has unlocked.",
                    body);
                if (GUI.Button(new Rect(x + 20f, 388f, width - 40f, 56f), "Open powered area"))
                {
                    UnlockArea();
                }
                return;
            }
            else if (!meta.Robot77Discovered)
            {
                GUI.Label(
                    new Rect(x + 20f, 294f, width - 40f, 48f),
                    "A weak signal is coming from inside the opened annex.",
                    body);
                if (GUI.Button(new Rect(x + 20f, 388f, width - 40f, 56f), "Investigate signal"))
                {
                    DiscoverRobot77();
                }
                return;
            }

            if (continuationOffered)
            {
                GUI.Label(
                    new Rect(x + 20f, 388f, width - 40f, 38f),
                    meta.Robot77Discovered
                        ? "The island changed. Another energy route is available."
                        : "You need more resources. Another route is available.",
                    body);
                if (GUI.Button(
                        new Rect(x + 20f, 438f, width - 40f, 58f),
                        levelNumber < LastLevel ? "Start next puzzle" : "Finish prototype"))
                {
                    NextLevel();
                }
            }
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
            var y = PrototypeGuiLayout.Height - 72f;
            if (PrototypeAttemptPolicy.CanRestartAttempt(runner.Status, continuationOffered) &&
                GUI.Button(new Rect(20f, y, 142f, 50f), "Restart"))
            {
                RestartLevel();
            }

            if (!metaLoopEnabled &&
                continuationOffered &&
                GUI.Button(new Rect(PrototypeGuiLayout.Width - 172f, y, 152f, 50f), "Next level"))
            {
                NextLevel();
            }
        }

        private void DrawSetComplete()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                wordWrap = true
            };
            GUI.Label(
                new Rect(30f, 100f, PrototypeGuiLayout.Width - 60f, PrototypeGuiLayout.Height - 200f),
                metaLoopEnabled
                    ? "Prototype 0.1 P1 path complete.\nPuzzle → reward → repair → island change → 77 → continuation is now implemented."
                    : "Prototype A initial set complete.\nThis is a greybox build for P0 comparison, not production gameplay.",
                style);

            if (!metaLoopEnabled &&
                GUI.Button(
                    new Rect(PrototypeGuiLayout.Width * 0.5f - 90f, PrototypeGuiLayout.Height - 88f, 180f, 52f),
                    "Restart set"))
            {
                setComplete = false;
                LoadLevel(FirstLevel);
            }
        }

        private static GUIStyle CenteredStyle(int fontSize, FontStyle fontStyle)
        {
            return new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
                fontStyle = fontStyle,
                wordWrap = true
            };
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
            var widthFit = (PrototypeGuiLayout.Width - 40f) / payload.Width;
            var heightFit = (PrototypeGuiLayout.Height - 230f) / payload.Height;
            return Mathf.Clamp(Mathf.Min(widthFit, heightFit), 36f, 86f);
        }

        private Rect GetBoardRect(float cellSize)
        {
            var width = payload.Width * cellSize;
            var height = payload.Height * cellSize;
            return new Rect(
                (PrototypeGuiLayout.Width - width) * 0.5f,
                104f + (PrototypeGuiLayout.Height - 230f - height) * 0.5f,
                width,
                height);
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
