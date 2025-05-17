using KahaGameCore.Package.EffectProcessor;
using KahaGameCore.Package.SideScrollerActor.Utlity;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_CutInText : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new CutInText();
        }
    }

    public class CutInText : EffectCommandBase
    {
        public override void Process(string[] vars, System.Action onCompleted, System.Action onForceQuit)
        {
            GeneralBlackScreen.Instance.ShowCutInText(ContextHandler.Instance.GetContext(int.Parse(vars[0])), delegate
            {
                onCompleted?.Invoke();
            });
        }
    }
}