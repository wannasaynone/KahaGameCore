using KahaGameCore.Package.EffectProcessor;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_HideStoryCG : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new HideStoryCG();
        }
    }

    public class HideStoryCG : EffectCommandBase
    {
        public override void Process(string[] vars, System.Action onCompleted, System.Action onForceQuit)
        {
            Utlity.GeneralBlackScreen.Instance.HideStoryCGImage(delegate
            {
                onCompleted?.Invoke();
            });
        }
    }
}