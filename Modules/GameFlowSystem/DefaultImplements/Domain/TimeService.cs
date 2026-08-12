using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.StaticData;
using KahaGameCore.Foundation.Messaging;
using KahaGameCore.GameFlowSystem;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.GameFlowSystem.DefaultImplements.Events;
using KahaGameCore.Parameters;
using KahaGameCore.Persistence;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    public class TimeService :
        ITimeService,
        ISaveParticipant<TimeServiceSnapshot>
    {
        public const string DayParameterKey = "Day";
        public const string SaveParticipantKey = "GameFlow.CurrentPhase";

        public TimePhaseData CurrentPhase { get; private set; }
        public int CurrentDay => parameters.GetInt(DayParameterKey);
        public string SaveKey => SaveParticipantKey;

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

        public void AdvancePhase()
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
                throw new InvalidOperationException(
                    $"[TimeService] 找不到階段 Key={phaseKey}。");
            }

            ApplyPhase(targetPhase, isNewDayCounted: false);
        }

        public TimeServiceSnapshot Capture()
        {
            if (CurrentPhase == null)
            {
                throw new InvalidOperationException(
                    "[TimeService] 尚未初始化目前階段，無法建立存檔快照。");
            }

            return new TimeServiceSnapshot
            {
                CurrentPhaseKey = CurrentPhase.Key
            };
        }

        public void Restore(TimeServiceSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            SetPhase(snapshot.CurrentPhaseKey);
        }

        private void ApplyPhase(TimePhaseData phase, bool isNewDayCounted)
        {
            CurrentPhase = phase;

            if (isNewDayCounted && phase.IsNewDay == 1)
            {
                parameters.Add(DayParameterKey, 1);
            }

            MessageBus.Publish(new TimePhaseChangedEvent(phase, CurrentDay));
        }
    }
}
