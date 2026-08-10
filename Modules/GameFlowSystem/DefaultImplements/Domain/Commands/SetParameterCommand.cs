using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.Parameters;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>SetParameter(Key, Value)：依目標型別解讀 calculation、condition 或 string literal。</summary>
    public sealed class SetParameterCommand : IEffectCommand
    {
        private readonly ParameterStore parameters;
        private readonly GameFlowExpressions expressions;

        public SetParameterCommand(ParameterStore parameters, GameFlowExpressions expressions)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.expressions = expressions ?? throw new ArgumentNullException(nameof(expressions));
        }

        public UniTask ExecuteAsync(
            EffectExecutionContext context,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExecuteCore(arguments);
            return UniTask.CompletedTask;
        }

        private void ExecuteCore(IReadOnlyList<string> arguments)
        {
            if (!parameters.TryGetValue(arguments[0], out ParameterValue currentValue))
            {
                throw new UnknownParameterException(arguments[0]);
            }

            switch (currentValue.Type)
            {
                case ParameterType.Int:
                    parameters.Set(arguments[0], expressions.CalculateInt(arguments[1]));
                    break;
                case ParameterType.Float:
                    parameters.Set(arguments[0], expressions.CalculateNumber(arguments[1]));
                    break;
                case ParameterType.Bool:
                    parameters.Set(arguments[0], expressions.Evaluate(arguments[1]));
                    break;
                case ParameterType.String:
                    parameters.Set(arguments[0], arguments[1]);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"SetParameter does not support {currentValue.Type} parameter '{arguments[0]}'.");
            }
        }
    }
}
