using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.GameData.Implemented;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    public class GameEventTriggerService : IGameEventTriggerService
    {
        private readonly IGameState gameState;
        private readonly IConditionEvaluator conditionEvaluator;
        private readonly IDialoguePlayer dialoguePlayer;
        private readonly IPerformancePlayer performancePlayer;
        private readonly ICommandExecutor commandExecutor;
        private readonly List<GameEventTriggerData> triggers;

        public GameEventTriggerService(
            GameStaticDataManager staticDataManager,
            IGameState gameState,
            IConditionEvaluator conditionEvaluator,
            IDialoguePlayer dialoguePlayer,
            IPerformancePlayer performancePlayer,
            ICommandExecutor commandExecutor)
        {
            this.gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            this.conditionEvaluator = conditionEvaluator ?? throw new ArgumentNullException(nameof(conditionEvaluator));
            this.dialoguePlayer = dialoguePlayer ?? throw new ArgumentNullException(nameof(dialoguePlayer));
            this.performancePlayer = performancePlayer ?? throw new ArgumentNullException(nameof(performancePlayer));
            this.commandExecutor = commandExecutor ?? throw new ArgumentNullException(nameof(commandExecutor));

            GameEventTriggerData[] loadedTriggers = staticDataManager.GetAllGameData<GameEventTriggerData>();
            triggers = loadedTriggers == null ? new List<GameEventTriggerData>() : loadedTriggers.ToList();
        }

        public async UniTask RaiseTimingAsync(string timing, CancellationToken cancellationToken = default)
        {
            List<GameEventTriggerData> candidates = triggers
                .Where(trigger => IsTimingMatched(trigger.Timing, timing))
                .OrderByDescending(trigger => trigger.Priority)
                .ThenBy(trigger => trigger.ID)
                .ToList();

            foreach (GameEventTriggerData trigger in candidates)
            {
                // 先前事件可能已中止流程（如 ReturnToTitle、Game Over），不再執行後續事件。
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // 先前事件可能改變狀態，因此每個事件執行前都重新驗證。
                if (!CanExecute(trigger))
                {
                    continue;
                }

                // 先累加觸發次數再執行，避免事件內的指令重入同一時機時重複觸發。
                gameState.Add(GameValueTags.EventTriggerCount(trigger.ID), 1);

                await ExecuteAsync(trigger);
            }
        }

        /// <summary>
        /// 表中 Timing 與發出的時機相符即命中；保留字 Any（如 PhaseStart:Any）
        /// 命中同一類別（冒號前綴相同）的所有時機，供「每個時段都要檢查」的事件使用。
        /// </summary>
        private static bool IsTimingMatched(string triggerTiming, string raisedTiming)
        {
            if (triggerTiming == raisedTiming)
            {
                return true;
            }

            int separatorIndex = raisedTiming.IndexOf(':');
            if (separatorIndex < 0)
            {
                return false;
            }

            return triggerTiming == raisedTiming.Substring(0, separatorIndex + 1) + "Any";
        }

        private bool CanExecute(GameEventTriggerData trigger)
        {
            if (trigger.MaxTriggerTimes > 0 &&
                gameState.Get(GameValueTags.EventTriggerCount(trigger.ID)) >= trigger.MaxTriggerTimes)
            {
                return false;
            }

            return conditionEvaluator.Evaluate(trigger.Condition);
        }

        private async UniTask ExecuteAsync(GameEventTriggerData trigger)
        {
            await performancePlayer.PlayAsync(trigger.PrePerformance);

            if (trigger.DialogueID > 0)
            {
                await dialoguePlayer.PlayAsync(trigger.DialogueID);
            }

            await commandExecutor.ExecuteAsync(trigger.Commands);
            await performancePlayer.PlayAsync(trigger.PostPerformance);
        }
    }
}
