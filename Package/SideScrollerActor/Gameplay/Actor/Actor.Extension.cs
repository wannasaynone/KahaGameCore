using KahaGameCore.Package.SideScrollerActor.Gameplay.Extension;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    public partial class Actor
    {
        public void ProcessExtention(string key, object data)
        {
            EndPrepareAttack();
            currentActorExtension = GetActorExtensionByKey(key);

            if (currentActorExtension == null)
            {
                Debug.LogError("No ActorExtension found with key: " + key + " in Actor: " + gameObject.name);
                return;
            }

            state = State.Extension;
            currentActorExtension.Process(data, OnExtensionEnded);
        }

        public void ForceEndCurrentExtension()
        {
            if (currentActorExtension != null)
            {
                currentActorExtension.ForceEnd();
            }
        }

        private ActorExtension GetActorExtensionByKey(string key)
        {
            for (int i = 0; i < actorExtensions.Length; i++)
            {
                if (actorExtensions[i].Key == key)
                {
                    return actorExtensions[i];
                }
            }

            return null;
        }

        private void OnExtensionEnded()
        {
            currentActorExtension = null;
            state = State.Normal;
            SetToIdle();
        }
    }
}
