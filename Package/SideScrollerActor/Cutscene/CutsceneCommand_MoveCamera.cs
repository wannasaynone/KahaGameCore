using System;
using DG.Tweening;
using KahaGameCore.Package.EffectProcessor;
using KahaGameCore.Package.SideScrollerActor.Camera;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_MoveCamera : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new MoveCamera();
        }
    }

    public class MoveCamera : EffectCommandBase
    {
        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            CameraController.Instance.transform.DOMoveX(CameraController.Instance.transform.position.x + float.Parse(vars[0]), float.Parse(vars[1]))
                .OnComplete(() =>
                {
                    onCompleted?.Invoke();
                });
        }
    }
}