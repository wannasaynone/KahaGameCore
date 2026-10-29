using UnityEngine;

namespace KahaGameCore.Interaction
{
    /// <summary>3D interactable. Needs a trigger Collider on the same GameObject.</summary>
    [DisallowMultipleComponent]
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
