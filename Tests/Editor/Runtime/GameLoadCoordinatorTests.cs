using System;
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
            private readonly Action onSceneLoaded;

            public MemoryLoadHost(Action onSceneLoaded = null)
            {
                this.onSceneLoaded = onSceneLoaded;
            }

            public string LoadedSceneKey { get; private set; }
            public int ScoreObservedDuringSceneComposition { get; private set; }
            public string PhaseObservedDuringSceneComposition { get; private set; }

            public UniTask LoadSceneAsync(
                string sceneKey,
                ParameterStore parameters,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadedSceneKey = sceneKey;
                ScoreObservedDuringSceneComposition =
                    parameters.GetInt("Score");
                PhaseObservedDuringSceneComposition =
                    parameters.GetString("CurrentPhase");
                onSceneLoaded?.Invoke();
                return UniTask.CompletedTask;
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
        public async Task LoadAsync_RestoresParametersBeforeSceneComposition()
        {
            ParameterStore savedParameters =
                CreateParameters(score: 7, phase: "Night");
            GameSaveDocumentJsonCodec codec =
                new GameSaveDocumentJsonCodec();
            GameSaveSlotStore slots =
                new GameSaveSlotStore(rootDirectory);
            slots.Save(2, codec.Write("Factory", savedParameters.Capture()));

            ParameterStore runtimeParameters =
                CreateParameters(score: 0, phase: "Morning");
            MemoryLoadHost host = new MemoryLoadHost();
            GameLoadCoordinator coordinator = new GameLoadCoordinator(
                runtimeParameters,
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
        }

        [Test]
        public void LoadAsync_CancelledDuringSceneCompositionPropagates()
        {
            ParameterStore savedParameters =
                CreateParameters(score: 7, phase: "Night");
            GameSaveDocumentJsonCodec codec =
                new GameSaveDocumentJsonCodec();
            GameSaveSlotStore slots =
                new GameSaveSlotStore(rootDirectory);
            slots.Save(2, codec.Write("Factory", savedParameters.Capture()));

            ParameterStore runtimeParameters =
                CreateParameters(score: 0, phase: "Morning");
            CancellationTokenSource cancellation =
                new CancellationTokenSource();
            MemoryLoadHost host = new MemoryLoadHost(cancellation.Cancel);
            GameLoadCoordinator coordinator = new GameLoadCoordinator(
                runtimeParameters,
                codec,
                slots,
                host);

            Assert.CatchAsync<OperationCanceledException>(async () =>
                await coordinator.LoadAsync(2, cancellation.Token));

            cancellation.Dispose();
        }

        [Test]
        public void LoadAsync_MalformedSaveFailsBeforeLoadingScene()
        {
            const string malformedSave =
                "{\"SchemaVersion\":2," +
                "\"SceneKey\":\"Factory\"," +
                "\"Parameters\":{\"SchemaVersion\":1,\"Values\":[]}}";
            GameSaveSlotStore slots =
                new GameSaveSlotStore(rootDirectory);
            slots.Save(2, malformedSave);
            MemoryLoadHost host = new MemoryLoadHost();
            GameLoadCoordinator coordinator = new GameLoadCoordinator(
                CreateParameters(score: 0, phase: "Morning"),
                new GameSaveDocumentJsonCodec(),
                slots,
                host);

            InvalidOperationException exception =
                Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await coordinator.LoadAsync(2, CancellationToken.None));

            Assert.That(exception.Message, Does.Contain("schema version"));
            Assert.That(host.LoadedSceneKey, Is.Null);
        }

        private static ParameterStore CreateParameters(int score, string phase)
        {
            return new ParameterStore(new[]
            {
                ParameterDefinition.Int("Score", "Score", score, 0, 10),
                ParameterDefinition.String("CurrentPhase", "Current Phase", phase)
            });
        }
    }
}
