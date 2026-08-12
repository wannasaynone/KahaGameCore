using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.GameEvents;
using KahaGameCore.Parameters;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.Persistence.GameEventsIntegration.Tests
{
    public sealed class GameSaveCoordinatorTests
    {
        private sealed class BlockingSetCommand : IEffectCommand
        {
            private readonly ParameterStore parameters;
            private readonly UniTaskCompletionSource release =
                new UniTaskCompletionSource();

            public BlockingSetCommand(ParameterStore parameters)
            {
                this.parameters = parameters;
            }

            public async UniTask ExecuteAsync(
                EffectExecutionContext context,
                IReadOnlyList<string> arguments,
                CancellationToken cancellationToken)
            {
                await release.Task.AttachExternalCancellation(
                    cancellationToken);
                parameters.Set("Score", 7);
            }

            public void Release()
            {
                release.TrySetResult();
            }
        }

        private string rootDirectory;

        [SetUp]
        public void SetUp()
        {
            rootDirectory = Path.Combine(
                Path.GetTempPath(),
                "KahaGameCore.GameSaveCoordinatorTests",
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
        public async Task SaveAsync_WaitsForQueueBeforeCapturingAndWritingSlot()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Score", "Score", 0, 0, 10)
            });
            BlockingSetCommand command =
                new BlockingSetCommand(parameters);
            EffectCommandRegistry commands = new EffectCommandRegistry();
            commands.Register(new EffectCommandDefinition(
                name: "BlockingSet",
                displayName: "Blocking Set",
                category: "Tests",
                Array.Empty<EffectCommandParameterDefinition>(),
                command));
            GameEventDocumentJsonCodec eventCodec =
                new GameEventDocumentJsonCodec();
            GameEventRunner runner = new GameEventRunner(
                new GameEventCatalog(Array.Empty<TextAsset>(), eventCodec),
                new EffectRuntime(commands),
                parameters,
                eventCodec);
            TextAsset file = new TextAsset("{" +
                "\"SchemaVersion\":1," +
                "\"DocumentGuid\":\"40000000-0000-0000-0000-000000000001\"," +
                "\"DisplayName\":\"Blocking Save Event\"," +
                "\"TriggerTiming\":\"\"," +
                "\"Condition\":\"\"," +
                "\"Priority\":0," +
                "\"Commands\":\"BlockingSet();\"}" );
            GameSaveSlotStore slots =
                new GameSaveSlotStore(rootDirectory);
            SaveParticipantRegistry participants =
                new SaveParticipantRegistry();
            GameSaveDocumentJsonCodec saveCodec =
                new GameSaveDocumentJsonCodec();
            GameSaveCoordinator coordinator = new GameSaveCoordinator(
                runner,
                parameters,
                participants,
                saveCodec,
                slots);
            UniTask eventOperation = runner.RunAsync(
                file,
                new EventContext(CancellationToken.None));
            UniTask saveOperation = coordinator.SaveAsync(
                3,
                "Factory",
                CancellationToken.None);

            try
            {
                Assert.That(slots.Exists(3), Is.False);

                command.Release();
                await eventOperation;
                await saveOperation;

                GameSaveSnapshot snapshot = saveCodec.Read(
                    slots.Load(3),
                    participants);
                Assert.That(
                    snapshot.Parameters.TryGetValue(
                        "Score",
                        out ParameterValue score),
                    Is.True);
                Assert.That(score.AsInt(), Is.EqualTo(7));
            }
            finally
            {
                command.Release();
                UnityEngine.Object.DestroyImmediate(file);
            }
        }
    }
}
