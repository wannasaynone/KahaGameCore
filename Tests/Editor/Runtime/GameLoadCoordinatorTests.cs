using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using KahaGameCore.Parameters;
using KahaGameCore.Persistence;
using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public sealed class GameLoadCoordinatorTests
    {
        private sealed class MemoryLoadHost : IGameLoadHost
        {
            private readonly PhaseParticipant phase;
            private readonly SaveParticipantRegistry sceneParticipants;
            private readonly Action onSceneLoaded;

            public MemoryLoadHost(
                PhaseParticipant phase,
                SaveParticipantRegistry sceneParticipants,
                Action onSceneLoaded = null)
            {
                this.phase = phase;
                this.sceneParticipants = sceneParticipants;
                this.onSceneLoaded = onSceneLoaded;
            }

            public string LoadedSceneKey { get; private set; }
            public int ScoreObservedDuringSceneComposition { get; private set; }
            public string PhaseObservedDuringSceneComposition { get; private set; }

            public UniTask<SaveParticipantRegistry> LoadSceneAsync(
                string sceneKey,
                ParameterStore parameters,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadedSceneKey = sceneKey;
                ScoreObservedDuringSceneComposition =
                    parameters.GetInt("Score");
                PhaseObservedDuringSceneComposition =
                    phase.CurrentPhaseKey;
                onSceneLoaded?.Invoke();
                return UniTask.FromResult(sceneParticipants);
            }
        }

        public sealed class PhaseSnapshot
        {
            public string CurrentPhaseKey;
        }

        private sealed class PhaseParticipant :
            ISaveParticipant<PhaseSnapshot>
        {
            public PhaseParticipant(string currentPhaseKey)
            {
                CurrentPhaseKey = currentPhaseKey;
            }

            public string SaveKey => "GameFlow.CurrentPhase";
            public string CurrentPhaseKey { get; private set; }

            public PhaseSnapshot Capture()
            {
                return new PhaseSnapshot
                {
                    CurrentPhaseKey = CurrentPhaseKey
                };
            }

            public void Restore(PhaseSnapshot snapshot)
            {
                CurrentPhaseKey = snapshot.CurrentPhaseKey;
            }
        }

        public sealed class PlayerSnapshot
        {
            public int Position;
        }

        private sealed class PlayerParticipant :
            ISaveParticipant<PlayerSnapshot>
        {
            public PlayerParticipant(int position)
            {
                Position = position;
            }

            public string SaveKey => "Player";
            public int Position { get; private set; }

            public PlayerSnapshot Capture()
            {
                return new PlayerSnapshot
                {
                    Position = Position
                };
            }

            public void Restore(PlayerSnapshot snapshot)
            {
                Position = snapshot.Position;
            }
        }

        private string rootDirectory;

        [SetUp]
        public void SetUp()
        {
            rootDirectory = Path.Combine(
                Path.GetTempPath(),
                "KahaGameCore.GameLoadCoordinatorTests",
                Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, true);
            }
        }

        [Test]
        public async Task LoadAsync_RestoresSemanticStateBeforeSceneParticipants()
        {
            ParameterStore savedParameters = CreateParameters(score: 7);
            SaveParticipantRegistry savedParticipants =
                new SaveParticipantRegistry();
            savedParticipants.Register(new PhaseParticipant("Night"));
            savedParticipants.Register(new PlayerParticipant(position: 3));
            GameSaveDocumentJsonCodec codec =
                new GameSaveDocumentJsonCodec();
            GameSaveSlotStore slots =
                new GameSaveSlotStore(rootDirectory);
            slots.Save(
                2,
                codec.Write(
                    "Factory",
                    savedParameters.Capture(),
                    savedParticipants.Capture()));

            ParameterStore runtimeParameters = CreateParameters(score: 0);
            PhaseParticipant runtimePhase =
                new PhaseParticipant("Morning");
            SaveParticipantRegistry beforeSceneParticipants =
                new SaveParticipantRegistry();
            beforeSceneParticipants.Register(runtimePhase);
            PlayerParticipant runtimePlayer =
                new PlayerParticipant(position: 0);
            SaveParticipantRegistry sceneParticipants =
                new SaveParticipantRegistry();
            sceneParticipants.Register(runtimePlayer);
            MemoryLoadHost host = new MemoryLoadHost(
                runtimePhase,
                sceneParticipants);
            GameLoadCoordinator coordinator = new GameLoadCoordinator(
                runtimeParameters,
                beforeSceneParticipants,
                codec,
                slots,
                host);

            await coordinator.LoadAsync(2, CancellationToken.None);

            Assert.That(host.LoadedSceneKey, Is.EqualTo("Factory"));
            Assert.That(
                host.ScoreObservedDuringSceneComposition,
                Is.EqualTo(7));
            Assert.That(
                host.PhaseObservedDuringSceneComposition,
                Is.EqualTo("Night"));
            Assert.That(runtimePlayer.Position, Is.EqualTo(3));
        }

        [Test]
        public void LoadAsync_CancelledAfterSceneCompositionDoesNotRestoreSceneParticipants()
        {
            ParameterStore savedParameters = CreateParameters(score: 7);
            SaveParticipantRegistry savedParticipants =
                new SaveParticipantRegistry();
            savedParticipants.Register(new PhaseParticipant("Night"));
            savedParticipants.Register(new PlayerParticipant(position: 3));
            GameSaveDocumentJsonCodec codec =
                new GameSaveDocumentJsonCodec();
            GameSaveSlotStore slots =
                new GameSaveSlotStore(rootDirectory);
            slots.Save(
                2,
                codec.Write(
                    "Factory",
                    savedParameters.Capture(),
                    savedParticipants.Capture()));

            ParameterStore runtimeParameters = CreateParameters(score: 0);
            PhaseParticipant runtimePhase =
                new PhaseParticipant("Morning");
            SaveParticipantRegistry beforeSceneParticipants =
                new SaveParticipantRegistry();
            beforeSceneParticipants.Register(runtimePhase);
            PlayerParticipant runtimePlayer =
                new PlayerParticipant(position: 0);
            SaveParticipantRegistry sceneParticipants =
                new SaveParticipantRegistry();
            sceneParticipants.Register(runtimePlayer);
            CancellationTokenSource cancellation =
                new CancellationTokenSource();
            MemoryLoadHost host = new MemoryLoadHost(
                runtimePhase,
                sceneParticipants,
                cancellation.Cancel);
            GameLoadCoordinator coordinator = new GameLoadCoordinator(
                runtimeParameters,
                beforeSceneParticipants,
                codec,
                slots,
                host);

            Assert.CatchAsync<OperationCanceledException>(async () =>
                await coordinator.LoadAsync(2, cancellation.Token));

            Assert.That(runtimePlayer.Position, Is.EqualTo(0));
            cancellation.Dispose();
        }

        [Test]
        public void LoadAsync_DuplicateUnknownParticipantFailsBeforeLoadingScene()
        {
            const string malformedSave =
                "{\"SchemaVersion\":1," +
                "\"SceneKey\":\"Factory\"," +
                "\"Parameters\":{\"SchemaVersion\":1,\"Values\":[]}," +
                "\"Participants\":[" +
                "{\"SaveKey\":\"Missing\",\"Snapshot\":{\"Value\":1}}," +
                "{\"SaveKey\":\"Missing\",\"Snapshot\":{\"Value\":2}}]}";
            GameSaveSlotStore slots =
                new GameSaveSlotStore(rootDirectory);
            slots.Save(2, malformedSave);
            ParameterStore parameters = CreateParameters(score: 0);
            PhaseParticipant phase = new PhaseParticipant("Morning");
            MemoryLoadHost host = new MemoryLoadHost(
                phase,
                new SaveParticipantRegistry());
            GameLoadCoordinator coordinator = new GameLoadCoordinator(
                parameters,
                new SaveParticipantRegistry(),
                new GameSaveDocumentJsonCodec(),
                slots,
                host);

            InvalidOperationException exception =
                Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await coordinator.LoadAsync(
                        2,
                        CancellationToken.None));

            Assert.That(exception.Message, Does.Contain("duplicate key"));
            Assert.That(host.LoadedSceneKey, Is.Null);
        }

        private static ParameterStore CreateParameters(int score)
        {
            return new ParameterStore(new[]
            {
                ParameterDefinition.Int(
                    "Score",
                    "Score",
                    score,
                    0,
                    10)
            });
        }
    }
}
