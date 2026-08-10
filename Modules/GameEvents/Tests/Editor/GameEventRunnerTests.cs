using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.GameFlowSystem.DefaultImplements.Commands;
using KahaGameCore.Parameters;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class GameEventRunnerTests
    {
        private sealed class RecordingCommand : IEffectCommand
        {
            private readonly List<string> records;

            public RecordingCommand(List<string> records)
            {
                this.records = records;
            }

            public UniTask ExecuteAsync(
                EffectExecutionContext context,
                IReadOnlyList<string> arguments,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                records.Add(arguments[0]);
                return UniTask.CompletedTask;
            }
        }

        private sealed class BlockingCommand : IEffectCommand
        {
            private readonly List<string> records;
            private readonly UniTaskCompletionSource release = new UniTaskCompletionSource();

            public BlockingCommand(List<string> records)
            {
                this.records = records;
            }

            public async UniTask ExecuteAsync(
                EffectExecutionContext context,
                IReadOnlyList<string> arguments,
                CancellationToken cancellationToken)
            {
                records.Add(arguments[0] + "-start");
                await release.Task.AttachExternalCancellation(cancellationToken);
                records.Add(arguments[0] + "-end");
            }

            public void Release()
            {
                release.TrySetResult();
            }
        }

        [Test]
        public void RunAsync_DirectFileExecutesSetParameter()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("machine_01_stage", "Machine Stage", 0, 0, 2)
            });
            GameFlowExpressions expressions = new GameFlowExpressions(parameters);
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(new EffectCommandDefinition(
                "SetParameter",
                "Set Parameter",
                "Parameters",
                new[]
                {
                    new EffectCommandParameterDefinition("key", EffectCommandParameterKind.ParameterKey),
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.Literal)
                },
                new SetParameterCommand(parameters, expressions)));
            EffectRuntime effects = new EffectRuntime(registry);
            GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();
            GameEventCatalog catalog = new GameEventCatalog(Array.Empty<TextAsset>(), codec);
            GameEventRunner runner = new GameEventRunner(catalog, effects, parameters);
            TextAsset file = new TextAsset(@"{
  ""SchemaVersion"": 1,
  ""DocumentGuid"": ""4c920099-f4f8-4ed6-b7f4-7acf7e105be8"",
  ""DisplayName"": ""Activate Machine"",
  ""TriggerTiming"": ""machine_interact"",
  ""Condition"": ""$machine_01_stage == 0"",
  ""Priority"": 100,
  ""Commands"": ""SetParameter(machine_01_stage,1);""
}") { name = "MachineActivate.gameevent.json" };

            try
            {
                runner.RunAsync(file, new EventContext(CancellationToken.None))
                    .GetAwaiter()
                    .GetResult();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(file);
            }

            Assert.That(parameters.GetInt("machine_01_stage"), Is.EqualTo(1));
        }

        [Test]
        public void TriggerAsync_UsesPriorityStableOrderAndCandidateSnapshot()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Gate", "Gate", 0, 0, 1)
            });
            GameFlowExpressions expressions = new GameFlowExpressions(parameters);
            List<string> records = new List<string>();
            EffectCommandRegistry registry = new EffectCommandRegistry();
            RegisterSetParameter(registry, parameters, expressions);
            registry.Register(new EffectCommandDefinition(
                "Record",
                "Record",
                "Tests",
                new[]
                {
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.Literal)
                },
                new RecordingCommand(records)));

            TextAsset firstHigh = CreateEvent(
                "10000000-0000-0000-0000-000000000001",
                "First High",
                "tick",
                "$Gate == 0",
                100,
                "Record(high-first);SetParameter(Gate,1);");
            TextAsset secondHigh = CreateEvent(
                "10000000-0000-0000-0000-000000000002",
                "Second High",
                "tick",
                "$Gate == 0",
                100,
                "Record(high-second);");
            TextAsset lower = CreateEvent(
                "10000000-0000-0000-0000-000000000003",
                "Lower",
                "tick",
                "",
                10,
                "Record(low);");
            TextAsset lateMatch = CreateEvent(
                "10000000-0000-0000-0000-000000000004",
                "Late Match",
                "tick",
                "$Gate == 1",
                0,
                "Record(late-match);");
            TextAsset[] files = { firstHigh, secondHigh, lower, lateMatch };

            try
            {
                GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();
                GameEventCatalog catalog = new GameEventCatalog(files, codec);
                GameEventRunner runner = new GameEventRunner(
                    catalog,
                    new EffectRuntime(registry),
                    parameters,
                    codec);

                runner.TriggerAsync("tick", new EventContext(CancellationToken.None))
                    .GetAwaiter()
                    .GetResult();
            }
            finally
            {
                for (int index = 0; index < files.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(files[index]);
                }
            }

            CollectionAssert.AreEqual(new[] { "high-first", "high-second", "low" }, records);
            Assert.That(parameters.GetInt("Gate"), Is.EqualTo(1));
        }

        [Test]
        public async Task RunAndTriggerAsync_ShareOneFifoQueue()
        {
            ParameterStore parameters = new ParameterStore(Array.Empty<ParameterDefinition>());
            List<string> records = new List<string>();
            BlockingCommand blocker = new BlockingCommand(records);
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(new EffectCommandDefinition(
                "Block",
                "Block",
                "Tests",
                new[]
                {
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.Literal)
                },
                blocker));
            registry.Register(new EffectCommandDefinition(
                "Record",
                "Record",
                "Tests",
                new[]
                {
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.Literal)
                },
                new RecordingCommand(records)));
            TextAsset direct = CreateEvent(
                "20000000-0000-0000-0000-000000000001",
                "Direct",
                "",
                "",
                0,
                "Block(first);");
            TextAsset queued = CreateEvent(
                "20000000-0000-0000-0000-000000000002",
                "Queued",
                "second",
                "",
                0,
                "Record(second);");
            GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();
            GameEventCatalog catalog = new GameEventCatalog(new[] { queued }, codec);
            GameEventRunner runner = new GameEventRunner(
                catalog,
                new EffectRuntime(registry),
                parameters,
                codec);
            EventContext context = new EventContext(CancellationToken.None);
            UniTask first = runner.RunAsync(direct, context);
            UniTask second = runner.TriggerAsync("second", context);

            try
            {
                CollectionAssert.AreEqual(new[] { "first-start" }, records);
            }
            finally
            {
                blocker.Release();
                await first;
                await second;
                UnityEngine.Object.DestroyImmediate(direct);
                UnityEngine.Object.DestroyImmediate(queued);
            }

            CollectionAssert.AreEqual(
                new[] { "first-start", "first-end", "second" },
                records);
        }

        [Test]
        public async Task RunAsync_InvalidDirectFileWaitsForEarlierQueuedJob()
        {
            ParameterStore parameters = new ParameterStore(Array.Empty<ParameterDefinition>());
            List<string> records = new List<string>();
            BlockingCommand blocker = new BlockingCommand(records);
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(new EffectCommandDefinition(
                "Block",
                "Block",
                "Tests",
                new[]
                {
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.Literal)
                },
                blocker));
            GameEventRunner runner = CreateRunner(parameters, registry);
            TextAsset firstFile = CreateEvent(
                "20000000-0000-0000-0000-000000000003",
                "First",
                "",
                "",
                0,
                "Block(first);");
            TextAsset invalidFile = new TextAsset("{") { name = "Invalid.gameevent.json" };
            EventContext context = new EventContext(CancellationToken.None);
            UniTask first = runner.RunAsync(firstFile, context);
            UniTask second = default;

            try
            {
                Assert.DoesNotThrow(() => second = runner.RunAsync(invalidFile, context));
                CollectionAssert.AreEqual(new[] { "first-start" }, records);
            }
            finally
            {
                blocker.Release();
                await first;
                UnityEngine.Object.DestroyImmediate(firstFile);
                UnityEngine.Object.DestroyImmediate(invalidFile);
            }

            GameEventException exception = Assert.ThrowsAsync<GameEventException>(async () =>
                await second);
            Assert.That(exception.Code, Is.EqualTo("InvalidJson"));
        }

        [Test]
        public void Catalog_DuplicateDocumentGuidFailsFast()
        {
            const string duplicateGuid = "30000000-0000-0000-0000-000000000001";
            TextAsset first = CreateEvent(duplicateGuid, "First", "tick", "", 0, "");
            TextAsset second = CreateEvent(duplicateGuid, "Second", "tick", "", 0, "");

            try
            {
                GameEventException exception = Assert.Throws<GameEventException>(() =>
                    new GameEventCatalog(
                        new[] { first, second },
                        new GameEventDocumentJsonCodec()));

                Assert.That(exception.Code, Is.EqualTo("DuplicateDocumentGuid"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void RunAsync_ConditionFalseSkipsCommands()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Gate", "Gate", 0, 0, 1)
            });
            List<string> records = new List<string>();
            EffectCommandRegistry registry = CreateRecordingRegistry(records);
            TextAsset file = CreateEvent(
                "30000000-0000-0000-0000-000000000002",
                "Skipped",
                "",
                "$Gate == 1",
                0,
                "Record(should-not-run);");

            try
            {
                CreateRunner(parameters, registry).RunAsync(
                        file,
                        new EventContext(CancellationToken.None))
                    .GetAwaiter()
                    .GetResult();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(file);
            }

            Assert.That(records, Is.Empty);
        }

        [Test]
        public void RunAsync_InvalidConditionFailsExplicitly()
        {
            ParameterStore parameters = new ParameterStore(Array.Empty<ParameterDefinition>());
            TextAsset file = CreateEvent(
                "30000000-0000-0000-0000-000000000003",
                "Invalid Condition",
                "",
                "$Missing == 1",
                0,
                "");

            try
            {
                GameEventException exception = Assert.Throws<GameEventException>(() =>
                    CreateRunner(parameters, new EffectCommandRegistry()).RunAsync(
                            file,
                            new EventContext(CancellationToken.None))
                        .GetAwaiter()
                        .GetResult());

                Assert.That(exception.Code, Is.EqualTo("ConditionFailed"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(file);
            }
        }

        [Test]
        public void RunAsync_EffectFailureFailsExplicitly()
        {
            ParameterStore parameters = new ParameterStore(Array.Empty<ParameterDefinition>());
            TextAsset file = CreateEvent(
                "30000000-0000-0000-0000-000000000004",
                "Unknown Effect",
                "",
                "",
                0,
                "Missing();");

            try
            {
                GameEventException exception = Assert.Throws<GameEventException>(() =>
                    CreateRunner(parameters, new EffectCommandRegistry()).RunAsync(
                            file,
                            new EventContext(CancellationToken.None))
                        .GetAwaiter()
                        .GetResult());

                Assert.That(exception.Code, Is.EqualTo("EffectFailed"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(file);
            }
        }

        [Test]
        public async Task RunAsync_CancellationDuringCommandPropagatesCancellation()
        {
            ParameterStore parameters = new ParameterStore(Array.Empty<ParameterDefinition>());
            List<string> records = new List<string>();
            BlockingCommand blocker = new BlockingCommand(records);
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(new EffectCommandDefinition(
                "Block",
                "Block",
                "Tests",
                new[]
                {
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.Literal)
                },
                blocker));
            TextAsset file = CreateEvent(
                "30000000-0000-0000-0000-000000000006",
                "Cancellation During Command",
                "",
                "",
                0,
                "Block(running);");
            CancellationTokenSource cancellation = new CancellationTokenSource();

            try
            {
                UniTask operation = CreateRunner(parameters, registry).RunAsync(
                    file,
                    new EventContext(cancellation.Token));
                CollectionAssert.AreEqual(new[] { "running-start" }, records);

                cancellation.Cancel();

                Assert.CatchAsync<OperationCanceledException>(async () => await operation);
            }
            finally
            {
                cancellation.Dispose();
                UnityEngine.Object.DestroyImmediate(file);
            }

            CollectionAssert.AreEqual(new[] { "running-start" }, records);
        }

        [Test]
        public void RunAsync_CancelledContextCancelsWithoutExecuting()
        {
            ParameterStore parameters = new ParameterStore(Array.Empty<ParameterDefinition>());
            List<string> records = new List<string>();
            TextAsset file = CreateEvent(
                "30000000-0000-0000-0000-000000000005",
                "Cancelled",
                "",
                "",
                0,
                "Record(should-not-run);");
            CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            try
            {
                Assert.Throws<OperationCanceledException>(() =>
                    CreateRunner(parameters, CreateRecordingRegistry(records)).RunAsync(
                            file,
                            new EventContext(cancellation.Token))
                        .GetAwaiter()
                        .GetResult());
            }
            finally
            {
                cancellation.Dispose();
                UnityEngine.Object.DestroyImmediate(file);
            }

            Assert.That(records, Is.Empty);
        }

        private static EffectCommandRegistry CreateRecordingRegistry(List<string> records)
        {
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(new EffectCommandDefinition(
                "Record",
                "Record",
                "Tests",
                new[]
                {
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.Literal)
                },
                new RecordingCommand(records)));
            return registry;
        }

        private static GameEventRunner CreateRunner(
            ParameterStore parameters,
            EffectCommandRegistry registry)
        {
            GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();
            return new GameEventRunner(
                new GameEventCatalog(Array.Empty<TextAsset>(), codec),
                new EffectRuntime(registry),
                parameters,
                codec);
        }

        private static void RegisterSetParameter(
            EffectCommandRegistry registry,
            ParameterStore parameters,
            GameFlowExpressions expressions)
        {
            registry.Register(new EffectCommandDefinition(
                "SetParameter",
                "Set Parameter",
                "Parameters",
                new[]
                {
                    new EffectCommandParameterDefinition("key", EffectCommandParameterKind.ParameterKey),
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.Literal)
                },
                new SetParameterCommand(parameters, expressions)));
        }

        private static TextAsset CreateEvent(
            string documentGuid,
            string displayName,
            string triggerTiming,
            string condition,
            int priority,
            string commands)
        {
            string json = "{"
                + "\"SchemaVersion\":1,"
                + "\"DocumentGuid\":\"" + documentGuid + "\","
                + "\"DisplayName\":\"" + displayName + "\","
                + "\"TriggerTiming\":\"" + triggerTiming + "\","
                + "\"Condition\":\"" + condition + "\","
                + "\"Priority\":" + priority + ","
                + "\"Commands\":\"" + commands + "\""
                + "}";
            return new TextAsset(json) { name = displayName + ".gameevent.json" };
        }
    }
}
