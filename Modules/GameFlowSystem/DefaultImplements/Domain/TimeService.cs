using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.StaticData;
using KahaGameCore.Foundation.Messaging;
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
        public const string PhaseParameterKey = "CurrentPhase";

        public TimePhaseData CurrentPhase { get; private set; }
        public int CurrentDay => parameters.GetInt(DayParameterKey);

        IGameFlowTimePhase IGameFlowTimeService.CurrentPhase => CurrentPhase;

        private readonly ParameterStore parameters;
        private readonly List<TimePhaseData> phases;

        public TimeService(GameStaticDataManager staticDataManager, ParameterStore parameters)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

            phases = LoadPhases(staticDataManager);

            // ponytail: never unsubscribed; TimeService and its ParameterStore are
            // built and dropped together by the composition root, so the handler
            // cannot outlive the store. Add IDisposable if that stops being true.
            this.parameters.Changed += OnParameterChanged;
            SyncPhaseFromParameter(publish: false);
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
            if (CurrentPhase == null)
            {
                throw new InvalidOperationException(
                    "[TimeService] 尚未初始化目前階段，無法推進。");
            }

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
            ApplyPhase(FindPhase(phaseKey), isNewDayCounted: false);
        }

        private TimePhaseData FindPhase(string phaseKey)
        {
            TimePhaseData targetPhase = phases.Find(phase => phase.Key == phaseKey);
            if (targetPhase == null)
            {
                throw new InvalidOperationException(
                    $"[TimeService] 找不到階段 Key={phaseKey}。");
            }

            return targetPhase;
        }

        private void ApplyPhase(TimePhaseData phase, bool isNewDayCounted)
        {
            if (isNewDayCounted && phase.IsNewDay == 1)
            {
                parameters.Add(DayParameterKey, 1);
            }

            // Writing the parameter is the single source of truth; the Changed
            // handler resolves the phase and publishes TimePhaseChangedEvent,
            // so a restored save takes the same path as a live phase change.
            parameters.Set(PhaseParameterKey, phase.Key);
        }

        private void OnParameterChanged(ParameterChanged change)
        {
            if (!string.Equals(change.Key, PhaseParameterKey, StringComparison.Ordinal))
            {
                return;
            }

            SyncPhaseFromParameter(publish: true);
        }

        private void SyncPhaseFromParameter(bool publish)
        {
            string phaseKey = parameters.GetString(PhaseParameterKey);
            if (string.IsNullOrEmpty(phaseKey))
            {
                CurrentPhase = null;
                return;
            }

            CurrentPhase = FindPhase(phaseKey);
            if (publish)
            {
                MessageBus.Publish(new TimePhaseChangedEvent(CurrentPhase, CurrentDay));
            }
        }
    }
}
