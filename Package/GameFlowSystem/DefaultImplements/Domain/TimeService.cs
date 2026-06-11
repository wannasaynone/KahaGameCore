using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.GameData.Implemented;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.GameFlowSystem;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Events;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    public class TimeService : ITimeService
    {
        public TimePhaseData CurrentPhase { get; private set; }
        public int CurrentDay => gameState.Get(GameValueTags.Day);

        IGameFlowTimePhase IGameFlowTimeService.CurrentPhase => CurrentPhase;

        private readonly IGameState gameState;
        private readonly List<TimePhaseData> phases;
        private readonly List<GameValueData> decayingValues;

        public TimeService(GameStaticDataManager staticDataManager, IGameState gameState)
        {
            this.gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));

            TimePhaseData[] loadedPhases = staticDataManager.GetAllGameData<TimePhaseData>();
            if (loadedPhases == null || loadedPhases.Length == 0)
            {
                throw new InvalidOperationException("[TimeService] TimePhaseData 表未載入或為空。");
            }

            phases = loadedPhases.OrderBy(phase => phase.ID).ToList();

            GameValueData[] valueDefinitions = staticDataManager.GetAllGameData<GameValueData>();
            decayingValues = (valueDefinitions ?? System.Array.Empty<GameValueData>())
                .Where(definition => definition.PhaseDecay != 0)
                .ToList();
        }

        public void ResetToFirstPhase()
        {
            ApplyPhase(phases[0], isNewDayCounted: false);
        }

        public void AdvanceTime()
        {
            TimePhaseData nextPhase = phases.Find(phase => phase.ID == CurrentPhase.NextID);
            if (nextPhase == null)
            {
                Debug.LogError($"[TimeService] 找不到階段 ID={CurrentPhase.NextID}（由 {CurrentPhase.Key} 的 NextID 指定）。");
                return;
            }

            ApplyPhase(nextPhase, isNewDayCounted: true);
            ApplyPhaseDecay();
        }

        public void SetPhase(string phaseKey)
        {
            TimePhaseData targetPhase = phases.Find(phase => phase.Key == phaseKey);
            if (targetPhase == null)
            {
                Debug.LogError($"[TimeService] 找不到階段 Key={phaseKey}。");
                return;
            }

            ApplyPhase(targetPhase, isNewDayCounted: false);
        }

        /// <summary>
        /// 時段自然消耗：只在 AdvanceTime（時間自然流逝）時套用，
        /// SetPhase 屬於劇情跳轉（交易返家、甜蜜的一天等），事件自行定義數值結果，不額外扣除。
        /// </summary>
        private void ApplyPhaseDecay()
        {
            foreach (GameValueData definition in decayingValues)
            {
                gameState.Add(definition.Tag, -definition.PhaseDecay);
            }
        }

        private void ApplyPhase(TimePhaseData phase, bool isNewDayCounted)
        {
            CurrentPhase = phase;

            if (isNewDayCounted && phase.IsNewDay == 1)
            {
                gameState.Add(GameValueTags.Day, 1);
            }

            gameState.Set(GameValueTags.CurrentPhase, phase.ID);
            EventBus.Publish(new TimePhaseChangedEvent(phase, CurrentDay));
        }
    }
}
