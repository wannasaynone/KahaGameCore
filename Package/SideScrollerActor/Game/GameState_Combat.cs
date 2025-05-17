using System.Collections.Generic;
using KahaGameCore.GameData.Implemented;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using KahaGameCore.Package.SideScrollerActor.View;

namespace KahaGameCore.Package.SideScrollerActor.Game
{
    public class GameState_Combat : GameStateBase
    {
        private readonly CombatState_LevelController levelController;
        private readonly CombatState_GameStartFlowController gameStartFlowController;
        private readonly CombatState_GameEndFlowController gameEndFlowController;

        public GameState_Combat(InGameView inGameView, GameEndView gameEndView, TitleView titleView, List<OnItemRecordChanged> itemRecord, GameStaticDataManager gameStaticDataManager)
        {
            levelController = new CombatState_LevelController(inGameView, itemRecord, gameStaticDataManager);
            gameStartFlowController = new CombatState_GameStartFlowController(levelController, inGameView, titleView);
            gameEndFlowController = new CombatState_GameEndFlowController(levelController, inGameView, gameEndView, EndCombatState);
        }

        protected override void OnStart()
        {
            levelController.StartListen();
            gameStartFlowController.StartListen();
            gameEndFlowController.StartListen();
        }

        protected override void OnEnd()
        {
            levelController.StopListen();
            gameStartFlowController.StopListen();
            gameEndFlowController.StopListen();
        }

        private void EndCombatState()
        {
            End();
        }
    }
}