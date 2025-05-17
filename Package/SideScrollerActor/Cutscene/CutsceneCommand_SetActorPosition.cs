using System;
using KahaGameCore.Package.EffectProcessor;
using KahaGameCore.Package.SideScrollerActor.Gameplay;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_SetActorPosition : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new SetActorPosition();
        }
    }

    public class SetActorPosition : EffectCommandBase
    {
        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            Actor actor = ActorContainer.GetActorByGameObjectName(vars[0]);
            if (actor == null)
            {
                Debug.LogError($"Actor with name {vars[0]} not found.");
                onCompleted?.Invoke();
                return;
            }

            Vector3 position = new Vector3(float.Parse(vars[1]), actor.transform.position.y, actor.transform.position.z);
            actor.transform.position = position;

            onCompleted?.Invoke();
        }
    }
}