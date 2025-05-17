using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game.Flow.Steps
{
    [CreateAssetMenu(fileName = "LoadLevelStep", menuName = "Game Flow/Steps/Load Level")]
    public class LoadLevelStep : GameFlowStep
    {
        public string levelPath;

        private FlowContext currentContext;

        public override void Execute(FlowContext context)
        {
            currentContext = context;
            EventBus.Subscribe<Game_OnLevelLoaded>(OnLevelLoaded);
            EventBus.Publish(new Game_OnNextLevelRequested
            {
                nextLevelName = levelPath
            });
        }

        private void OnLevelLoaded(Game_OnLevelLoaded e)
        {
            EventBus.Unsubscribe<Game_OnLevelLoaded>(OnLevelLoaded);

            CompleteStep(currentContext);
        }
    }
}
