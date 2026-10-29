using System;
using Cysharp.Threading.Tasks;
using KahaGameCore.Interaction;
using UnityEngine;

namespace KahaGameCore.GameEvents
{
    /// <summary>
    /// Runs a Game Event from the interactable on the same GameObject: on enter
    /// when the interactable has no key bindings, otherwise when any binding's
    /// key is pressed. Layers, keys and prompts are configured on the interactable.
    /// Enabling or disabling the trigger enables or disables the interactable, so
    /// a disabled trigger shows no prompt and runs nothing.
    /// </summary>
    public abstract class SceneGameEventTriggerBase<TInteractable, TActivator> : MonoBehaviour
        where TInteractable : InteractableBase<TActivator>
        where TActivator : Component
    {
        [SerializeField]
        private TextAsset gameEventFile;

        private GameEventRunner runner;
        private EventContext context;
        private TInteractable interactable;

        public TextAsset GameEventFile => gameEventFile;

        public void Configure(TextAsset file)
        {
            gameEventFile = file;
        }

        public void Initialize(GameEventRunner gameEventRunner, EventContext eventContext)
        {
            runner = gameEventRunner ?? throw new ArgumentNullException(nameof(gameEventRunner));
            context = eventContext ?? throw new ArgumentNullException(nameof(eventContext));
        }

        public void Trigger()
        {
            UnityEngine.Debug.Log("trigger " + gameObject.name);
            TriggerAsync().Forget();
        }

        public UniTask TriggerAsync()
        {
            if (runner == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} must be initialized by the composition root.");
            }

            if (gameEventFile == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} requires a Game Event TextAsset.");
            }

            return runner.RunAsync(gameEventFile, context);
        }

        protected void Awake()
        {
            interactable = GetComponent<TInteractable>();
            interactable.Entered += HandleEntered;
            interactable.Interacted += HandleInteracted;
        }

        protected void OnEnable()
        {
            if (interactable != null)
            {
                interactable.enabled = true;
            }
        }

        protected void OnDisable()
        {
            if (interactable != null)
            {
                interactable.enabled = false;
            }
        }

        protected void OnDestroy()
        {
            if (interactable != null)
            {
                interactable.Entered -= HandleEntered;
                interactable.Interacted -= HandleInteracted;
            }
        }

        private void HandleEntered(TActivator activator)
        {
            if (runner != null && enabled && !interactable.HasBindings)
            {
                Trigger();
            }
        }

        private void HandleInteracted(int bindingIndex, TActivator activator)
        {
            if (runner != null && enabled)
            {
                Trigger();
            }
        }
    }
}
