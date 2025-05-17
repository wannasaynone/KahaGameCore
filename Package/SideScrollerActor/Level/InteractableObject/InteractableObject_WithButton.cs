using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Level.InteractableObject
{
    public class InteractableObject_WithButton : InteractableObject
    {
        [SerializeField] private KeyCode interactKey = KeyCode.F;
        [SerializeField] private string interactedCommand = "Null";

        private bool isInteracting = false;

        protected override void Interact()
        {
            isInteracting = true;
        }

        private void Update()
        {
            if (isInteracting && Input.GetKeyDown(interactKey))
            {
                EventBus.Publish(new InteractableObject_OnInteractedWithButton()
                {
                    interactedCommand = interactedCommand
                });
                isInteracting = false;
            }
        }

        protected override void Exit()
        {
            isInteracting = false;
        }
    }
}

