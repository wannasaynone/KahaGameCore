using KahaGameCore.Package.SideScrollerActor.Gameplay;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Level.InteractableObject
{
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class InteractableObject : MonoBehaviour
    {
        [SerializeField] private string detectTag = "Player";

        protected Actor actor;

        private void Start()
        {
            BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
            boxCollider.isTrigger = true;
            Rigidbody2D rigidbody = GetComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.CompareTag(detectTag))
            {
                actor = collision.gameObject.GetComponent<Actor>();
                if (actor != null) Interact();
            }
        }

        protected abstract void Interact();

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.gameObject.CompareTag(detectTag))
            {
                actor = collision.gameObject.GetComponent<Actor>();
                if (actor != null) Exit();
            }
        }

        protected abstract void Exit();
    }
}