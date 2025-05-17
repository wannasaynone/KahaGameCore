using KahaGameCore.Package.SideScrollerActor.Level;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game.Flow.Steps
{
    [CreateAssetMenu(fileName = "PauseLevelStep", menuName = "Game Flow/Steps/Pause Level")]
    public class PauseLevelStep : GameFlowStep
    {
        public override void Execute(FlowContext context)
        {
            LevelManager.Pause();

            CompleteStep(context);
        }
    }
}