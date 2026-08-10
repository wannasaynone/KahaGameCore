using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects.Internal;

namespace KahaGameCore.Effects
{
    public sealed class EffectRuntime
    {
        public const string DefaultTiming = "Execute";

        private readonly EffectCommandRegistry registry;

        public EffectRuntime(EffectCommandRegistry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public EffectParseResult Parse(string source)
        {
            return EffectProgramParser.Parse(source);
        }

        public string Serialize(EffectProgram program)
        {
            return EffectProgramSerializer.Serialize(program);
        }

        public async UniTask<EffectExecutionResult> ExecuteAsync(
            string source,
            EffectExecutionContext context,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(source, DefaultTiming, context, cancellationToken);
        }

        public async UniTask<EffectExecutionResult> ExecuteAsync(
            string source,
            string timing,
            EffectExecutionContext context,
            CancellationToken cancellationToken)
        {
            EffectParseResult parseResult = Parse(source);
            if (!parseResult.IsSuccess)
            {
                return EffectExecutionResult.Failed(parseResult.Diagnostics[0]);
            }

            EffectTimingBlock block = null;
            for (int blockIndex = 0; blockIndex < parseResult.Program.Blocks.Count; blockIndex++)
            {
                if (string.Equals(parseResult.Program.Blocks[blockIndex].Name, timing, StringComparison.Ordinal))
                {
                    block = parseResult.Program.Blocks[blockIndex];
                    break;
                }
            }

            if (block == null)
            {
                return EffectExecutionResult.Failed(new EffectDiagnostic(
                    "UnknownTiming",
                    $"Effect timing '{timing}' does not exist.",
                    0,
                    0));
            }

            for (int index = 0; index < block.Commands.Count; index++)
            {
                EffectCommandCall call = block.Commands[index];
                if (cancellationToken.IsCancellationRequested)
                {
                    return EffectExecutionResult.Cancelled(new EffectDiagnostic(
                        "Cancelled",
                        "Effect execution was cancelled.",
                        call.Position,
                        call.Length));
                }

                if (!registry.TryGetDefinition(call.Name, out EffectCommandDefinition definition))
                {
                    return EffectExecutionResult.Failed(new EffectDiagnostic(
                        "UnknownCommand",
                        $"Effect command '{call.Name}' is not registered.",
                        call.Position,
                        call.Name.Length));
                }

                if (definition.Parameters.Count != call.Arguments.Count)
                {
                    return EffectExecutionResult.Failed(new EffectDiagnostic(
                        "InvalidArgumentCount",
                        $"Effect command '{call.Name}' expects {definition.Parameters.Count} arguments but received {call.Arguments.Count}.",
                        call.Position,
                        call.Length));
                }

                try
                {
                    await definition.Command.ExecuteAsync(
                        context ?? new EffectExecutionContext(),
                        call.Arguments,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return EffectExecutionResult.Cancelled(new EffectDiagnostic(
                        "Cancelled",
                        $"Effect command '{call.Name}' was cancelled.",
                        call.Position,
                        call.Length));
                }
                catch (Exception exception)
                {
                    return EffectExecutionResult.Failed(new EffectDiagnostic(
                        "CommandFailed",
                        $"Effect command '{call.Name}' failed: {exception.Message}",
                        call.Position,
                        call.Length));
                }
            }

            return EffectExecutionResult.Succeeded();
        }
    }
}
