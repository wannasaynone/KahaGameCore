using System;
using KahaGameCore.Package.EffectProcessor;
using KahaGameCore.Package.SideScrollerActor.Gameplay;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_PlayAnimation : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new PlayAnimation();
        }
    }

    public class PlayAnimation : EffectCommandBase
    {
        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            Actor actor = ActorContainer.GetActorByGameObjectName(vars[0]);

            if (actor != null)
            {
                actor.Animator.Play(vars[1], 0);

                if (vars[2] == "L")
                {
                    actor.SetFacingLeft(true);
                }
                else if (vars[2] == "R")
                {
                    actor.SetFacingRight(true);
                }
                else
                {
                    Debug.Log($"Invalid animation side: {vars[2]}");
                }

                onCompleted?.Invoke();
            }
            else
            {
                Debug.LogError($"Actor not found with name: {vars[0]}");
                onCompleted?.Invoke();
            }
        }
    }
}