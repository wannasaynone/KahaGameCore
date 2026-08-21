using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

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

        private GameEventRunner runner;
        private EventContext context;

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

            Trigger();
        }

        private bool IncludesLayer(int layer)
        {
            return (triggeringLayers.value & (1 << layer)) != 0;
        }
    }
}
