using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace KahaGameCore.Package.PlayerControlable
{
    [RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
    public class InteractableObjectTrigger : MonoBehaviour
    {
        public event Action<InteractableObject> OnInteractingWith;
        [SerializeField] private UnityEvent<InteractableObject> onInteractingEvent;

        private List<InteractableObject> interactableObjects = new List<InteractableObject>();

        public void AddInteractableObject(InteractableObject interactableObject)
        {
            if (!interactableObjects.Contains(interactableObject))
            {
                interactableObjects.Add(interactableObject);
            }
        }

        public void RemoveInteractableObject(InteractableObject interactableObject)
        {
            if (interactableObjects.Contains(interactableObject))
            {
                interactableObjects.Remove(interactableObject);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                OnInteracting();
            }
        }

        private void OnInteracting()
        {
            if (interactableObjects.Count == 0)
            {
                return;
            }

            OnInteractingWith?.Invoke(interactableObjects[interactableObjects.Count - 1]);
            onInteractingEvent?.Invoke(interactableObjects[interactableObjects.Count - 1]);
        }
    }
}