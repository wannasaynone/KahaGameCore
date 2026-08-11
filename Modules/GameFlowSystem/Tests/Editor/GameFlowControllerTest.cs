using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem.Tests
{
    public class GameFlowControllerTest
    {
        private sealed class FakePhase : IGameFlowTimePhase
        {
            public int ID { get; set; }
            public string Key { get; set; }
        }

        private sealed class FakeTimeService : IGameFlowTimeService
        {
            public readonly List<FakePhase> Phases = new List<FakePhase>();
            public int Index { get; private set; }
            public int AdvanceCount { get; private set; }
            public IGameFlowTimePhase CurrentPhase => Phases[Index];

            public void ResetToFirstPhase() => Index = 0;

            public void AdvancePhase()
            {
                AdvanceCount++;
                Index = (Index + 1) % Phases.Count;
            }
        }

        private sealed class FakeLocationService : IGameFlowLocationService
        {
            public int CurrentLocationID { get; set; }
        }

        private sealed class FakeAction : IGameFlowAction
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string TriggerTiming { get; set; }
            public Vector2 AnchoredPosition { get; set; }
            public string MenuGroup { get; set; }
        }

        private sealed class FakeActionProvider : IGameFlowActionProvider
        {
            public readonly List<IGameFlowAction> Actions = new List<IGameFlowAction>();
            public readonly HashSet<int> DisabledActionIds = new HashSet<int>();
            public IReadOnlyList<IGameFlowAction> GetVisibleActions(int locationId) => Actions;
            public bool IsEnabled(IGameFlowAction action) => !DisabledActionIds.Contains(action.ID);
        }

        private sealed class FakeTriggerService : IGameFlowEventTriggerService
        {
            public readonly List<string> RaisedTimings = new List<string>();
            public CancellationTokenSource CancelSource;
            public int CancelAfter = int.MaxValue;
            public Action<string> OnRaised;
            public Func<string, UniTask> OnRaiseAsync;

            public async UniTask RaiseTimingAsync(
                string timing,
                CancellationToken cancellationToken = default)
            {
                RaisedTimings.Add(timing);
                OnRaised?.Invoke(timing);
                if (OnRaiseAsync != null)
                {
                    await OnRaiseAsync(timing);
                }

                if (RaisedTimings.Count >= CancelAfter)
                {
                    CancelSource.Cancel();
                }
            }
        }

        private sealed class FakeActionMenuPresenter : IActionMenuPresenter
        {
            public Func<IReadOnlyList<ActionMenuEntry>, IGameFlowAction> OnSelect;

            public UniTask<IGameFlowAction> SelectActionAsync(IReadOnlyList<ActionMenuEntry> entries)
            {
                return UniTask.FromResult(OnSelect(entries));
            }
        }

        private FakeTimeService timeService;
        private FakeLocationService locationService;
        private FakeActionProvider actionProvider;
        private FakeTriggerService triggerService;
        private FakeActionMenuPresenter presenter;
        private CancellationTokenSource cancelSource;

        [SetUp]
        public void SetUp()
        {
            timeService = new FakeTimeService();
            locationService = new FakeLocationService();
            actionProvider = new FakeActionProvider();
            triggerService = new FakeTriggerService();
            presenter = new FakeActionMenuPresenter();
            cancelSource = new CancellationTokenSource();
            triggerService.CancelSource = cancelSource;
        }

        [TearDown]
        public void TearDown()
        {
            cancelSource.Dispose();
        }

        private GameFlowController CreateController()
        {
            return new GameFlowController(
                timeService,
                locationService,
                actionProvider,
                triggerService,
                presenter);
        }

        private void Run()
        {
            timeService.ResetToFirstPhase();
            CreateController().RunNewGameAsync(cancelSource.Token).GetAwaiter().GetResult();
        }

        [Test]
        public void ActionPhase_RaisesChosenActionTriggerTiming_ThenAfterAction()
        {
            timeService.Phases.Add(new FakePhase { ID = 1, Key = "Day" });
            actionProvider.Actions.Add(new FakeAction
            {
                ID = 7,
                Name = "料理",
                TriggerTiming = "Action:Cook"
            });
            presenter.OnSelect = entries => entries[0].Action;
            triggerService.CancelAfter = 5;

            Run();

            CollectionAssert.AreEqual(
                new[] { "GameStart", "PhaseStart", "PhaseStart:Day", "Action:Cook", "AfterAction" },
                triggerService.RaisedTimings);
        }

        [Test]
        public void ActionPhase_AwaitsActionEventBeforeAfterAction()
        {
            timeService.Phases.Add(new FakePhase { ID = 1, Key = "Day" });
            actionProvider.Actions.Add(new FakeAction
            {
                ID = 7,
                Name = "料理",
                TriggerTiming = "Action:Cook"
            });
            presenter.OnSelect = entries => entries[0].Action;
            triggerService.CancelAfter = 5;
            UniTaskCompletionSource actionEvent = new UniTaskCompletionSource();
            triggerService.OnRaiseAsync = timing =>
                timing == "Action:Cook" ? actionEvent.Task : UniTask.CompletedTask;

            UniTask flow = CreateController().RunNewGameAsync(cancelSource.Token);

            CollectionAssert.AreEqual(
                new[] { "GameStart", "PhaseStart", "PhaseStart:Day", "Action:Cook" },
                triggerService.RaisedTimings);
            Assert.IsFalse(triggerService.RaisedTimings.Contains("AfterAction"));

            actionEvent.TrySetResult();
            flow.GetAwaiter().GetResult();
            Assert.AreEqual("AfterAction", triggerService.RaisedTimings.Last());
        }

        [Test]
        public void NoVisibleActions_AdvancesPhase()
        {
            timeService.Phases.Add(new FakePhase { ID = 1, Key = "Day" });
            timeService.Phases.Add(new FakePhase { ID = 2, Key = "Night" });
            triggerService.CancelAfter = 5;

            Run();

            Assert.AreEqual(1, timeService.AdvanceCount);
            CollectionAssert.AreEqual(
                new[] { "GameStart", "PhaseStart", "PhaseStart:Day", "PhaseStart", "PhaseStart:Night" },
                triggerService.RaisedTimings);
        }

        [Test]
        public void AllVisibleActionsDisabled_AdvancesPhaseWithoutOpeningMenu()
        {
            timeService.Phases.Add(new FakePhase { ID = 1, Key = "Day" });
            timeService.Phases.Add(new FakePhase { ID = 2, Key = "Night" });
            actionProvider.Actions.Add(new FakeAction { ID = 7, TriggerTiming = "Action:Cook" });
            actionProvider.DisabledActionIds.Add(7);
            triggerService.CancelAfter = 5;
            presenter.OnSelect = _ => throw new AssertionException("全 disabled 時不應開啟 Action menu。");

            Run();

            Assert.AreEqual(1, timeService.AdvanceCount);
        }

        [Test]
        public void AutomaticAdvanceCycle_ThrowsInsteadOfLoopingForever()
        {
            timeService.Phases.Add(new FakePhase { ID = 1, Key = "Day" });
            timeService.Phases.Add(new FakePhase { ID = 2, Key = "Night" });

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(Run);

            StringAssert.Contains("自動推進形成循環", exception.Message);
            Assert.AreEqual(2, timeService.AdvanceCount);
        }

        [Test]
        public void NewGame_ClearsAutomaticAdvanceCycleHistory()
        {
            timeService.Phases.Add(new FakePhase { ID = 1, Key = "Day" });
            timeService.Phases.Add(new FakePhase { ID = 2, Key = "Night" });
            GameFlowController controller = CreateController();
            triggerService.CancelAfter = 5;

            timeService.ResetToFirstPhase();
            controller.RunNewGameAsync(cancelSource.Token).GetAwaiter().GetResult();

            cancelSource.Dispose();
            cancelSource = new CancellationTokenSource();
            triggerService.CancelSource = cancelSource;
            triggerService.RaisedTimings.Clear();
            triggerService.CancelAfter = 4;
            timeService.ResetToFirstPhase();

            Assert.DoesNotThrow(() =>
                controller.RunNewGameAsync(cancelSource.Token).GetAwaiter().GetResult());
            Assert.AreEqual(2, timeService.AdvanceCount);
        }

        [Test]
        public void PhaseChangedByActionEvent_ReentersNewPhaseAfterEventQueueCompletes()
        {
            timeService.Phases.Add(new FakePhase { ID = 1, Key = "Day" });
            timeService.Phases.Add(new FakePhase { ID = 2, Key = "Night" });
            actionProvider.Actions.Add(new FakeAction { ID = 7, TriggerTiming = "Action:Sleep" });
            presenter.OnSelect = entries => entries[0].Action;
            triggerService.OnRaised = timing =>
            {
                if (timing == "Action:Sleep")
                {
                    timeService.AdvancePhase();
                }
            };
            triggerService.CancelAfter = 7;

            Run();

            CollectionAssert.AreEqual(
                new[]
                {
                    "GameStart", "PhaseStart", "PhaseStart:Day", "Action:Sleep", "AfterAction",
                    "PhaseStart", "PhaseStart:Night"
                },
                triggerService.RaisedTimings);
        }

        [Test]
        public void LocationMovedDuringEvent_RaisesEnterLocation()
        {
            timeService.Phases.Add(new FakePhase { ID = 1, Key = "Morning" });
            locationService.CurrentLocationID = 1;
            triggerService.OnRaised = timing =>
            {
                if (timing == GameFlowTimings.GameStart)
                {
                    locationService.CurrentLocationID = 5;
                }
            };
            triggerService.CancelAfter = 4;

            Run();

            CollectionAssert.AreEqual(
                new[] { "GameStart", "EnterLocation:5", "PhaseStart", "PhaseStart:Morning" },
                triggerService.RaisedTimings);
        }

        [Test]
        public void PresenterReturnsNull_AfterCancellationDoesNotRaiseActionTiming()
        {
            timeService.Phases.Add(new FakePhase { ID = 1, Key = "Day" });
            actionProvider.Actions.Add(new FakeAction { ID = 7, TriggerTiming = "Action:Cook" });
            presenter.OnSelect = entries =>
            {
                cancelSource.Cancel();
                return null;
            };

            Run();

            CollectionAssert.AreEqual(
                new[] { "GameStart", "PhaseStart", "PhaseStart:Day" },
                triggerService.RaisedTimings);
        }
    }
}
