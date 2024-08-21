using UnityEngine;

namespace KahaGameCore.Package.PlayerControlable
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class InteractableObject : MonoBehaviour
    {
        public string InteractTargetTag => interactTargetTag;
        [SerializeField] private string interactTargetTag = "DefaultInteractableObject";

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null)
            {
                return;
            }

            InteractableObjectTrigger interactableObjectTrigger = collision.GetComponent<InteractableObjectTrigger>();
            if (interactableObjectTrigger != null)
            {
                interactableObjectTrigger.AddInteractableObject(this);
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision == null)
            {
                return;
            }

            InteractableObjectTrigger interactableObjectTrigger = collision.GetComponent<InteractableObjectTrigger>();
            if (interactableObjectTrigger != null)
            {
                interactableObjectTrigger.RemoveInteractableObject(this);
            }
        }
    }
}