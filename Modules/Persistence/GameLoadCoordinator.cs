using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Parameters;

namespace KahaGameCore.Persistence
{
    /// <summary>
    /// Restores one save slot in the fixed parameter-state then Scene
    /// composition order, so Scene binders observe restored values.
    /// </summary>
    public sealed class GameLoadCoordinator
    {
        private readonly ParameterStore parameters;
        private readonly GameSaveDocumentJsonCodec codec;
        private readonly GameSaveSlotStore slots;
        private readonly IGameLoadHost host;

        public GameLoadCoordinator(
            ParameterStore parameters,
            GameSaveDocumentJsonCodec codec,
            GameSaveSlotStore slots,
            IGameLoadHost host)
        {
            this.parameters = parameters ??
                throw new ArgumentNullException(nameof(parameters));
            this.codec = codec ??
                throw new ArgumentNullException(nameof(codec));
            this.slots = slots ??
                throw new ArgumentNullException(nameof(slots));
            this.host = host ??
                throw new ArgumentNullException(nameof(host));
        }

        public async UniTask LoadAsync(
            int slot,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GameSaveDocument document = codec.ReadDocument(slots.Load(slot));
            parameters.Restore(codec.DecodeParameters(document));

            await host.LoadSceneAsync(
                document.SceneKey,
                parameters,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
