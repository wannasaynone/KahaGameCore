using KahaGameCore.Package.EffectProcessor;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_BlackFadeOut : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new BlackFadeOut();
        }
    }

    public class BlackFadeOut : EffectCommandBase
    {
        public override void Process(string[] vars, System.Action onCompleted, System.Action onForceQuit)
        {
            Utlity.GeneralBlackScreen.Instance.FadeOut(delegate
            {
                onCompleted?.Invoke();
            });
        }
    }
}