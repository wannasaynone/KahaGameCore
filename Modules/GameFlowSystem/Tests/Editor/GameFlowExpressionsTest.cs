using System;
using System.Collections.Generic;
using System.Threading;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.GameFlowSystem.DefaultImplements.Commands;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.Parameters;
using KahaGameCore.Effects;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace KahaGameCore.GameFlowSystem.Tests
{
    public class GameFlowExpressionsTest
    {
        private sealed class TokenCapturingCommand : IEffectCommand
        {
            public CancellationToken ReceivedToken { get; private set; }

            public UniTask ExecuteAsync(
                EffectExecutionContext context,
                IReadOnlyList<string> arguments,
                CancellationToken cancellationToken)
            {
                ReceivedToken = cancellationToken;
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeLocationService : ILocationService
        {
            public int CurrentLocationID { get; private set; }
            public LocationData CurrentLocation => null;
            public void ResetToInitial() => CurrentLocationID = 0;
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
        public void Adapter_UsesParameterStoreForBothCalculationAndCondition()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Supplies", "物資", initialValue: 12, minValue: 0, maxValue: 9999)
            });
            GameFlowExpressions expressions = new GameFlowExpressions(parameters);

            int calculated = expressions.CalculateInt("$Supplies * 2");
            bool condition = expressions.Evaluate("$Supplies >= 10");

            Assert.That(calculated, Is.EqualTo(24));
            Assert.That(condition, Is.True);
        }

        [Test]
        public void CommandExecutor_UsesSharedEffectRuntimeToSetParameter()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Base", "Base", 4, 0, 100),
                ParameterDefinition.Int("Result", "Result", 0, 0, 100)
            });
            GameFlowExpressions expressions = new GameFlowExpressions(parameters);
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(new EffectCommandDefinition(
                name: "SetParameter",
                displayName: "Set Parameter",
                category: "Parameters",
                new[]
                {
                    new EffectCommandParameterDefinition("key", EffectCommandParameterKind.ParameterKey),
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.NumberExpression)
                },
                new SetParameterCommand(parameters, expressions)));
            EffectRuntime runtime = new EffectRuntime(registry);
            ICommandExecutor executor = new EffectCommandExecutor(runtime);

            executor.ExecuteAsync("SetParameter(Result,$Base * 3)", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(parameters.GetInt("Result"), Is.EqualTo(12));
        }

        [Test]
        public void CommandExecutor_ForwardsCallerCancellationTokenToEffectCommand()
        {
            TokenCapturingCommand command = new TokenCapturingCommand();
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(new EffectCommandDefinition(
                name: "CaptureToken",
                displayName: "Capture Token",
                category: "Tests",
                Array.Empty<EffectCommandParameterDefinition>(),
                command));
            ICommandExecutor executor = new EffectCommandExecutor(new EffectRuntime(registry));
            using CancellationTokenSource cancellation = new CancellationTokenSource();

            executor.ExecuteAsync("CaptureToken()", cancellation.Token).GetAwaiter().GetResult();

            Assert.That(command.ReceivedToken, Is.EqualTo(cancellation.Token));
        }

        [Test]
        public void CommandExecutor_CancelledEffectThrowsOperationCanceledExceptionWithCallerToken()
        {
            TokenCapturingCommand command = new TokenCapturingCommand();
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(new EffectCommandDefinition(
                name: "CaptureToken",
                displayName: "Capture Token",
                category: "Tests",
                Array.Empty<EffectCommandParameterDefinition>(),
                command));
            ICommandExecutor executor = new EffectCommandExecutor(new EffectRuntime(registry));
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            OperationCanceledException exception = Assert.Throws<OperationCanceledException>(() =>
                executor.ExecuteAsync("CaptureToken()", cancellation.Token).GetAwaiter().GetResult());

            Assert.That(exception.CancellationToken, Is.EqualTo(cancellation.Token));
        }

        [Test]
        public void AddParameter_InvalidExpressionDoesNotMutateParameter()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Supplies", "物資", initialValue: 12, minValue: 0, maxValue: 9999)
            });
            AddParameterCommand command = new AddParameterCommand(parameters, new GameFlowExpressions(parameters));

            Assert.Throws<GameFlowExpressionException>(() =>
                Execute(command, "Supplies", "$Missing + 1"));

            Assert.That(parameters.GetInt("Supplies"), Is.EqualTo(12));
        }

        [Test]
        public void SetParameter_UsesCalculationPath()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Base", "基準", initialValue: 4, minValue: 0, maxValue: 99),
                ParameterDefinition.Int("Result", "結果", initialValue: 0, minValue: 0, maxValue: 99)
            });

            Execute(new SetParameterCommand(parameters, new GameFlowExpressions(parameters)), "Result", "$Base * 3");

            Assert.That(parameters.GetInt("Result"), Is.EqualTo(12));
        }

        [Test]
        public void SetParameter_SetsBoolFromConditionExpression()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Day", "天數", initialValue: 2, minValue: 1, maxValue: 999),
                ParameterDefinition.Bool("OutingUnlocked", "外出解鎖", initialValue: false)
            });

            Execute(
                new SetParameterCommand(parameters, new GameFlowExpressions(parameters)),
                "OutingUnlocked",
                "$Day >= 2");

            Assert.That(parameters.GetBool("OutingUnlocked"), Is.True);
        }

        [Test]
        public void AddParameter_AddsFloatCalculation()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Float("Speed", "速度", initialValue: 1.5f, minValue: 0f, maxValue: 10f),
                ParameterDefinition.Float("Bonus", "加成", initialValue: 0.25f, minValue: 0f, maxValue: 1f)
            });

            Execute(
                new AddParameterCommand(parameters, new GameFlowExpressions(parameters)),
                "Speed",
                "$Bonus * 2");

            Assert.That(parameters.GetFloat("Speed"), Is.EqualTo(2f));
        }

        [Test]
        public void SetParameter_SetsStringLiteralWithoutExpressionMode()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.String("PlayerName", "玩家名稱", initialValue: "Mia")
            });

            Execute(
                new SetParameterCommand(parameters, new GameFlowExpressions(parameters)),
                "PlayerName",
                "Noah");

            Assert.That(parameters.GetString("PlayerName"), Is.EqualTo("Noah"));
        }

        [Test]
        public void SetParameter_SetsFloatFromCalculation()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Float("Speed", "速度", initialValue: 1.5f, minValue: 0f, maxValue: 10f)
            });

            Execute(
                new SetParameterCommand(parameters, new GameFlowExpressions(parameters)),
                "Speed",
                "$Speed + 0.25");

            Assert.That(parameters.GetFloat("Speed"), Is.EqualTo(1.75f));
        }

        [Test]
        public void MoveToLocation_UsesCalculationPath()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Destination", "目的地", initialValue: 6, minValue: 1, maxValue: 99)
            });
            FakeLocationService locations = new FakeLocationService();

            Execute(
                new MoveToLocationCommand(new GameFlowExpressions(parameters), locations),
                "$Destination + 1");

            Assert.That(locations.CurrentLocationID, Is.EqualTo(7));
        }

        [Test]
        public void StartDialogue_UsesCalculationPath()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Dialogue", "對話", initialValue: 9, minValue: 1, maxValue: 999)
            });
            FakeDialoguePlayer player = new FakeDialoguePlayer();

            Execute(
                new StartDialogueCommand(new GameFlowExpressions(parameters), player),
                "$Dialogue");

            Assert.That(player.PlayedId, Is.EqualTo(9));
        }

        [Test]
        public void ShowHint_UsesCalculationPath()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Hint", "提示", initialValue: 11, minValue: 1, maxValue: 999)
            });
            FakeTextProvider text = new FakeTextProvider();
            FakeHintPresenter presenter = new FakeHintPresenter();

            Execute(
                new ShowHintCommand(new GameFlowExpressions(parameters), text, presenter),
                "$Hint");

            Assert.That(text.RequestedId, Is.EqualTo(11));
            Assert.That(presenter.ShownText, Is.EqualTo("Text 11"));
        }

        private static void Execute(IEffectCommand command, params string[] arguments)
        {
            command.ExecuteAsync(
                    new EffectExecutionContext(),
                    arguments,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }
    }
}
