using UnityEngine;

namespace KahaGameCore.Interaction
{
    /// <summary>
    /// 3D interactable. Its Collider is the interaction volume and is made a
    /// trigger on Awake. Add the concrete collider first: Collider is abstract,
    /// so Unity cannot add it for you.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("KahaGameCore/Interaction/Interactable3D")]
    public sealed class Interactable3D : InteractableBase<Collider>
    {
        private Collider volume;

        protected override Vector3 FocusPoint
        {
            get
            {
                if (volume == null)
                {
                    volume = GetComponent<Collider>();
                }

                return volume != null ? volume.bounds.center : transform.position;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            volume = GetComponent<Collider>();
            volume.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            NotifyEntered(other);
        }

        private void OnTriggerExit(Collider other)
        {
            NotifyExited(other);
        }
    }
}
