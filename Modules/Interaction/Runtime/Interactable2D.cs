using UnityEngine;

namespace KahaGameCore.Interaction
{
    /// <summary>2D interactable. Needs a trigger Collider2D on the same GameObject.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("KahaGameCore/Interaction/Interactable2D")]
    public sealed class Interactable2D : InteractableBase<Collider2D>
    {
        private Collider2D volume;

        protected override Vector3 FocusPoint
        {
            get
            {
                if (volume == null)
                {
                    volume = GetComponent<Collider2D>();
                }

                return volume != null ? volume.bounds.center : transform.position;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            NotifyEntered(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            NotifyExited(other);
        }
    }
}
