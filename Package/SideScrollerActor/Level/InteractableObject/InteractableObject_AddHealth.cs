using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Level.InteractableObject
{
    public class InteractableObject_AddHealth : InteractableObject
    {
        public int add = 30;

        [SerializeField] private AudioClip interactSound;

        protected override void Exit()
        {
        }

        protected override void Interact()
        {
            if (interactSound != null)
            {
                Audio.AudioManager.Instance.PlaySound(interactSound);
            }
            actor.AddHealth(add);
            Destroy(gameObject);
        }
    }
}