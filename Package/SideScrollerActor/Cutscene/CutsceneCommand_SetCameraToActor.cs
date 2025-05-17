using System;
using DG.Tweening;
using KahaGameCore.Package.EffectProcessor;
using KahaGameCore.Package.SideScrollerActor.Gameplay;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_SetCameraToActor : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new SetCameraToActor();
        }
    }

    public class SetCameraToActor : EffectCommandBase
    {
        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            Actor actor = ActorContainer.GetActorByGameObjectName(vars[0]);

            if (actor != null)
            {
                float moveTime = float.Parse(vars[1]);

                Camera.CameraController.Instance.transform.DOMoveX(actor.transform.position.x, moveTime).OnComplete(delegate
                {
                    onCompleted?.Invoke();
                });
            }
            else
            {
                Debug.LogError($"Actor with name {vars[0]} not found.");
                onCompleted?.Invoke();
            }
        }
    }
}