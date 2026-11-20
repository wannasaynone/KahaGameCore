using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.GameEvents.Editor;
using KahaGameCore.Parameters;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class GameEventControlFlowTests
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
                records.Add(arguments[0]);
                return UniTask.CompletedTask;
            }
        }

        private const string ParentGuid = "40000000-0000-0000-0000-000000000001";
        private const string ChildGuid = "40000000-0000-0000-0000-000000000002";

        [TestCase(true, new[] { "before", "then", "after" })]
        [TestCase(false, new[] { "before", "after" })]
        public async Task If_RunsBlockOnlyWhenConditionHolds(bool flag, string[] expected)
        {
            List<string> records = await RunAsync(
                "Record(before);If($Flag == true);Record(then);EndIf();Record(after);",
                flag: flag);

            CollectionAssert.AreEqual(expected, records);
        }

        [TestCase(true, new[] { "then", "after" })]
        [TestCase(false, new[] { "else", "after" })]
        public async Task Else_RunsOnlyWhenConditionFails(bool flag, string[] expected)
        {
            List<string> records = await RunAsync(
                "If($Flag == true);Record(then);Else();Record(else);EndIf();Record(after);",
                flag: flag);

            CollectionAssert.AreEqual(expected, records);
        }

        [TestCase(0, new[] { "small", "end" })]
        [TestCase(1, new[] { "big", "one", "end" })]
        [TestCase(2, new[] { "big", "not-one", "end" })]
        public async Task If_CanNest(int count, string[] expected)
        {
            List<string> records = await RunAsync(
                "If($Count > 0);Record(big);" +
                "If($Count == 1);Record(one);Else();Record(not-one);EndIf();" +
                "Else();Record(small);EndIf();Record(end);",
                count: count);

            CollectionAssert.AreEqual(expected, records);
        }

        [Test]
        public async Task Quit_StopsTheRestOfTheEvent()
        {
            List<string> records = await RunAsync(
                "Record(before);Quit();Record(after);");

            CollectionAssert.AreEqual(new[] { "before" }, records);
        }

        [TestCase(true, new[] { "before" })]
        [TestCase(false, new[] { "before", "after" })]
        public async Task Quit_InsideIfOnlyStopsWhenReached(bool flag, string[] expected)
        {
            List<string> records = await RunAsync(
                "Record(before);If($Flag == true);Quit();EndIf();Record(after);",
                flag: flag);

            CollectionAssert.AreEqual(expected, records);
        }

        [Test]
        public async Task Quit_InTriggeredEventReturnsToTheCaller()
        {
            List<string> records = new List<string>();
            TextAsset parent = CreateEvent(
                ParentGuid,
                "Parent",
                "Record(p1);TriggerEvent(" + ChildGuid + ");Record(p2);");
            TextAsset child = CreateEvent(
                ChildGuid,
                "Child",
                "Record(c1);Quit();Record(c2);");
            try
            {
                GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();
                EffectCommandRegistry registry = CreateRegistry(records);
                GameEventCommandRouter router = new GameEventCommandRouter();
                foreach (EffectCommandDefinition definition in
                         GameEventEffectCommandModule.CreateDefinitions(router))
                {
                    registry.Register(definition);
                }

                GameEventRunner runner = new GameEventRunner(
                    new GameEventCatalog(new[] { parent, child }, codec),
                    new EffectRuntime(registry),
                    CreateParameters(flag: false, count: 0),
                    codec);
                router.Initialize(runner);

                await runner.RunAsync(parent, new EventContext(CancellationToken.None));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
                UnityEngine.Object.DestroyImmediate(child);
            }

            CollectionAssert.AreEqual(new[] { "p1", "c1", "p2" }, records);
        }

        [TestCase("Record(a);EndIf();")]
        [TestCase("If($Flag == true);Record(a);")]
        [TestCase("Else();")]
        [TestCase("If($Flag == true);Else();Else();EndIf();")]
        [TestCase("Quit(1);")]
        public void BrokenStructure_FailsBeforeRunningAnyCommand(string commands)
        {
            List<string> records = new List<string>();
            GameEventException exception = Assert.ThrowsAsync<GameEventException>(
                async () => await RunAsync(commands, records: records));

            Assert.That(exception.Code, Is.EqualTo("InvalidControlFlow"));
            Assert.That(records, Is.Empty);
        }

        [Test]
        public void Analyze_ReportsDepthAndTheBrokenRow()
        {
            GameEventCommandStructure structure = GameEventControlFlow.Analyze(new[]
            {
                "If", "A", "If", "B", "EndIf", "Else", "C", "EndIf", "D", "EndIf"
            });

            CollectionAssert.AreEqual(
                new[] { 0, 1, 1, 2, 1, 0, 1, 0, 0, 0 },
                Enumerable.Range(0, structure.Count).Select(structure.GetDepth));
            Assert.That(structure.FirstErrorIndex, Is.EqualTo(9));
            Assert.That(
                structure.GetErrorCode(9),
                Is.EqualTo(GameEventControlFlowError.EndIfWithoutIf));
        }

        [Test]
        public void RemovingIf_AlsoRemovesItsElseAndEndIfButKeepsTheBody()
        {
            List<GameEventCommandDraft> drafts = new[]
                {
                    "Before", "If", "A", "Else", "B", "EndIf", "After"
                }
                .Select(name => new GameEventCommandDraft { Name = name })
                .ToList();

            GameEventCommandDraftOperations.Remove(drafts, index: 1);

            CollectionAssert.AreEqual(
                new[] { "Before", "A", "B", "After" },
                drafts.Select(draft => draft.Name));
        }

        [Test]
        public void RenameInCommands_RewritesIfConditions()
        {
            string renamed = GameEventParameterRenamer.RenameInCommands(
                source: "If($Old > 1);EndIf();",
                commandsByName: GameEventCommandDescriptorLookup.Create(null),
                oldKey: "Old",
                newKey: "New");

            Assert.That(renamed, Is.EqualTo("If($New > 1);EndIf();"));
        }

        private static async Task<List<string>> RunAsync(
            string commands,
            bool flag = false,
            int count = 0,
            List<string> records = null)
        {
            records = records ?? new List<string>();
            GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();
            GameEventRunner runner = new GameEventRunner(
                new GameEventCatalog(Array.Empty<TextAsset>(), codec),
                new EffectRuntime(CreateRegistry(records)),
                CreateParameters(flag, count),
                codec);
            TextAsset file = CreateEvent(ParentGuid, "ControlFlow", commands);
            try
            {
                await runner.RunAsync(file, new EventContext(CancellationToken.None));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(file);
            }

            return records;
        }

        private static ParameterStore CreateParameters(bool flag, int count)
        {
            return new ParameterStore(new[]
            {
                ParameterDefinition.Bool(key: "Flag", displayName: "Flag", initialValue: flag),
                ParameterDefinition.Int(
                    key: "Count",
                    displayName: "Count",
                    initialValue: count,
                    minValue: 0,
                    maxValue: 10)
            });
        }

        private static EffectCommandRegistry CreateRegistry(List<string> records)
        {
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(new EffectCommandDefinition(
                name: "Record",
                displayName: "Record",
                category: "Tests",
                new[]
                {
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.Literal)
                },
                new RecordingCommand(records)));
            return registry;
        }

        private static TextAsset CreateEvent(
            string documentGuid,
            string displayName,
            string commands)
        {
            string json = "{"
                + "\"SchemaVersion\":2,"
                + "\"DocumentGuid\":\"" + documentGuid + "\","
                + "\"DisplayName\":\"" + displayName + "\","
                + "\"TriggerTiming\":\"\","
                + "\"Condition\":\"\","
                + "\"Commands\":\"" + commands + "\""
                + "}";
            return new TextAsset(json) { name = displayName + ".gameevent.json" };
        }
    }
}
