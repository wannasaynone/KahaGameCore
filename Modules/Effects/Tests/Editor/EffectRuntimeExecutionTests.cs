using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace KahaGameCore.Effects.Tests
{
    public sealed class EffectRuntimeExecutionTests
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

        private sealed class ThrowingCommand : IEffectCommand
        {
            public UniTask ExecuteAsync(
                EffectExecutionContext context,
                IReadOnlyList<string> arguments,
                CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("boom");
            }
        }

        private sealed class CancellingCommand : IEffectCommand
        {
            public UniTask ExecuteAsync(
                EffectExecutionContext context,
                IReadOnlyList<string> arguments,
                CancellationToken cancellationToken)
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }

        [Test]
        public void NonSuccessfulResult_RequiresDiagnostic()
        {
            Assert.Throws<ArgumentNullException>(() => EffectExecutionResult.Failed(null));
            Assert.Throws<ArgumentNullException>(() => EffectExecutionResult.Cancelled(null));
        }

        [Test]
        public void ExecuteAsync_RunsRegisteredCommandsInSourceOrder()
        {
            List<string> records = new List<string>();
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
            EffectRuntime runtime = new EffectRuntime(registry);

            EffectExecutionResult result = runtime.ExecuteAsync(
                "Record(first);Record(second);",
                new EffectExecutionContext(),
                CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.IsSuccess, Is.True, result.FormatDiagnostic());
            CollectionAssert.AreEqual(new[] { "first", "second" }, records);
        }

        [Test]
        public void ExecuteAsync_ExplicitProgramRunsOnlyRequestedTiming()
        {
            List<string> records = new List<string>();
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
            EffectRuntime runtime = new EffectRuntime(registry);

            EffectExecutionResult result = runtime.ExecuteAsync(
                "Start{Record(start);}Finish{Record(finish);}",
                "Finish",
                new EffectExecutionContext(),
                CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.IsSuccess, Is.True, result.FormatDiagnostic());
            CollectionAssert.AreEqual(new[] { "finish" }, records);
        }

        [Test]
        public void ExecuteAsync_UnknownTimingReturnsStructuredFailure()
        {
            EffectRuntime runtime = new EffectRuntime(new EffectCommandRegistry());

            EffectExecutionResult result = runtime.ExecuteAsync(
                    "Start{}",
                    "Missing",
                    new EffectExecutionContext(),
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(result.Status, Is.EqualTo(EffectExecutionStatus.Failed));
            Assert.That(result.Diagnostic.Code, Is.EqualTo("UnknownTiming"));
            StringAssert.Contains("Missing", result.Diagnostic.Message);
        }

        [Test]
        public void Registry_DuplicateNameFailsFast()
        {
            EffectCommandRegistry registry = new EffectCommandRegistry();
            EffectCommandDefinition definition = Define("Same", new ThrowingCommand());
            registry.Register(definition);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => registry.Register(definition));

            StringAssert.Contains("already registered", exception.Message);
        }

        [Test]
        public void Registry_TryGetDefinitionReturnsRegisteredMetadata()
        {
            EffectCommandRegistry registry = new EffectCommandRegistry();
            EffectCommandDefinition definition = Define("Known", new ThrowingCommand());
            registry.Register(definition);

            bool found = registry.TryGetDefinition("Known", out EffectCommandDefinition actual);

            Assert.That(found, Is.True);
            Assert.That(actual, Is.SameAs(definition));
            Assert.That(registry.TryGetDefinition("Missing", out _), Is.False);
        }

        [Test]
        public void ExecuteAsync_UnknownCommandReturnsStructuredFailure()
        {
            EffectRuntime runtime = new EffectRuntime(new EffectCommandRegistry());

            EffectExecutionResult result = Execute(runtime, "Missing();");

            Assert.That(result.Status, Is.EqualTo(EffectExecutionStatus.Failed));
            Assert.That(result.Diagnostic.Code, Is.EqualTo("UnknownCommand"));
            Assert.That(result.Diagnostic.Position, Is.EqualTo(0));
            Assert.That(result.Diagnostic.Length, Is.EqualTo("Missing".Length));
        }

        [Test]
        public void ExecuteAsync_InvalidArityReturnsStructuredFailure()
        {
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(Define("NoArguments", new ThrowingCommand()));
            EffectRuntime runtime = new EffectRuntime(registry);

            EffectExecutionResult result = Execute(runtime, "NoArguments(extra);");

            Assert.That(result.Status, Is.EqualTo(EffectExecutionStatus.Failed));
            Assert.That(result.Diagnostic.Code, Is.EqualTo("InvalidArgumentCount"));
        }

        [Test]
        public void ExecuteAsync_CommandExceptionReturnsStructuredFailure()
        {
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(Define("Throw", new ThrowingCommand()));
            EffectRuntime runtime = new EffectRuntime(registry);

            EffectExecutionResult result = Execute(runtime, "Throw();");

            Assert.That(result.Status, Is.EqualTo(EffectExecutionStatus.Failed));
            Assert.That(result.Diagnostic.Code, Is.EqualTo("CommandFailed"));
            StringAssert.Contains("boom", result.Diagnostic.Message);
        }

        [Test]
        public void ExecuteAsync_CommandCancellationReturnsCancelled()
        {
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(Define("Cancel", new CancellingCommand()));
            EffectRuntime runtime = new EffectRuntime(registry);

            EffectExecutionResult result = Execute(runtime, "Cancel();");

            Assert.That(result.Status, Is.EqualTo(EffectExecutionStatus.Cancelled));
            Assert.That(result.Diagnostic.Code, Is.EqualTo("Cancelled"));
        }

        private static EffectExecutionResult Execute(EffectRuntime runtime, string source)
        {
            return runtime.ExecuteAsync(
                    source,
                    new EffectExecutionContext(),
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        private static EffectCommandDefinition Define(string name, IEffectCommand command)
        {
            return new EffectCommandDefinition(
                name,
                name,
                "Tests",
                Array.Empty<EffectCommandParameterDefinition>(),
                command);
        }
    }
}
