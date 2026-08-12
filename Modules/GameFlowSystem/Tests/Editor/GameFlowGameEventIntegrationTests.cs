using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.GameEvents;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.GameFlowSystem.DefaultImplements.Commands;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.GameFlowSystem.DefaultImplements.DataAccess;
using KahaGameCore.GameFlowSystem.GameEventsIntegration;
using KahaGameCore.Parameters;
using KahaGameCore.StaticData;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem.Tests
{
    public class GameFlowGameEventIntegrationTests
    {
        private sealed class FakeLocationService : IGameFlowLocationService
        {
            public int CurrentLocationID => 1;
        }

        private sealed class FakeAction : IGameFlowAction
        {
            public int ID => 7;
            public string Name => "睡覺";
            public string Description => string.Empty;
            public string TriggerTiming => "Action:Sleep";
            public Vector2 AnchoredPosition => Vector2.zero;
            public string MenuGroup => string.Empty;
        }

        private sealed class FakeActionProvider : IGameFlowActionProvider
        {
            private static readonly IGameFlowAction Action = new FakeAction();
            private static readonly IReadOnlyList<IGameFlowAction> Actions =
                new[] { Action };

            public IReadOnlyList<IGameFlowAction> GetVisibleActions(int locationId) => Actions;
            public bool IsEnabled(IGameFlowAction action) => true;
        }

        private sealed class CancelOnSecondSelectionPresenter : IActionMenuPresenter
        {
            private readonly CancellationTokenSource cancellation;
            private int selectionCount;

            public CancelOnSecondSelectionPresenter(CancellationTokenSource cancellation)
            {
                this.cancellation = cancellation;
            }

            public UniTask<IGameFlowAction> SelectActionAsync(
                IReadOnlyList<ActionMenuEntry> entries)
            {
                selectionCount++;
                if (selectionCount == 1)
                {
                    return UniTask.FromResult(entries[0].Action);
                }

                cancellation.Cancel();
                return UniTask.FromResult<IGameFlowAction>(null);
            }
        }

        [Test]
        public void ActionTiming_RealRunnerExecutesPhaseCommand_BeforeFlowReenters()
        {
            TextAsset phaseTable = new TextAsset(
                "[{\"ID\":1,\"Key\":\"Day\",\"DisplayName\":\"白天\",\"NextID\":2,\"IsNewDay\":0}," +
                "{\"ID\":2,\"Key\":\"Night\",\"DisplayName\":\"夜晚\",\"NextID\":1,\"IsNewDay\":0}]")
            {
                name = nameof(TimePhaseData)
            };
            GameStaticDataManager staticData = new GameStaticDataManager();
            staticData.Add<TimePhaseData>(
                new TextAssetJsonStaticDataHandler(new[] { phaseTable }));
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Day", "天數", 1, 1, 999)
            });
            TimeService timeService = new TimeService(staticData, parameters);
            timeService.ResetToFirstPhase();

            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(new EffectCommandDefinition(
                name: "AdvancePhase",
                displayName: "AdvancePhase",
                category: "Game Flow",
                Array.Empty<EffectCommandParameterDefinition>(),
                new AdvancePhaseCommand(timeService)));
            EffectRuntime runtime = new EffectRuntime(registry);
            GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();
            TextAsset actionEvent = new TextAsset(
                "{\"SchemaVersion\":1," +
                "\"DocumentGuid\":\"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa\"," +
                "\"DisplayName\":\"睡覺\"," +
                "\"TriggerTiming\":\"Action:Sleep\"," +
                "\"Condition\":\"\"," +
                "\"Priority\":100," +
                "\"Commands\":\"AdvancePhase();\"}");
            GameEventRunner runner = new GameEventRunner(
                new GameEventCatalog(new[] { actionEvent }, codec),
                runtime,
                parameters,
                codec);

            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            {
                GameFlowController controller = new GameFlowController(
                    timeService,
                    new FakeLocationService(),
                    new FakeActionProvider(),
                    new GameFlowGameEventAdapter(runner),
                    new CancelOnSecondSelectionPresenter(cancellation));

                controller.RunNewGameAsync(cancellation.Token).GetAwaiter().GetResult();
            }

            Assert.That(timeService.CurrentPhase.Key, Is.EqualTo("Night"));
        }
    }
}
