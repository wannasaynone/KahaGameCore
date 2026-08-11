using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem
{
    /// <summary>
    /// 表驅動遊戲主流程。流程骨架固定為：
    ///   開新遊戲 → GameStart 事件 → ┐
    ///   ┌──────────────────────────┘
    ///   │ 階段開始 → PhaseStart 事件 → 行動選擇 → Action TriggerTiming → AfterAction 事件 → …
    ///   └─ 階段切換（由表中指令推動）後回到階段開始
    /// 所有劇情、條件與數值變化都由各專案的表格定義，本類別不含任何劇情內容。
    /// </summary>
    public class GameFlowController
    {
        private readonly IGameFlowTimeService timeService;
        private readonly IGameFlowLocationService locationService;
        private readonly IGameFlowActionProvider actionProvider;
        private readonly IGameFlowEventTriggerService triggerService;
        private readonly IActionMenuPresenter actionMenuPresenter;
        private readonly HashSet<int> automaticallyAdvancedPhaseIds = new HashSet<int>();

        /// <summary>最後一次已觸發 EnterLocation 事件的地點，用於偵測指令造成的移動。</summary>
        private int lastEnteredLocationId;

        public GameFlowController(
            IGameFlowTimeService timeService,
            IGameFlowLocationService locationService,
            IGameFlowActionProvider actionProvider,
            IGameFlowEventTriggerService triggerService,
            IActionMenuPresenter actionMenuPresenter)
        {
            this.timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            this.locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            this.actionProvider = actionProvider ?? throw new ArgumentNullException(nameof(actionProvider));
            this.triggerService = triggerService ?? throw new ArgumentNullException(nameof(triggerService));
            this.actionMenuPresenter = actionMenuPresenter ?? throw new ArgumentNullException(nameof(actionMenuPresenter));
        }

        public async UniTask RunNewGameAsync(CancellationToken token)
        {
            // 開新局狀態（Parameters / TimeService / LocationService）由組裝根在呼叫前重置。
            lastEnteredLocationId = locationService.CurrentLocationID;
            automaticallyAdvancedPhaseIds.Clear();

            await triggerService.RaiseTimingAsync(GameFlowTimings.GameStart, token);
            await RaiseLocationTimingsIfMovedAsync(token);

            while (!token.IsCancellationRequested)
            {
                await RunPhaseAsync(token);
            }
        }

        private async UniTask RunPhaseAsync(CancellationToken token)
        {
            IGameFlowTimePhase phase = timeService.CurrentPhase;

            await triggerService.RaiseTimingAsync(GameFlowTimings.PhaseStart, token);
            if (token.IsCancellationRequested || timeService.CurrentPhase.ID != phase.ID)
            {
                return;
            }

            await triggerService.RaiseTimingAsync(GameFlowTimings.PhaseStartFor(phase.Key), token);
            await RaiseLocationTimingsIfMovedAsync(token);

            // 事件指令（SetPhase 等）可能已切換階段，直接進入新階段。
            if (token.IsCancellationRequested || timeService.CurrentPhase.ID != phase.ID)
            {
                return;
            }

            while (!token.IsCancellationRequested && timeService.CurrentPhase.ID == phase.ID)
            {
                IReadOnlyList<ActionMenuEntry> entries = BuildActionMenuEntries();
                if (!entries.Any(entry => entry.IsEnabled))
                {
                    AdvancePhaseOrThrowCycle(phase);
                    return;
                }

                automaticallyAdvancedPhaseIds.Clear();
                await RunActionRoundAsync(entries, token);
            }
        }

        private async UniTask RunActionRoundAsync(
            IReadOnlyList<ActionMenuEntry> entries,
            CancellationToken token)
        {
            IGameFlowAction chosenAction = await actionMenuPresenter.SelectActionAsync(entries);
            if (chosenAction == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(chosenAction.TriggerTiming))
            {
                throw new InvalidOperationException(
                    $"[GameFlowController] Action {chosenAction.ID} 的 TriggerTiming 不可為空白。");
            }

            await triggerService.RaiseTimingAsync(chosenAction.TriggerTiming, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            await triggerService.RaiseTimingAsync(GameFlowTimings.AfterAction, token);
            await RaiseLocationTimingsIfMovedAsync(token);
        }

        private void AdvancePhaseOrThrowCycle(IGameFlowTimePhase phase)
        {
            if (!automaticallyAdvancedPhaseIds.Add(phase.ID))
            {
                throw new InvalidOperationException(
                    $"[GameFlowController] 無可用 Action 的 Phase 自動推進形成循環；Phase {phase.Key} ({phase.ID}) 再次出現。");
            }

            Debug.LogWarning(
                $"[GameFlowController] 地點 {locationService.CurrentLocationID} 於 Phase {phase.Key} 沒有 enabled Action，自動推進 Phase。");
            timeService.AdvancePhase();
        }

        private IReadOnlyList<ActionMenuEntry> BuildActionMenuEntries()
        {
            return actionProvider
                .GetVisibleActions(locationService.CurrentLocationID)
                .Select(action => new ActionMenuEntry(action, actionProvider.IsEnabled(action)))
                .ToList();
        }

        /// <summary>
        /// 指令（移動類）移動地點後，補發 EnterLocation 事件。
        /// 事件本身又可能再移動地點（例如被送回家），因此以迴圈處理直到穩定。
        /// </summary>
        private async UniTask RaiseLocationTimingsIfMovedAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && locationService.CurrentLocationID != lastEnteredLocationId)
            {
                lastEnteredLocationId = locationService.CurrentLocationID;
                await triggerService.RaiseTimingAsync(GameFlowTimings.EnterLocation(lastEnteredLocationId), token);
            }
        }
    }
}
