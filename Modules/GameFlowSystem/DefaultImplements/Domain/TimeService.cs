using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.StaticData;
using KahaGameCore.GameEvent;
using KahaGameCore.GameFlowSystem;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.GameFlowSystem.DefaultImplements.Events;
using KahaGameCore.Parameters;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    public class TimeService : ITimeService
    {
        public const string DayParameterKey = "Day";

        public TimePhaseData CurrentPhase { get; private set; }
        public int CurrentDay => parameters.GetInt(DayParameterKey);

        IGameFlowTimePhase IGameFlowTimeService.CurrentPhase => CurrentPhase;

        private readonly ParameterStore parameters;
        private readonly List<TimePhaseData> phases;

        public TimeService(GameStaticDataManager staticDataManager, ParameterStore parameters)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

            phases = LoadPhases(staticDataManager);
        }

        private static List<TimePhaseData> LoadPhases(GameStaticDataManager staticDataManager)
        {
            TimePhaseData[] loadedPhases = staticDataManager.GetAllGameData<TimePhaseData>();
            if (loadedPhases == null || loadedPhases.Length == 0)
            {
                throw new InvalidOperationException("[TimeService] TimePhaseData 表未載入或為空。");
            }

            return loadedPhases.OrderBy(phase => phase.ID).ToList();
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
                parameters.Add(DayParameterKey, 1);
            }

            EventBus.Publish(new TimePhaseChangedEvent(phase, CurrentDay));
        }
    }
}
