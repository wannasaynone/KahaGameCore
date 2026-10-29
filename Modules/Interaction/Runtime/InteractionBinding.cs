using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KahaGameCore.Interaction
{
    /// <summary>One key an interactable answers to, with the prompt shown while it can be pressed.</summary>
    [Serializable]
    public sealed class InteractionBinding
    {
        [SerializeField] private InputActionReference input;

        [Tooltip("Shown while this key can be pressed. Optional.")]
        [SerializeField] private GameObject prompt;

        [Tooltip("A disabled binding shows no prompt and ignores its key.")]
        [SerializeField] private bool enabled = true;

        public InteractionBinding(InputActionReference input, GameObject prompt, bool enabled = true)
        {
            this.input = input;
            this.prompt = prompt;
            this.enabled = enabled;
        }

        public InputActionReference Input => input;
        public GameObject Prompt => prompt;

        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        internal InputAction Action => input == null ? null : input.action;

        internal void ShowPrompt(bool visible)
        {
            if (prompt != null && prompt.activeSelf != visible)
            {
                prompt.SetActive(visible);
            }
        }
    }
}
