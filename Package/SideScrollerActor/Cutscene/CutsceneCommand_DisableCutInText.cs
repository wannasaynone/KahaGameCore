using KahaGameCore.Package.EffectProcessor;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_DisableCutInText : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new DisableCutInText();
        }
    }

    public class DisableCutInText : EffectCommandBase
    {
        public override void Process(string[] vars, System.Action onCompleted, System.Action onForceQuit)
        {
            Utlity.GeneralBlackScreen.Instance.HideCutInText(delegate
            {
                onCompleted?.Invoke();
            });
        }
    }
}