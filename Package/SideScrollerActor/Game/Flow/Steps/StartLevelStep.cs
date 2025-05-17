using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game.Flow.Steps
{
    [CreateAssetMenu(fileName = "StartLevelStep", menuName = "Game Flow/Steps/Start Level")]
    public class StartLevelStep : GameFlowStep
    {
        public override void Execute(FlowContext context)
        {
            if (context.LevelController != null)
            {
                context.LevelController.StartLevel();
                CompleteStep(context);
            }
            else
            {
                Debug.LogError("Cannot start level: LevelController is null");
                CompleteStep(context);
            }
        }
    }
}
