using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Parameters;

namespace KahaGameCore.Persistence
{
    /// <summary>
    /// Restores one save slot in the fixed semantic-state → Scene composition →
    /// Scene participant order.
    /// </summary>
    public sealed class GameLoadCoordinator
    {
        private readonly ParameterStore parameters;
        private readonly SaveParticipantRegistry beforeSceneParticipants;
        private readonly GameSaveDocumentJsonCodec codec;
        private readonly GameSaveSlotStore slots;
        private readonly IGameLoadHost host;

        public GameLoadCoordinator(
            ParameterStore parameters,
            SaveParticipantRegistry beforeSceneParticipants,
            GameSaveDocumentJsonCodec codec,
            GameSaveSlotStore slots,
            IGameLoadHost host)
        {
            this.parameters = parameters ??
                throw new ArgumentNullException(nameof(parameters));
            this.beforeSceneParticipants = beforeSceneParticipants ??
                throw new ArgumentNullException(nameof(beforeSceneParticipants));
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
            ParameterSnapshot parameterSnapshot =
                codec.DecodeParameters(document);
            SaveParticipantSnapshotSet beforeSceneSnapshot =
                codec.DecodeRegisteredParticipants(
                    document,
                    beforeSceneParticipants);

            parameters.Restore(parameterSnapshot);
            beforeSceneParticipants.Restore(beforeSceneSnapshot);

            SaveParticipantRegistry sceneParticipants =
                await host.LoadSceneAsync(
                    document.SceneKey,
                    parameters,
                    cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (sceneParticipants == null)
            {
                throw new InvalidOperationException(
                    "Game load host returned no Scene participants.");
            }

            SaveParticipantSnapshotSet sceneSnapshot =
                codec.DecodeRegisteredParticipants(
                    document,
                    sceneParticipants);
            ValidateParticipantOwnership(
                document,
                sceneParticipants);
            sceneParticipants.Restore(sceneSnapshot);
        }

        private void ValidateParticipantOwnership(
            GameSaveDocument document,
            SaveParticipantRegistry sceneParticipants)
        {
            foreach (SaveParticipantDocument participant in
                document.Participants)
            {
                bool registeredBeforeScene =
                    beforeSceneParticipants.TryGetSnapshotType(
                        participant.SaveKey,
                        out Type _);
                bool registeredInScene =
                    sceneParticipants.TryGetSnapshotType(
                        participant.SaveKey,
                        out Type _);

                if (registeredBeforeScene && registeredInScene)
                {
                    throw new InvalidOperationException(
                        $"Save participant key '{participant.SaveKey}' is " +
                        "registered both before and after Scene loading.");
                }

                if (!registeredBeforeScene && !registeredInScene)
                {
                    throw new InvalidOperationException(
                        $"Save participant key '{participant.SaveKey}' has " +
                        "no load registration.");
                }
            }
        }
    }
}
