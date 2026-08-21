using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.GameEvents;
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

        private sealed class NoOpTriggerService : IGameFlowEventTriggerService
        {
            public UniTask RaiseTimingAsync(
                string timing,
                CancellationToken cancellationToken = default) => UniTask.CompletedTask;
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
                "[{\"ID\":1,\"Key\":\"Morning\",\"DisplayName\":\"早晨\",\"NextID\":2,\"IsNewDay\":1}," +
                "{\"ID\":2,\"Key\":\"Night\",\"DisplayName\":\"晚上\",\"NextID\":1,\"IsNewDay\":0}]")
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
            time.AdvancePhase();
            time.AdvancePhase();

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
        public void Builder_UsesCompositionRootParameterStoreAcrossDefaultServices()
        {
            GameStaticDataManager staticData = new GameStaticDataManager();
            AddTable<TimePhaseData>(staticData,
                "[{\"ID\":1,\"Key\":\"Morning\",\"DisplayName\":\"早晨\",\"NextID\":1,\"IsNewDay\":0}]");
            AddTable<LocationData>(staticData,
                "[{\"ID\":1,\"Name\":\"小屋\",\"VisibleCondition\":\"\",\"ShowInMenu\":0,\"SortOrder\":1}," +
                "{\"ID\":2,\"Name\":\"市集\",\"VisibleCondition\":\"\",\"ShowInMenu\":1,\"SortOrder\":2}]");
            AddTable<PlayerActionData>(staticData, "[]");
            AddTable<GameTextData>(staticData, "[]");
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Day", "天數", initialValue: 1, minValue: 1, maxValue: 999),
                ParameterDefinition.Int("Supplies", "物資", initialValue: 12, minValue: 0, maxValue: 9999)
            });

            Type gameFlowFactory = typeof(GameFlowEffectCommandModuleFactory);
            EffectCommandConfiguration commandConfiguration =
                new EffectCommandConfiguration(
                    new[]
                    {
                        new EffectCommandModuleReference(
                            gameFlowFactory.Assembly.GetName().Name,
                            $"{gameFlowFactory.FullName}, {gameFlowFactory.Assembly.GetName().Name}")
                    },
                    new[] { "AdvancePhase" });
            GameFlowServices services = new GameFlowSystemBuilder(staticData, parameters)
                .WithEffectCommandConfiguration(commandConfiguration)
                .WithActionMenuPresenter(new NoOpActionMenuPresenter())
                .OverrideDialoguePlayer(new NoOpDialoguePlayer())
                .WithEventTriggerFactory(_ => new NoOpTriggerService())
                .Build();

            Assert.That(services.Parameters, Is.SameAs(parameters));
            Assert.That(services.ConditionEvaluator.Evaluate("$Supplies == 12"), Is.True);
            Assert.That(services.TimeService.CurrentDay, Is.EqualTo(1));
            Assert.That(
                services.CommandRegistry.TryGetDefinition("AdvancePhase", out _),
                Is.True);

            services.Parameters.Set("Supplies", 99);
            services.LocationService.MoveTo(2);
            services.ResetForNewGame();

            Assert.That(services.Parameters.GetInt("Supplies"), Is.EqualTo(12));
            Assert.That(services.LocationService.CurrentLocationID, Is.EqualTo(1));
            Assert.That(services.TimeService.CurrentPhase.Key, Is.EqualTo("Morning"));
        }

        [Test]
        public void SampleParameterTables_LoadEightDefinitionsWithoutFlowStateKeys()
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
            Assert.That(definitions, Has.Length.EqualTo(8));
            Assert.That(store.TryGetValue("Day", out _), Is.True);
            Assert.That(store.TryGetValue("CurrentPhase", out _), Is.False);
            Assert.That(store.TryGetValue("CurrentLocation", out _), Is.False);
        }

        [Test]
        public void SampleActions_AllReferenceCatalogGameEventTimings()
        {
            const string sampleFolder =
                "Assets/KahaGameCore/Modules/GameFlowSystem/DefaultViews/SampleData";
            string[] eventPaths = AssetDatabase
                .FindAssets("t:TextAsset", new[] { sampleFolder + "/GameEvents" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".gameevent.json"))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            TextAsset[] eventFiles = eventPaths
                .Select(path => AssetDatabase.LoadAssetAtPath<TextAsset>(path))
                .ToArray();
            GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();

            Assert.DoesNotThrow(() => new GameEventCatalog(eventFiles, codec));
            HashSet<string> catalogTimings = eventFiles
                .Select(file => codec.Read(file.text).TriggerTiming)
                .ToHashSet(StringComparer.Ordinal);

            TextAsset actionTable = AssetDatabase.LoadAssetAtPath<TextAsset>(
                sampleFolder + "/PlayerActionData.txt");
            GameStaticDataManager staticData = new GameStaticDataManager();
            staticData.Add<PlayerActionData>(
                new TextAssetJsonStaticDataHandler(new[] { actionTable }));
            PlayerActionData[] actions = staticData.GetAllGameData<PlayerActionData>();

            Assert.That(actions, Has.Length.GreaterThan(0));
            foreach (PlayerActionData action in actions)
            {
                Assert.That(
                    catalogTimings,
                    Does.Contain(action.TriggerTiming),
                    $"Action {action.ID} references missing timing '{action.TriggerTiming}'.");
            }
        }

        private static void AddTable<T>(GameStaticDataManager staticData, string json)
            where T : IGameData
        {
            TextAsset table = new TextAsset(json) { name = typeof(T).Name };
            staticData.Add<T>(new TextAssetJsonStaticDataHandler(new[] { table }));
        }
    }
}
