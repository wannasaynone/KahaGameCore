using System;
using DG.Tweening;
using KahaGameCore.Package.EffectProcessor;
using KahaGameCore.Package.SideScrollerActor.Gameplay;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_MoveActor : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new MoveActor();
        }
    }

    public class MoveActor : EffectCommandBase
    {
        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            Actor actor = ActorContainer.GetActorByGameObjectName(vars[0]);

            if (actor != null)
            {
                if (vars.Length >= 4 && vars[3] == "T")
                {
                    actor.transform.DOMoveX(float.Parse(vars[1]) + actor.transform.position.x, float.Parse(vars[2])).SetEase(Ease.Linear);
                    onCompleted?.Invoke();
                }
                else
                {
                    actor.transform.DOMoveX(float.Parse(vars[1]) + actor.transform.position.x, float.Parse(vars[2])).SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        onCompleted?.Invoke();
                    });
                }
            }
        }
    }
}
