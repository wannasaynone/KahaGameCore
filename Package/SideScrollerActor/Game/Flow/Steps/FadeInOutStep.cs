using KahaGameCore.Package.SideScrollerActor.Utlity;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game.Flow
{
    [CreateAssetMenu(fileName = "FadeInOutStep", menuName = "Game Flow/Steps/Fade In Out")]
    public class FadeInOutStep : GameFlowStep
    {
        public enum FadeType { FadeIn, FadeOut }

        [SerializeField] private FadeType fadeType = FadeType.FadeIn;

        public override void Execute(FlowContext context)
        {
            switch (fadeType)
            {
                case FadeType.FadeIn:
                    GeneralBlackScreen.Instance.FadeIn(() => CompleteStep(context));
                    break;
                case FadeType.FadeOut:
                    GeneralBlackScreen.Instance.FadeOut(() => CompleteStep(context));
                    break;
                default:
                    Debug.LogError("Invalid fade type specified.");
                    CompleteStep(context);
                    break;
            }
        }
    }
}