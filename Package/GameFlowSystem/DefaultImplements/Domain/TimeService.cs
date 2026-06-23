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

        public TimeService(GameStaticDataManager staticDataManager, IGameState gameState)
        {
            this.gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));

            TimePhaseData[] loadedPhases = staticDataManager.GetAllGameData<TimePhaseData>();
            if (loadedPhases == null || loadedPhases.Length == 0)
            {
                throw new InvalidOperationException("[TimeService] TimePhaseData 表未載入或為空。");
            }

            phases = loadedPhases.OrderBy(phase => phase.ID).ToList();
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
