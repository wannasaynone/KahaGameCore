using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.GameData.Implemented;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Events;
using KahaGameCore.Package.GameFlowSystem.Samples.Views;

namespace KahaGameCore.Package.GameFlowSystem.Samples.Presenters
{
    /// <summary>
    /// 監聽遊戲狀態事件並更新 HUD。
    /// HUD 上顯示哪些數值由 GameValueData 表的 ShowInHUD 欄位決定。
    /// </summary>
    public class GameplayHudPresenter : IDisposable
    {
        private readonly GameplayHudView view;
        private readonly IGameState gameState;
        private readonly ITimeService timeService;
        private readonly List<GameValueData> hudValueDefinitions;

        public GameplayHudPresenter(
            GameplayHudView view,
            GameStaticDataManager staticDataManager,
            IGameState gameState,
            ITimeService timeService)
        {
            this.view = view ? view : throw new ArgumentNullException(nameof(view));
            this.gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            this.timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));

            GameValueData[] definitions = staticDataManager.GetAllGameData<GameValueData>();
            hudValueDefinitions = definitions == null
                ? new List<GameValueData>()
                : definitions.Where(definition => definition.ShowInHUD == 1).OrderBy(definition => definition.ID).ToList();

            EventBus.Subscribe<GameValueChangedEvent>(OnGameValueChanged);
            EventBus.Subscribe<TimePhaseChangedEvent>(OnTimePhaseChanged);
            EventBus.Subscribe<MonologueRequestedEvent>(OnMonologueRequested);
        }

        /// <summary>開新遊戲時重建狀態列。</summary>
        public void Refresh()
        {
            view.BindStats(hudValueDefinitions
                .Select(definition => (definition.Tag, definition.DisplayName, gameState.Get(definition.Tag)))
                .ToList());

            UpdateDayPhaseText();
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<GameValueChangedEvent>(OnGameValueChanged);
            EventBus.Unsubscribe<TimePhaseChangedEvent>(OnTimePhaseChanged);
            EventBus.Unsubscribe<MonologueRequestedEvent>(OnMonologueRequested);
        }

        private void OnGameValueChanged(GameValueChangedEvent changedEvent)
        {
            view.TryUpdateStat(changedEvent.Tag, changedEvent.NewValue);
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
