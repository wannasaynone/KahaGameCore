using KahaGameCore.Package.EffectProcessor;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_StopBGM : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new StopBGM();
        }
    }


    public class StopBGM : EffectCommandBase
    {
        public override void Process(string[] vars, System.Action onCompleted, System.Action onForceQuit)
        {
            Audio.AudioManager.Instance.StopBGM();
            onCompleted?.Invoke();
        }
    }
}