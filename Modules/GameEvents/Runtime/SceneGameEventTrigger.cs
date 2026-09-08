using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KahaGameCore.GameEvents
{
    [DisallowMultipleComponent]
    public sealed class SceneGameEventTrigger : MonoBehaviour
    {
        [SerializeField]
        private TextAsset gameEventFile;

        [Tooltip("Only colliders on these layers can trigger this Game Event.")]
        [SerializeField]
        private LayerMask triggeringLayers;

        [Tooltip("Leave empty to trigger on enter, or assign an action to wait for that input.")]
        [SerializeField]
        private InputActionReference triggerInput;

        [Tooltip("Object shown while this trigger waits for the key press.")]
        [SerializeField]
        private GameObject inputPrompt;

        private GameEventRunner runner;
        private EventContext context;
        private bool isWaitingForKeyPress;
        private bool enabledTriggerInput;

        public TextAsset GameEventFile => gameEventFile;

        public void Configure(TextAsset file)
        {
            gameEventFile = file;
        }

        public void Configure(TextAsset file, LayerMask layers)
        {
            gameEventFile = file;
            triggeringLayers = layers;
        }

        public void Initialize(GameEventRunner gameEventRunner, EventContext eventContext)
        {
            runner = gameEventRunner ?? throw new ArgumentNullException(nameof(gameEventRunner));
            context = eventContext ?? throw new ArgumentNullException(nameof(eventContext));
        }

        public void Trigger()
        {
            TriggerAsync().Forget();
        }

        public UniTask TriggerAsync()
        {
            if (runner == null)
            {
                throw new InvalidOperationException(
                    "SceneGameEventTrigger must be initialized by the composition root.");
            }

            if (gameEventFile == null)
            {
                throw new InvalidOperationException(
                    "SceneGameEventTrigger requires a Game Event TextAsset.");
            }

            return runner.RunAsync(gameEventFile, context);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (runner == null || other == null || !IncludesLayer(other.gameObject.layer))
            {
                return;
            }

            if (TriggerAction == null)
            {
                Trigger();
                return;
            }

            isWaitingForKeyPress = true;
            ShowPrompt(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null || !IncludesLayer(other.gameObject.layer))
            {
                return;
            }

            CancelKeyPress();
        }

        private void Awake()
        {
            ShowPrompt(false);
        }

        private void OnEnable()
        {
            InputAction action = TriggerAction;
            if (action != null && !action.enabled)
            {
                action.Enable();
                enabledTriggerInput = true;
            }
        }

        private void OnDisable()
        {
            if (enabledTriggerInput)
            {
                TriggerAction?.Disable();
                enabledTriggerInput = false;
            }

            CancelKeyPress();
        }

        private void Update()
        {
            InputAction action = TriggerAction;
            if (isWaitingForKeyPress &&
                action != null &&
                action.enabled &&
                action.WasPressedThisFrame())
            {
                ConfirmKeyPress();
            }
        }

        private InputAction TriggerAction =>
            triggerInput == null ? null : triggerInput.action;

        private void ConfirmKeyPress()
        {
            isWaitingForKeyPress = false;
            ShowPrompt(false);
            Trigger();
        }

        private void CancelKeyPress()
        {
            isWaitingForKeyPress = false;
            ShowPrompt(false);
        }

        private void ShowPrompt(bool visible)
        {
            if (inputPrompt != null)
            {
                inputPrompt.SetActive(visible);
            }
        }

        private bool IncludesLayer(int layer)
        {
            return (triggeringLayers.value & (1 << layer)) != 0;
        }
    }
}
