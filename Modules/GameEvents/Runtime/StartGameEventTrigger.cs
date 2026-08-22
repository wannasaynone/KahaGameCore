using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KahaGameCore.GameEvents
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Kaha Game Core/Game Events/Start Game Event Trigger")]
    public sealed class StartGameEventTrigger : MonoBehaviour
    {
        [SerializeField] private TextAsset gameEventFile;

        private GameEventRunner runner;
        private EventContext context;

        public TextAsset GameEventFile => gameEventFile;

        public void Configure(TextAsset file)
        {
            gameEventFile = file;
        }

        public void Initialize(
            GameEventRunner gameEventRunner,
            EventContext eventContext)
        {
            runner = gameEventRunner ??
                throw new ArgumentNullException(nameof(gameEventRunner));
            context = eventContext ??
                throw new ArgumentNullException(nameof(eventContext));
        }

        public UniTask TriggerAsync()
        {
            if (runner == null)
            {
                throw new InvalidOperationException(
                    "StartGameEventTrigger must be initialized by the composition root.");
            }

            if (gameEventFile == null)
            {
                throw new InvalidOperationException(
                    "StartGameEventTrigger requires a Game Event TextAsset.");
            }

            return runner.RunAsync(gameEventFile, context);
        }
    }
}
