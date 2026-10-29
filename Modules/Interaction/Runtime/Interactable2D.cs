using UnityEngine;

namespace KahaGameCore.Interaction
{
    /// <summary>
    /// 2D interactable. Its Collider2D is the interaction volume and is made a
    /// trigger on Awake. Add the concrete collider first: Collider2D is abstract,
    /// so Unity cannot add it for you.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
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

        protected override void Awake()
        {
            base.Awake();
            volume = GetComponent<Collider2D>();
            volume.isTrigger = true;
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
