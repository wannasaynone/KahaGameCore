using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using KahaGameCore.Package.GameFlowSystem;

namespace KahaGameCore.Package.GameFlowSystem.Tests
{
    public class GameFlowControllerTest
    {
        private class FakePhase : IGameFlowTimePhase
        {
            public int ID { get; set; }
            public string Key { get; set; }
            public bool AllowAction { get; set; }
        }

        private class FakeState : IGameFlowState
        {
            public int ResetCount { get; private set; }
            public void ResetToInitial() => ResetCount++;
        }

        private class FakeTimeService : IGameFlowTimeService
        {
            public List<FakePhase> Phases = new List<FakePhase>();
            public int Index { get; private set; }
            public int AdvanceCount { get; private set; }

            public IGameFlowTimePhase CurrentPhase => Phases[Index];
            public void ResetToFirstPhase() => Index = 0;
            public void AdvanceTime()
            {
                AdvanceCount++;
                Index = (Index + 1) % Phases.Count;
            }
        }

        private class FakeLocationService : IGameFlowLocationService
        {
            public int CurrentLocationID { get; set; }
        }

        private class FakeAction : IGameFlowAction
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Commands { get; set; }
            public Vector2 AnchoredPosition { get; set; }
        }

        private class FakeActionProvider : IGameFlowActionProvider
        {
            public List<IGameFlowAction> Actions = new List<IGameFlowAction>();
            public IReadOnlyList<IGameFlowAction> GetVisibleActions(int locationId) => Actions;
            public bool IsEnabled(IGameFlowAction action) => true;
        }

        /// <summary>記錄所有時機點；達到 CancelAfter 次數後取消流程，避免測試卡在無限迴圈。</summary>
        private class FakeTriggerService : IGameFlowEventTriggerService
        {
            public readonly List<string> RaisedTimings = new List<string>();
            public CancellationTokenSource CancelSource;
            public int CancelAfter = int.MaxValue;
            public Action<string> OnRaised;

            public UniTask RaiseTimingAsync(string timing, CancellationToken cancellationToken = default)
            {
                RaisedTimings.Add(timing);
                OnRaised?.Invoke(timing);
                if (RaisedTimings.Count >= CancelAfter)
                {
                    CancelSource.Cancel();
                }
                return UniTask.CompletedTask;
            }
        }

        private class FakeCommandExecutor : IGameFlowCommandExecutor
        {
            public readonly List<string> ExecutedCommands = new List<string>();
            public UniTask ExecuteAsync(string rawCommands)
            {
                ExecutedCommands.Add(rawCommands);
                return UniTask.CompletedTask;
            }
        }

        private class FakeActionMenuPresenter : IActionMenuPresenter
        {
            public Func<IReadOnlyList<ActionMenuEntry>, IGameFlowAction> OnSelect;
            public UniTask<IGameFlowAction> SelectActionAsync(IReadOnlyList<ActionMenuEntry> entries)
            {
                return UniTask.FromResult(OnSelect(entries));
            }
        }

        private FakeState state;
        private FakeTimeService timeService;
        private FakeLocationService locationService;
        private FakeActionProvider actionProvider;
        private FakeTriggerService triggerService;
        private FakeCommandExecutor commandExecutor;
        private FakeActionMenuPresenter presenter;
        private CancellationTokenSource cancelSource;

        [SetUp]
        public void SetUp()
        {
            state = new FakeState();
            timeService = new FakeTimeService();
            locationService = new FakeLocationService();
            actionProvider = new FakeActionProvider();
            triggerService = new FakeTriggerService();
            commandExecutor = new FakeCommandExecutor();
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
                timeService, locationService, actionProvider,
                triggerService, commandExecutor, presenter);
        }

        private void Run()
        {
            // 開新局的重置由組裝根負責，這裡先模擬呼叫端重置，再跑流程。
            state.ResetToInitial();
            timeService.ResetToFirstPhase();

            // 所有 Fake 都同步完成，整個流程會同步跑完直到取消。
            CreateController().RunNewGameAsync(cancelSource.Token).GetAwaiter().GetResult();
        }

        [Test]
        public void RunNewGame_RaisesGameStartThenPhaseStart_AndAutoAdvancesNonActionPhase()
        {
            timeService.Phases.Add(new FakePhase { ID = 1, Key = "Morning", AllowAction = false });
            timeService.Phases.Add(new FakePhase { ID = 2, Key = "Night", AllowAction = false });
            triggerService.CancelAfter = 3;

            Run();

            Assert.AreEqual(1, state.ResetCount);
            CollectionAssert.AreEqual(
                new[] { "GameStart", "PhaseStart:Morning", "PhaseStart:Night" },
                triggerService.RaisedTimings);
            Assert.AreEqual(1, timeService.AdvanceCount);
        }

        [Test]
        public void ActionPhase_ExecutesChosenActionCommands_ThenRaisesAfterAction()
        {
            timeService.Phases.Add(new FakePhase { ID = 1, Key = "Day", AllowAction = true });
            actionProvider.Actions.Add(new FakeAction { ID = 7, Name = "料理", Commands = "AddValue(Satiety,20)" });
            presenter.OnSelect = entries => entries[0].Action;
            triggerService.CancelAfter = 3;

            Run();

            CollectionAssert.AreEqual(
                new[] { "GameStart", "PhaseStart:Day", "AfterAction:7" },
                triggerService.RaisedTimings);
            CollectionAssert.AreEqual(new[] { "AddValue(Satiety,20)" }, commandExecutor.ExecutedCommands);
        }

        [Test]
        public void ActionPhase_WithNoVisibleActions_AdvancesTimeToAvoidStall()
        {
            timeService.Phases.Add(new FakePhase { ID = 1, Key = "Day", AllowAction = true });
            timeService.Phases.Add(new FakePhase { ID = 2, Key = "Night", AllowAction = false });
            triggerService.CancelAfter = 3;

            Run();

            CollectionAssert.AreEqual(
                new[] { "GameStart", "PhaseStart:Day", "PhaseStart:Night" },
                triggerService.RaisedTimings);
            Assert.AreEqual(1, timeService.AdvanceCount);
        }

        [Test]
        public void LocationMovedDuringEvent_RaisesEnterLocation()
        {
            timeService.Phases.Add(new FakePhase { ID = 1, Key = "Morning", AllowAction = false });
            locationService.CurrentLocationID = 1;
            triggerService.OnRaised = timing =>
            {
                if (timing == GameFlowTimings.GameStart)
                {
                    locationService.CurrentLocationID = 5;
                }
            };
            triggerService.CancelAfter = 3;

            Run();

            CollectionAssert.AreEqual(
                new[] { "GameStart", "EnterLocation:5", "PhaseStart:Morning" },
                triggerService.RaisedTimings);
        }

        [Test]
        public void PresenterReturnsNull_FlowExitsRoundWithoutExecutingCommands()
        {
            timeService.Phases.Add(new FakePhase { ID = 1, Key = "Day", AllowAction = true });
            actionProvider.Actions.Add(new FakeAction { ID = 7, Name = "料理", Commands = "AddValue(Satiety,20)" });
            presenter.OnSelect = entries =>
            {
                // 模擬返回標題：取消流程並讓選擇以 null 結束。
                cancelSource.Cancel();
                return null;
            };

            Run();

            CollectionAssert.AreEqual(new[] { "GameStart", "PhaseStart:Day" }, triggerService.RaisedTimings);
            Assert.AreEqual(0, commandExecutor.ExecutedCommands.Count);
        }
    }
}
