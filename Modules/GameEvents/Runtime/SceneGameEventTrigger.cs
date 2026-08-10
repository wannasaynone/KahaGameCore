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

        private GameEventRunner runner;
        private EventContext context;

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
    }
}
