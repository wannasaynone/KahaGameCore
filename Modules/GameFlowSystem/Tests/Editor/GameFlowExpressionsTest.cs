using System;
using System.Collections.Generic;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.GameFlowSystem.DefaultImplements.Commands;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace KahaGameCore.GameFlowSystem.Tests
{
    public class GameFlowExpressionsTest
    {
        private sealed class FakeGameState : IGameState
        {
            private readonly Dictionary<string, int> values = new Dictionary<string, int>();

            public int Get(string tag) => values.TryGetValue(tag, out int value) ? value : 0;
            public bool TryGet(string tag, out int value) => values.TryGetValue(tag, out value);
            public void Add(string tag, int amount) => Set(tag, Get(tag) + amount);
            public void Set(string tag, int value) => values[tag] = value;
            public void ResetToInitial() => values.Clear();
        }

        private sealed class FakeLocationService : ILocationService
        {
            public int CurrentLocationID { get; private set; }
            public LocationData CurrentLocation => null;
            public void MoveTo(int locationId) => CurrentLocationID = locationId;
            public IReadOnlyList<LocationData> GetSelectableLocations() => Array.Empty<LocationData>();
        }

        private sealed class FakeDialoguePlayer : IDialoguePlayer
        {
            public int PlayedId { get; private set; }
            public UniTask PlayAsync(int dialogueId) { PlayedId = dialogueId; return UniTask.CompletedTask; }
        }

        private sealed class FakeTextProvider : IGameTextProvider
        {
            public int RequestedId { get; private set; }
            public string GetText(int textId) { RequestedId = textId; return $"Text {textId}"; }
            public GameTextData PickRandom(string group) => null;
        }

        private sealed class FakeHintPresenter : IHintPresenter
        {
            public string ShownText { get; private set; }
            public UniTask ShowAsync(string text) { ShownText = text; return UniTask.CompletedTask; }
        }

        [Test]
        public void Adapter_UsesGameStateForBothCalculationAndCondition()
        {
            FakeGameState state = new FakeGameState();
            state.Set("Supplies", 12);
            GameFlowExpressions expressions = new GameFlowExpressions(state);

            int calculated = expressions.CalculateInt("$Supplies * 2");
            bool condition = expressions.Evaluate("$Supplies >= 10");

            Assert.That(calculated, Is.EqualTo(24));
            Assert.That(condition, Is.True);
        }

        [Test]
        public void AddValue_InvalidExpressionDoesNotMutateStateOrCompleteCommand()
        {
            FakeGameState state = new FakeGameState();
            state.Set("Supplies", 12);
            AddValueCommand command = new AddValueCommand(state, new GameFlowExpressions(state));
            bool completed = false;

            Assert.Throws<GameFlowExpressionException>(() =>
                command.Process(new[] { "Supplies", "$Missing + 1" }, () => completed = true, null));

            Assert.That(state.Get("Supplies"), Is.EqualTo(12));
            Assert.That(completed, Is.False);
        }

        [Test]
        public void SetValue_UsesCalculationPath()
        {
            FakeGameState state = new FakeGameState();
            state.Set("Base", 4);

            new SetValueCommand(state, new GameFlowExpressions(state)).Process(new[] { "Result", "$Base * 3" }, null, null);

            Assert.That(state.Get("Result"), Is.EqualTo(12));
        }

        [Test]
        public void MoveToLocation_UsesCalculationPath()
        {
            FakeGameState state = new FakeGameState();
            state.Set("Destination", 6);
            FakeLocationService locations = new FakeLocationService();

            new MoveToLocationCommand(new GameFlowExpressions(state), locations).Process(new[] { "$Destination + 1" }, null, null);

            Assert.That(locations.CurrentLocationID, Is.EqualTo(7));
        }

        [Test]
        public void StartDialogue_UsesCalculationPath()
        {
            FakeGameState state = new FakeGameState();
            state.Set("Dialogue", 9);
            FakeDialoguePlayer player = new FakeDialoguePlayer();

            new StartDialogueCommand(new GameFlowExpressions(state), player).Process(new[] { "$Dialogue" }, null, null);

            Assert.That(player.PlayedId, Is.EqualTo(9));
        }

        [Test]
        public void ShowHint_UsesCalculationPath()
        {
            FakeGameState state = new FakeGameState();
            state.Set("Hint", 11);
            FakeTextProvider text = new FakeTextProvider();
            FakeHintPresenter presenter = new FakeHintPresenter();

            new ShowHintCommand(new GameFlowExpressions(state), text, presenter).Process(new[] { "$Hint" }, null, null);

            Assert.That(text.RequestedId, Is.EqualTo(11));
            Assert.That(presenter.ShownText, Is.EqualTo("Text 11"));
        }
    }
}
