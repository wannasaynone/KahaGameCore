using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.GameEvents;
using KahaGameCore.Parameters;

namespace KahaGameCore.Persistence.GameEventsIntegration
{
    public sealed class GameSaveCoordinator
    {
        private readonly GameEventRunner gameEvents;
        private readonly ParameterStore parameters;
        private readonly GameSaveDocumentJsonCodec codec;
        private readonly GameSaveSlotStore slots;

        public GameSaveCoordinator(
            GameEventRunner gameEvents,
            ParameterStore parameters,
            GameSaveDocumentJsonCodec codec,
            GameSaveSlotStore slots)
        {
            this.gameEvents = gameEvents ??
                throw new ArgumentNullException(nameof(gameEvents));
            this.parameters = parameters ??
                throw new ArgumentNullException(nameof(parameters));
            this.codec = codec ??
                throw new ArgumentNullException(nameof(codec));
            this.slots = slots ??
                throw new ArgumentNullException(nameof(slots));
        }

        public async UniTask SaveAsync(
            int slot,
            string sceneKey,
            CancellationToken cancellationToken)
        {
            await gameEvents.WaitUntilIdleAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            slots.Save(slot, codec.Write(sceneKey, parameters.Capture()));
        }
    }
}
