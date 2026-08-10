using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.Parameters;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>AddParameter(Key, Formula)：將 Int／Float Parameter 加上公式結果。</summary>
    public sealed class AddParameterCommand : IEffectCommand
    {
        private readonly ParameterStore parameters;
        private readonly GameFlowExpressions expressions;

        public AddParameterCommand(ParameterStore parameters, GameFlowExpressions expressions)
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
            if (!parameters.TryGetValue(arguments[0], out ParameterValue currentValue))
            {
                throw new UnknownParameterException(arguments[0]);
            }

            switch (currentValue.Type)
            {
                case ParameterType.Int:
                    parameters.Add(arguments[0], expressions.CalculateInt(arguments[1]));
                    break;
                case ParameterType.Float:
                    parameters.Add(arguments[0], expressions.CalculateNumber(arguments[1]));
                    break;
                default:
                    throw new InvalidOperationException(
                        $"AddParameter does not support {currentValue.Type} parameter '{arguments[0]}'.");
            }

            return UniTask.CompletedTask;
        }
    }
}
