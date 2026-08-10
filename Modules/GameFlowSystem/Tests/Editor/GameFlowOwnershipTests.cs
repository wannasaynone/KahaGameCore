using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.GameFlowSystem.DefaultImplements.DataAccess;
using KahaGameCore.Parameters;
using KahaGameCore.StaticData;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FlowLocationService = KahaGameCore.GameFlowSystem.DefaultImplements.LocationService;

namespace KahaGameCore.GameFlowSystem.Tests
{
    public class GameFlowOwnershipTests
    {
        private sealed class NoOpDialoguePlayer : IDialoguePlayer
        {
            public UniTask PlayAsync(int dialogueId) => UniTask.CompletedTask;
        }

        private sealed class NoOpPerformancePlayer : IPerformancePlayer
        {
            public void Register(string performanceId, IStagePerformance performance) { }
            public UniTask PlayAsync(string performanceId) => UniTask.CompletedTask;
        }

        private sealed class RecordingCommandExecutor : ICommandExecutor
        {
            public int ExecutionCount { get; private set; }

            public void Execute(string rawCommands, Action onCompleted)
            {
                ExecutionCount++;
                onCompleted?.Invoke();
            }

            public UniTask ExecuteAsync(string rawCommands, CancellationToken cancellationToken)
            {
                ExecutionCount++;
                return UniTask.CompletedTask;
            }
        }

        private sealed class NoOpActionMenuPresenter : IActionMenuPresenter
        {
            public UniTask<IGameFlowAction> SelectActionAsync(IReadOnlyList<ActionMenuEntry> entries)
            {
                return UniTask.FromResult<IGameFlowAction>(null);
            }
        }

        [Test]
        public void TimeService_AdvancingIntoNewDay_ChangesDayParameterAndOwnsCurrentPhase()
        {
            TextAsset phases = new TextAsset(
                "[{\"ID\":1,\"Key\":\"Morning\",\"DisplayName\":\"早晨\",\"NextID\":2,\"IsNewDay\":1,\"AllowAction\":1}," +
                "{\"ID\":2,\"Key\":\"Night\",\"DisplayName\":\"晚上\",\"NextID\":1,\"IsNewDay\":0,\"AllowAction\":1}]")
            {
                name = nameof(TimePhaseData)
            };
            GameStaticDataManager staticData = new GameStaticDataManager();
            staticData.Add<TimePhaseData>(new TextAssetJsonStaticDataHandler(new[] { phases }));
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Day", "天數", initialValue: 1, minValue: 1, maxValue: 999)
            });
            TimeService time = new TimeService(staticData, parameters);

            time.ResetToFirstPhase();
            time.AdvanceTime();
            time.AdvanceTime();

