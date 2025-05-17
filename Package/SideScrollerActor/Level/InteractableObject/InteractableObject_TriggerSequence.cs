using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Level.InteractableObject
{
    public class InteractableObject_TriggerSequence : InteractableObject
    {
        [SerializeField] private string sequenceName;

        protected override void Exit()
        {

        }

        protected override void Interact()
        {
            gameObject.SetActive(false);
            EventBus.Publish(new Game_TriggerSequence
            {
                sequenceName = sequenceName
            });
        }
    }
}
