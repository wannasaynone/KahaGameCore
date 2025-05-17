using System;
using DG.Tweening;
using KahaGameCore.Package.EffectProcessor;
using KahaGameCore.Package.SideScrollerActor.Camera;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_SetCamera : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new SetCamera();
        }
    }

    public class SetCamera : EffectCommandBase
    {
        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            CameraController.Instance.transform.DOMoveX(float.Parse(vars[0]), float.Parse(vars[1])).OnComplete
            (
                delegate
                {
                    onCompleted?.Invoke();
                }
            );
        }
    }
}