            Assert.That(parameters.GetInt("Day"), Is.EqualTo(2));
            Assert.That(time.CurrentDay, Is.EqualTo(2));
            Assert.That(time.CurrentPhase.Key, Is.EqualTo("Morning"));
            Assert.That(parameters.TryGetValue("CurrentPhase", out _), Is.False);
        }

        [Test]
        public void LocationService_MoveAndReset_AreOwnedByLocationService()
        {
            TextAsset locations = new TextAsset(
                "[{\"ID\":1,\"Name\":\"小屋\",\"VisibleCondition\":\"\",\"ShowInMenu\":0,\"SortOrder\":1}," +
                "{\"ID\":2,\"Name\":\"市集\",\"VisibleCondition\":\"\",\"ShowInMenu\":1,\"SortOrder\":2}]")
            {
                name = nameof(LocationData)
            };
            GameStaticDataManager staticData = new GameStaticDataManager();
            staticData.Add<LocationData>(new TextAssetJsonStaticDataHandler(new[] { locations }));
            ParameterStore parameters = new ParameterStore(new ParameterDefinition[0]);
            FlowLocationService service = new FlowLocationService(staticData, new GameFlowExpressions(parameters));

            service.MoveTo(2);
            Assert.That(service.CurrentLocationID, Is.EqualTo(2));

            service.ResetToInitial();

            Assert.That(service.CurrentLocationID, Is.EqualTo(1));
            Assert.That(parameters.TryGetValue("CurrentLocation", out _), Is.False);
        }

        [Test]
        public void GameEventTriggerService_RepeatedTiming_DoesNotKeepExecutionCounts()
        {
            TextAsset triggers = new TextAsset(
                "[{\"ID\":1,\"Timing\":\"GameStart\",\"Condition\":\"\",\"Priority\":100," +
                "\"DialogueID\":0,\"PrePerformance\":\"\",\"PostPerformance\":\"\",\"Commands\":\"Ping()\"}]")
            {
                name = nameof(GameEventTriggerData)
            };
            GameStaticDataManager staticData = new GameStaticDataManager();
            staticData.Add<GameEventTriggerData>(new TextAssetJsonStaticDataHandler(new[] { triggers }));
            ParameterStore parameters = new ParameterStore(new ParameterDefinition[0]);
            RecordingCommandExecutor commands = new RecordingCommandExecutor();
            GameEventTriggerService service = new GameEventTriggerService(
                staticData,
                new GameFlowExpressions(parameters),
                new NoOpDialoguePlayer(),
                new NoOpPerformancePlayer(),
                commands);

            service.RaiseTimingAsync("GameStart").GetAwaiter().GetResult();
            service.RaiseTimingAsync("GameStart").GetAwaiter().GetResult();

            Assert.That(commands.ExecutionCount, Is.EqualTo(2));
        }

        [Test]
        public void Builder_UsesCompositionRootParameterStoreAcrossDefaultServices()
        {
            GameStaticDataManager staticData = new GameStaticDataManager();
            AddTable<TimePhaseData>(staticData,
                "[{\"ID\":1,\"Key\":\"Morning\",\"DisplayName\":\"早晨\",\"NextID\":1,\"IsNewDay\":0,\"AllowAction\":1}]");
            AddTable<LocationData>(staticData,
                "[{\"ID\":1,\"Name\":\"小屋\",\"VisibleCondition\":\"\",\"ShowInMenu\":0,\"SortOrder\":1}," +
                "{\"ID\":2,\"Name\":\"市集\",\"VisibleCondition\":\"\",\"ShowInMenu\":1,\"SortOrder\":2}]");
            AddTable<PlayerActionData>(staticData, "[]");
            AddTable<GameEventTriggerData>(staticData, "[]");
            AddTable<GameTextData>(staticData, "[]");
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Day", "天數", initialValue: 1, minValue: 1, maxValue: 999),
                ParameterDefinition.Int("Supplies", "物資", initialValue: 12, minValue: 0, maxValue: 9999)
            });

            GameFlowServices services = new GameFlowSystemBuilder(staticData, parameters)
                .WithActionMenuPresenter(new NoOpActionMenuPresenter())
                .OverrideDialoguePlayer(new NoOpDialoguePlayer())
                .Build();

            Assert.That(services.Parameters, Is.SameAs(parameters));
            Assert.That(services.ConditionEvaluator.Evaluate("$Supplies == 12"), Is.True);
            Assert.That(services.TimeService.CurrentDay, Is.EqualTo(1));

            services.Parameters.Set("Supplies", 99);
            services.LocationService.MoveTo(2);
            services.ResetForNewGame();

            Assert.That(services.Parameters.GetInt("Supplies"), Is.EqualTo(12));
            Assert.That(services.LocationService.CurrentLocationID, Is.EqualTo(1));
            Assert.That(services.TimeService.CurrentPhase.Key, Is.EqualTo("Morning"));
        }

        [Test]
        public void SampleParameterTables_LoadNineDefinitionsWithoutFlowStateKeys()
        {
            const string folder = "Assets/KahaGameCore/Modules/GameFlowSystem/DefaultViews/SampleData/Parameters";
            string[] paths = AssetDatabase.FindAssets("t:TextAsset", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".parameters.json"))
                .OrderBy(path => path)
                .ToArray();
            ParameterTableJsonCodec codec = new ParameterTableJsonCodec();
            ParameterTable[] tables = paths
                .Select(path => codec.Read(AssetDatabase.LoadAssetAtPath<TextAsset>(path).text))
                .ToArray();
            ParameterDefinition[] definitions = tables
                .SelectMany(table => table.Definitions)
                .ToArray();
            ParameterStore store = new ParameterStore(definitions);

            Assert.That(tables, Has.Length.EqualTo(1));
            Assert.That(definitions, Has.Length.EqualTo(9));
            Assert.That(store.TryGetValue("Day", out _), Is.True);
            Assert.That(store.TryGetValue("CurrentPhase", out _), Is.False);
            Assert.That(store.TryGetValue("CurrentLocation", out _), Is.False);
            Assert.That(store.GetInt("machine_01_stage"), Is.EqualTo(0));
        }

        private static void AddTable<T>(GameStaticDataManager staticData, string json)
            where T : IGameData
        {
            TextAsset table = new TextAsset(json) { name = typeof(T).Name };
            staticData.Add<T>(new TextAssetJsonStaticDataHandler(new[] { table }));
        }
    }
}
