using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.GameEvent;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.GameFlowSystem.DefaultImplements.Events;
using KahaGameCore.Parameters;

namespace KahaGameCore.GameFlowSystem.DefaultViews
{
    /// <summary>
    /// 監聽 Parameter 與流程事件並更新 HUD。
    /// HUD 上顯示哪些 Parameter 由 composition root 明列。
    /// </summary>
    public class GameplayHudPresenter : IDisposable
    {
        private readonly GameplayHudView view;
        private readonly ParameterStore parameters;
        private readonly ITimeService timeService;
        private readonly List<ParameterDefinition> hudParameterDefinitions;

        public GameplayHudPresenter(
            GameplayHudView view,
            ParameterStore parameters,
            IReadOnlyList<ParameterDefinition> hudParameterDefinitions,
            ITimeService timeService)
        {
            this.view = view ? view : throw new ArgumentNullException(nameof(view));
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            this.hudParameterDefinitions = hudParameterDefinitions == null
                ? throw new ArgumentNullException(nameof(hudParameterDefinitions))
                : hudParameterDefinitions.ToList();

            parameters.Changed += OnParameterChanged;
            EventBus.Subscribe<TimePhaseChangedEvent>(OnTimePhaseChanged);
            EventBus.Subscribe<MonologueRequestedEvent>(OnMonologueRequested);
        }

        /// <summary>開新遊戲時重建狀態列。</summary>
        public void Refresh()
        {
            view.BindStats(hudParameterDefinitions
                .Select(definition => (definition.Key, definition.DisplayName, parameters.GetInt(definition.Key)))
                .ToList());

            UpdateDayPhaseText();
        }

        public void Dispose()
        {
            parameters.Changed -= OnParameterChanged;
            EventBus.Unsubscribe<TimePhaseChangedEvent>(OnTimePhaseChanged);
            EventBus.Unsubscribe<MonologueRequestedEvent>(OnMonologueRequested);
        }

        private void OnParameterChanged(ParameterChanged changed)
        {
            if (hudParameterDefinitions.Any(definition => definition.Key == changed.Key))
            {
                view.TryUpdateStat(changed.Key, changed.NewValue.AsInt());
            }
        }

        private void OnTimePhaseChanged(TimePhaseChangedEvent changedEvent)
        {
            UpdateDayPhaseText();
        }

        private void OnMonologueRequested(MonologueRequestedEvent requestedEvent)
        {
            view.ShowMonologue(requestedEvent.Text);
        }

        private void UpdateDayPhaseText()
        {
            if (timeService.CurrentPhase == null)
            {
                return;
            }

            view.SetDayPhase($"第 {timeService.CurrentDay} 天　{timeService.CurrentPhase.DisplayName}");
        }
    }
}
