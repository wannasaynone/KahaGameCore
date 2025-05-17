using KahaGameCore.Package.SideScrollerActor.Level;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game.Flow.Steps
{
    [CreateAssetMenu(fileName = "ResumeLevelStep", menuName = "Game Flow/Steps/Resume Level Step")]
    public class ResumeLevelStep : GameFlowStep
    {
        public override void Execute(FlowContext context)
        {
            LevelManager.Resume();
            CompleteStep(context);
        }
    }
}
