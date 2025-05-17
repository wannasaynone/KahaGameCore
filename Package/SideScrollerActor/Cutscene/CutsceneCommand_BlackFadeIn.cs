using System;
using KahaGameCore.Package.EffectProcessor;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_BlackFadeIn : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new BlackFadeIn();
        }
    }

    public class BlackFadeIn : EffectCommandBase
    {
        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            Utlity.GeneralBlackScreen.Instance.FadeIn(delegate
            {
                onCompleted?.Invoke();
            });
        }
    }
}