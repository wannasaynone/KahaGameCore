using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KahaGameCore.Interaction
{
    /// <summary>
    /// "Stand here and press a key". Tracks activators inside the trigger volume,
    /// raises Entered/Exited for every one of them, and raises Interacted when a
    /// binding's key is pressed while this interactable has the activator's focus.
    ///
    /// Focus: among every interactable an activator is inside, only the nearest
    /// one shows prompts and answers keys; on a tie the one entered last wins.
    ///
    /// Input: a binding answers only while its InputAction is enabled. Pausing
    /// gameplay input is done by disabling the action map; this component never
    /// enables or disables actions itself.
    /// </summary>
    public abstract class InteractableBase<TActivator> : MonoBehaviour
        where TActivator : Component
    {
        private static readonly List<InteractableBase<TActivator>> Active = new();
        private static int nextEntrySequence;

        [Tooltip("Only colliders on these layers can activate this interactable.")]
        [SerializeField] private LayerMask activatorLayers;

        [Tooltip("Keys this interactable answers to. Leave empty for enter-only use (Entered).")]
        [SerializeField] private List<InteractionBinding> bindings = new();

        // Activator -> the global sequence number of its latest entry.
        private readonly Dictionary<TActivator, int> inRange = new();

        public event Action<TActivator> Entered;
        public event Action<TActivator> Exited;

        /// <summary>Binding index and the activator that had focus when the key was pressed.</summary>
        public event Action<int, TActivator> Interacted;

        public LayerMask ActivatorLayers => activatorLayers;
        public IReadOnlyList<InteractionBinding> Bindings => bindings;
        public bool HasBindings => bindings.Count > 0;

        /// <summary>Activators currently inside the volume, in no particular order.</summary>
        public IEnumerable<TActivator> ActivatorsInRange
        {
            get
            {
                foreach (TActivator activator in inRange.Keys)
                {
                    if (activator != null)
                    {
                        yield return activator;
                    }
                }
            }
        }

        /// <summary>
        /// Where focus distance is measured from. Defaults to the pivot; the 2D/3D
        /// subclasses use their trigger volume so pivot conventions do not matter.
        /// </summary>
        protected virtual Vector3 FocusPoint => transform.position;

        // Same as isActiveAndEnabled at runtime, but also true for edit-mode tests
        // where OnEnable is invoked by hand.
        private bool IsLive => enabled && gameObject.activeInHierarchy;

        // Only interactables that can answer a key compete for focus; enter-only
        // volumes and fully disabled ones must not hide a neighbour's prompt.
        private bool AnswersKeys
        {
            get
            {
                if (!IsLive)
                {
                    return false;
                }

                foreach (InteractionBinding binding in bindings)
                {
                    InputAction action = binding.Action;
                    if (binding.Enabled && action != null && action.enabled)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Configure(LayerMask layers, IEnumerable<InteractionBinding> newBindings)
        {
            HideAllPrompts();
            activatorLayers = layers;
            bindings = new List<InteractionBinding>(newBindings ?? Array.Empty<InteractionBinding>());
            HideAllPrompts();
        }

        public void SetBindingEnabled(int index, bool value)
        {
            bindings[index].Enabled = value;
            if (!value)
            {
                bindings[index].ShowPrompt(false);
            }
        }

        /// <summary>True when the activator is inside this interactable and it is the activator's focus.</summary>
        public bool HasFocusOf(TActivator activator)
        {
            if (activator == null || !inRange.TryGetValue(activator, out int sequence))
            {
                return false;
            }

            Vector3 position = activator.transform.position;
            float distance = (FocusPoint - position).sqrMagnitude;
            foreach (InteractableBase<TActivator> other in Active)
            {
                if (other == this || other == null ||
                    !other.inRange.TryGetValue(activator, out int otherSequence) ||
                    !other.AnswersKeys)
                {
                    continue;
                }

                float otherDistance = (other.FocusPoint - position).sqrMagnitude;
                if (otherDistance < distance ||
                    (Mathf.Approximately(otherDistance, distance) && otherSequence > sequence))
                {
                    return false;
                }
            }

            return true;
        }

        protected void NotifyEntered(TActivator activator)
        {
            // Unity sends trigger messages to disabled components too.
            if (!IsLive || !IsActivator(activator))
            {
                return;
            }

            inRange[activator] = ++nextEntrySequence;
            Entered?.Invoke(activator);
        }

        protected void NotifyExited(TActivator activator)
        {
            if (activator == null || !inRange.Remove(activator))
            {
                return;
            }

            Exited?.Invoke(activator);
        }

        protected virtual void Awake()
        {
            HideAllPrompts();
            if (activatorLayers.value == 0)
            {
                Debug.LogWarning(
                    $"Interactable '{name}' has no activator layers, so nothing can activate it.",
                    this);
            }
        }

        protected virtual void OnEnable()
        {
            Active.Remove(this);
            Active.Add(this);
        }

        protected virtual void OnDisable()
        {
            Active.Remove(this);
            // Unity sends no exit when the volume is disabled, and enter again when
            // it is re-enabled, so forget everything now.
            inRange.Clear();
            HideAllPrompts();
        }

        protected virtual void Update()
        {
            RemoveDestroyedActivators();
            for (int index = 0; index < bindings.Count; index++)
            {
                InteractionBinding binding = bindings[index];
                InputAction action = binding.Action;
                TActivator focused = binding.Enabled && action != null && action.enabled
                    ? FindFocusedActivator()
                    : null;
                binding.ShowPrompt(focused != null);

                if (focused != null && action.WasPressedThisFrame())
                {
                    // One interaction per frame: the handler may disable bindings,
                    // this component, or destroy the object.
                    Interacted?.Invoke(index, focused);
                    return;
                }
            }
        }

        private TActivator FindFocusedActivator()
        {
            TActivator latest = null;
            int latestSequence = int.MinValue;
            foreach (KeyValuePair<TActivator, int> pair in inRange)
            {
                if (pair.Value > latestSequence && HasFocusOf(pair.Key))
                {
                    latest = pair.Key;
                    latestSequence = pair.Value;
                }
            }

            return latest;
        }

        private bool IsActivator(TActivator activator)
        {
            return activator != null &&
                   (activatorLayers.value & (1 << activator.gameObject.layer)) != 0;
        }

        private void RemoveDestroyedActivators()
        {
            if (inRange.Count == 0)
            {
                return;
            }

            List<TActivator> destroyed = null;
            foreach (TActivator activator in inRange.Keys)
            {
                if (activator == null)
                {
                    (destroyed ??= new List<TActivator>()).Add(activator);
                }
            }

            if (destroyed == null)
            {
                return;
            }

            foreach (TActivator activator in destroyed)
            {
                inRange.Remove(activator);
            }
        }

        private void HideAllPrompts()
        {
            foreach (InteractionBinding binding in bindings)
            {
                binding.ShowPrompt(false);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetFocus()
        {
            Active.Clear();
            nextEntrySequence = 0;
        }
    }
}
