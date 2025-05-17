using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.Game.Flow;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using KahaGameCore.Package.SideScrollerActor.View;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game
{
    public class CombatState_GameStartFlowController
    {
        private readonly CombatState_LevelController levelController;
        private readonly InGameView inGameView;
        private readonly TitleView titleView;
        private GameFlowSequence customGameFlow;

        public CombatState_GameStartFlowController(CombatState_LevelController levelController, InGameView inGameView, TitleView titleView)
        {
            this.levelController = levelController;
            this.inGameView = inGameView;
            this.titleView = titleView;
        }

        public void StartListen()
        {
            EventBus.Subscribe<Game_TriggerSequence>(OnGameTriggerSequence);
        }

        public void StopListen()
        {
            EventBus.Unsubscribe<Game_TriggerSequence>(OnGameTriggerSequence);
        }

        private void StartProcess(GameFlowSequence customGameFlow)
        {
            if (this.customGameFlow != null)
            {
                Debug.LogError("Game flow is already in progress. Cannot start a new one.");
                return;
            }

            EventBus.Unsubscribe<Game_TriggerSequence>(OnGameTriggerSequence);
            this.customGameFlow = customGameFlow;

            if (this.customGameFlow != null)
            {
                // Use custom flow sequence
                Debug.Log($"Starting custom game flow: {this.customGameFlow.name}");

                this.customGameFlow.OnSequenceCompleted += OnGameFlowCompleted;

                // Create context with necessary references
                FlowContext context = new FlowContext
                {
                    LevelController = levelController,
                    InGameView = inGameView,
                    TitleView = titleView
                };

                // Execute the sequence
                this.customGameFlow.ExecuteSequence(context);
            }
        }

        private void OnGameFlowCompleted()
        {
            if (customGameFlow != null)
            {
                customGameFlow.OnSequenceCompleted -= OnGameFlowCompleted;
                customGameFlow = null;
            }

            EventBus.Subscribe<Game_TriggerSequence>(OnGameTriggerSequence);
        }

        private void OnGameTriggerSequence(Game_TriggerSequence e)
        {
            GameFlowSequence gameFlow = Resources.Load<GameFlowSequence>(e.sequenceName);

            if (gameFlow == null)
            {
                Debug.LogError($"Game flow sequence '{e.sequenceName}' not found.");
                return;
            }

            StartProcess(gameFlow);
        }
    }
}
