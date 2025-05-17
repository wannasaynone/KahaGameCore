using KahaGameCore.Package.SideScrollerActor.Cutscene;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game.Flow.Steps
{
    [CreateAssetMenu(fileName = "PlayCutsceneStep", menuName = "Game Flow/Steps/Play Cutscene")]
    public class PlayCutsceneStep : GameFlowStep
    {
        public string cutsceneName;
        public bool skipCutscene = false;

        private FlowContext currentContext;

        public override void Execute(FlowContext context)
        {
            currentContext = context;

            if (skipCutscene)
            {
                CompleteStep(currentContext);
            }
            else
            {
                CutscenePlayer.Instance.Play(cutsceneName, OnCutsceneComplete);
            }
        }

        private void OnCutsceneComplete()
        {
            CompleteStep(currentContext);
        }
    }
}
