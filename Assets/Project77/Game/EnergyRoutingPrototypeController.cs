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
        private const float PortraitBoardTop = 224f;
        private const float CompactBoardTop = 174f;
        private const float BoardBottomReserve = 94f;

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
        private string activeTargetLabel;
        private string feedback = "Ready.";
        private string islandNotice = "The generator is dark. The annex has no power.";
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
            activeTargetLabel = null;
            dragPath.Clear();
            continuationOffered = false;
            continuationContext = "post_level";
            levelStartMs = NowMs();
            feedback = levelNumber == FirstLevel ? "Start with the labeled nodes." : "Ready.";

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

            if (IsBlocked(cell))
            {
                invalidInteractionCount++;
                feedback = "X is blocked. Start on a labeled endpoint.";
                TrackInvalid("path_start", "blocked");
                return;
            }

            var pair = FindPairAtEndpoint(cell);
            if (pair == null)
            {
                invalidInteractionCount++;
                feedback = "Start on a labeled endpoint such as R1 or R2.";
                TrackInvalid("path_start", "wrong_target");
                return;
            }

            activePairId = pair.Id;
            activeTargetLabel = cell.Equals(pair.Start)
                ? EnergyRoutingPresentation.EndpointLabel(pair.Id, 2)
                : EnergyRoutingPresentation.EndpointLabel(pair.Id, 1);
            dragPath.Clear();
            dragPath.Add(cell);
            feedback = $"Keep your finger down and drag to {activeTargetLabel}.";
        }

        private void ContinueDrag(Vector2 screenPosition)
        {
            if (!TryScreenToCell(screenPosition, out var target) || dragPath.Count == 0)
            {
                return;
            }

            var last = dragPath[dragPath.Count - 1];
            if (target.Equals(last))
            {
                return;
            }

            if (last.X != target.X && last.Y != target.Y)
            {
                return;
            }

            var stepX = Math.Sign(target.X - last.X);
            var stepY = Math.Sign(target.Y - last.Y);
            while (!last.Equals(target))
            {
                var next = new GridCell(last.X + stepX, last.Y + stepY);
                if (!TryAppendDragCell(next))
                {
                    return;
                }

                last = dragPath[dragPath.Count - 1];
            }
        }

        private bool TryAppendDragCell(GridCell cell)
        {
            var last = dragPath[dragPath.Count - 1];
            if (!last.IsOrthogonallyAdjacentTo(cell))
            {
                return false;
            }

            if (dragPath.Count >= 2 && cell.Equals(dragPath[dragPath.Count - 2]))
            {
                dragPath.RemoveAt(dragPath.Count - 1);
                return true;
            }

            if (dragPath.Contains(cell))
            {
                feedback = "A route cannot loop through the same square.";
                return false;
            }

            if (IsBlocked(cell))
            {
                feedback = "X is blocked. Keep holding and route around it.";
                return false;
            }

            var endpointPair = FindPairAtEndpoint(cell);
            if (endpointPair != null && endpointPair.Id != activePairId)
            {
                feedback = "That endpoint belongs to another pair.";
                return false;
            }

            if (IsOccupiedByAnotherPath(activePairId, cell))
            {
                feedback = "That square is already used. Paths cannot cross.";
                return false;
            }

            dragPath.Add(cell);
            return true;
        }

        private void EndDrag()
        {
            var pairId = activePairId;
            var targetLabel = activeTargetLabel;
            activePairId = null;
            activeTargetLabel = null;

            var result = runner.Apply(new EnergyRoutingPathAction(pairId, dragPath.ToArray()));
            dragPath.Clear();

            if (!result.Accepted)
            {
                invalidInteractionCount++;
                feedback = FriendlyInvalidReason(result.Reason, targetLabel);
                TrackInvalid("path_end", MapInvalidReason(result.Reason));
                return;
            }

            validInteractionCount++;
            var code = EnergyRoutingPresentation.PairCode(pairId);
            feedback = runner.Status == PuzzleRunStatus.Succeeded
                ? "All matching labels connected. Network restored."
                : $"{code}1 and {code}2 connected. Connect the remaining pair.";

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
            activeTargetLabel = null;
            dragPath.Clear();
            continuationOffered = false;
            validInteractionCount = 0;
            invalidInteractionCount = 0;
            levelStartMs = NowMs();
            feedback = "Route cleared. Start again on a labeled endpoint.";

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
            islandNotice = meta.CanRepairGenerator
                ? "You now have enough material and stored power to repair the generator."
                : "The recovered resources can be used to repair the island generator.";

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

            islandNotice = "POWER RESTORED. Lights come on and the sealed annex receives power.";
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

            islandNotice = "ANNEX OPEN. A weak signal is now detectable inside.";
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

            islandNotice = "SIGNAL FOUND: 77. The damaged robot reacts to the restored power.";
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
            var compactVertical = PrototypeGuiLayout.Height < 650f;
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = compactVertical ? 22 : 26,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            var feedbackStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = compactVertical ? 16 : 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            var instructionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = compactVertical ? 15 : 17,
                wordWrap = true
            };
            var legendStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = compactVertical ? 14 : 16,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            GUI.Label(
                new Rect(20f, 10f, PrototypeGuiLayout.Width - 40f, 34f),
                metaLoopEnabled ? "Project 77 — Energy Routing" : "Project 77 — Prototype A: Energy Routing",
                titleStyle);
            GUI.Label(
                new Rect(20f, 46f, PrototypeGuiLayout.Width - 40f, compactVertical ? 38f : 46f),
                $"Route {levelNumber} of {LastLevel} · {feedback}",
                feedbackStyle);

            var instructionY = compactVertical ? 86f : 96f;
            var instructionHeight = compactVertical ? 78f : 116f;
            var panel = new Rect(20f, instructionY, PrototypeGuiLayout.Width - 40f, instructionHeight);
            GUI.Box(panel, string.Empty);

            var connectedCount = ConnectedPairCount();
            GUI.Label(
                new Rect(panel.x + 10f, panel.y + 6f, panel.width - 20f, compactVertical ? 42f : 66f),
                EnergyRoutingPresentation.Instruction(levelNumber, payload.BlockedCells.Count, connectedCount),
                instructionStyle);
            GUI.Label(
                new Rect(panel.x + 10f, panel.y + instructionHeight - (compactVertical ? 30f : 40f), panel.width - 20f, compactVertical ? 28f : 36f),
                BuildLegend(),
                legendStyle);
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
            var body = CenteredStyle(18, FontStyle.Normal);
            var resource = CenteredStyle(22, FontStyle.Bold);

            GUI.Label(new Rect(x, 62f, width, 44f), "Route restored", title);
            GUI.Box(new Rect(x, 124f, width, 276f), string.Empty);
            GUI.Label(new Rect(x + 20f, 144f, width - 40f, 36f), "Recovered resources", body);
            GUI.Label(
                new Rect(x + 20f, 190f, width - 40f, 74f),
                $"SCRAP +{pendingReward.ScrapAmount}\nENERGY +{pendingReward.EnergyAmount}",
                resource);
            GUI.Label(
                new Rect(x + 20f, 270f, width - 40f, 70f),
                "Scrap is repair material. Energy is stored power. Both can restore island machinery.",
                body);

            if (GUI.Button(new Rect(x + 20f, 420f, width - 40f, 58f), "Take resources"))
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
            var notice = CenteredStyle(19, FontStyle.Bold);

            GUI.Label(new Rect(x, 24f, width, 44f), "Abandoned Island", title);
            GUI.Label(
                new Rect(x, 68f, width, 52f),
                $"SCRAP {meta.Scrap}/{PrototypeMetaProgression.GeneratorScrapCost}   ·   ENERGY {meta.Energy}/{PrototypeMetaProgression.GeneratorEnergyCost}",
                status);

            GUI.Box(new Rect(x, 126f, width, 246f), string.Empty);
            GUI.Label(
                new Rect(x + 20f, 144f, width - 40f, 36f),
                meta.GeneratorRepaired ? "[GENERATOR] ONLINE" : "[GENERATOR] OFFLINE — NEEDS REPAIR",
                status);
            GUI.Label(
                new Rect(x + 20f, 184f, width - 40f, 34f),
                meta.AreaUnlocked ? "[ANNEX] OPEN" : "[ANNEX] LOCKED — NO POWER",
                body);
            GUI.Label(
                new Rect(x + 20f, 222f, width - 40f, 56f),
                meta.Robot77Discovered ? "[SIGNAL] 77 FOUND" : "[SIGNAL] NONE",
                body);
            GUI.Label(
                new Rect(x + 20f, 286f, width - 40f, 68f),
                islandNotice,
                notice);

            if (!meta.GeneratorRepaired && meta.CanRepairGenerator)
            {
                if (GUI.Button(
                        new Rect(x + 20f, 394f, width - 40f, 62f),
                        $"Repair generator — spend {PrototypeMetaProgression.GeneratorScrapCost} Scrap + {PrototypeMetaProgression.GeneratorEnergyCost} Energy"))
                {
                    RepairGenerator();
                }
                return;
            }

            if (!meta.GeneratorRepaired)
            {
                GUI.Label(
                    new Rect(x + 20f, 382f, width - 40f, 50f),
                    $"Generator repair needs {PrototypeMetaProgression.GeneratorScrapCost} Scrap and {PrototypeMetaProgression.GeneratorEnergyCost} Energy.",
                    body);
            }
            else if (!meta.AreaUnlocked)
            {
                if (GUI.Button(new Rect(x + 20f, 394f, width - 40f, 62f), "Open the powered annex"))
                {
                    UnlockArea();
                }
                return;
            }
            else if (!meta.Robot77Discovered)
            {
                if (GUI.Button(new Rect(x + 20f, 394f, width - 40f, 62f), "Investigate the signal"))
                {
                    DiscoverRobot77();
                }
                return;
            }

            if (continuationOffered)
            {
                GUI.Label(
                    new Rect(x + 20f, 444f, width - 40f, 50f),
                    meta.Robot77Discovered
                        ? "Another energy route is available."
                        : "You need more resources. Another energy route is available.",
                    body);
                if (GUI.Button(
                        new Rect(x + 20f, 504f, width - 40f, 62f),
                        levelNumber < LastLevel ? "Restore another route" : "Finish prototype"))
                {
                    NextLevel();
                }
            }
        }

        private void DrawBoard()
        {
            var cellSize = GetCellSize();
            var boardRect = GetBoardRect(cellSize);
            var blockedStyle = CenteredStyle(Mathf.Clamp((int)(cellSize * 0.38f), 20, 34), FontStyle.Bold);

            for (var y = 0; y < payload.Height; y++)
            {
                for (var x = 0; x < payload.Width; x++)
                {
                    var cell = new GridCell(x, y);
                    var rect = GetCellRect(cell, boardRect, cellSize);
                    var previous = GUI.backgroundColor;
                    var blocked = IsBlocked(cell);
                    GUI.backgroundColor = blocked
                        ? new Color(0.92f, 0.30f, 0.24f)
                        : new Color(0.78f, 0.82f, 0.88f);
                    GUI.Box(rect, string.Empty);
                    GUI.backgroundColor = previous;

                    if (blocked)
                    {
                        GUI.Label(rect, "X", blockedStyle);
                    }
                }
            }

            foreach (var pair in payload.Pairs)
            {
                DrawPath(pair.Id, runner.GetPath(pair.Id), boardRect, cellSize, 0.68f);
            }

            if (activePairId != null)
            {
                DrawPath(activePairId, dragPath, boardRect, cellSize, 0.50f);
            }

            foreach (var pair in payload.Pairs)
            {
                DrawEndpoint(pair.Id, pair.Start, 1, boardRect, cellSize);
                DrawEndpoint(pair.Id, pair.End, 2, boardRect, cellSize);
            }
        }

        private void DrawControls()
        {
            var y = PrototypeGuiLayout.Height - 72f;
            if (PrototypeAttemptPolicy.CanRestartAttempt(runner.Status, continuationOffered) &&
                GUI.Button(new Rect(20f, y, 142f, 50f), "Clear routes"))
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
                    ? "Prototype route complete. Thank you for playing."
                    : "Prototype A initial set complete.\nThis is an editor-only P0 comparison path.",
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

        private void DrawEndpoint(
            string pairId,
            GridCell cell,
            int endpointNumber,
            Rect boardRect,
            float cellSize)
        {
            var rect = GetCellRect(cell, boardRect, cellSize);
            var outerInset = cellSize * 0.11f;
            rect.x += outerInset;
            rect.y += outerInset;
            rect.width -= outerInset * 2f;
            rect.height -= outerInset * 2f;

            var previous = GUI.backgroundColor;
            GUI.backgroundColor = PairColor(pairId);
            GUI.Box(rect, string.Empty);

            var inner = rect;
            var innerInset = Mathf.Max(4f, cellSize * 0.08f);
            inner.x += innerInset;
            inner.y += innerInset;
            inner.width -= innerInset * 2f;
            inner.height -= innerInset * 2f;
            GUI.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
            GUI.Box(inner, string.Empty);
            GUI.backgroundColor = previous;

            var markerStyle = CenteredStyle(Mathf.Clamp((int)(cellSize * 0.27f), 16, 24), FontStyle.Bold);
            GUI.Label(inner, EnergyRoutingPresentation.EndpointLabel(pairId, endpointNumber), markerStyle);
        }

        private float GetCellSize()
        {
            var widthFit = (PrototypeGuiLayout.Width - 40f) / payload.Width;
            var availableHeight = Mathf.Max(150f, PrototypeGuiLayout.Height - GetBoardTop() - BoardBottomReserve);
            var heightFit = availableHeight / payload.Height;
            return Mathf.Clamp(Mathf.Min(widthFit, heightFit), 32f, 86f);
        }

        private Rect GetBoardRect(float cellSize)
        {
            var width = payload.Width * cellSize;
            var height = payload.Height * cellSize;
            var boardTop = GetBoardTop();
            var availableHeight = Mathf.Max(height, PrototypeGuiLayout.Height - boardTop - BoardBottomReserve);
            return new Rect(
                (PrototypeGuiLayout.Width - width) * 0.5f,
                boardTop + (availableHeight - height) * 0.5f,
                width,
                height);
        }

        private static float GetBoardTop()
        {
            return PrototypeGuiLayout.Height < 650f ? CompactBoardTop : PortraitBoardTop;
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

        private EnergyRoutingPair FindPairAtEndpoint(GridCell cell)
        {
            foreach (var pair in payload.Pairs)
            {
                if (pair.Start.Equals(cell) || pair.End.Equals(cell))
                {
                    return pair;
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

        private bool IsOccupiedByAnotherPath(string pairId, GridCell cell)
        {
            foreach (var pair in payload.Pairs)
            {
                if (pair.Id == pairId)
                {
                    continue;
                }

                var path = runner.GetPath(pair.Id);
                for (var index = 0; index < path.Count; index++)
                {
                    if (path[index].Equals(cell))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private int ConnectedPairCount()
        {
            var count = 0;
            foreach (var pair in payload.Pairs)
            {
                if (runner.GetPath(pair.Id).Count > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private string BuildLegend()
        {
            var pairIds = new List<string>();
            foreach (var pair in payload.Pairs)
            {
                pairIds.Add(pair.Id);
            }

            return EnergyRoutingPresentation.Legend(pairIds, payload.BlockedCells.Count > 0);
        }

        private static string FriendlyInvalidReason(string reason, string targetLabel)
        {
            if (reason == EnergyRoutingInvalidReason.Blocked)
            {
                return "X is blocked. Route around it.";
            }
            if (reason == EnergyRoutingInvalidReason.Crossing)
            {
                return "Paths cannot cross or share squares.";
            }
            if (reason == EnergyRoutingInvalidReason.SelfIntersection)
            {
                return "A route cannot loop through the same square.";
            }
            if (reason == EnergyRoutingInvalidReason.NonContiguous)
            {
                return "Drag through side-touching squares, not diagonally.";
            }
            if (reason == EnergyRoutingInvalidReason.WrongTarget)
            {
                return "Finish on the matching label, not another pair.";
            }
            if (reason == EnergyRoutingInvalidReason.BadEndpoint)
            {
                return targetLabel == null
                    ? "Start and finish on matching labels."
                    : $"Keep your finger down until {targetLabel}.";
            }
            return "That route is not valid. Try another path.";
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
                case "red": return new Color(1f, 0.34f, 0.30f);
                case "blue": return new Color(0.30f, 0.62f, 1f);
                case "green": return new Color(0.30f, 0.88f, 0.48f);
                case "yellow": return new Color(1f, 0.82f, 0.24f);
                default: return new Color(0.78f, 0.48f, 1f);
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
