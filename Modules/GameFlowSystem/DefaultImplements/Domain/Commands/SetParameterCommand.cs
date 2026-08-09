using System;
using KahaGameCore.Parameters;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>SetParameter(Key, Value)：依目標型別解讀 calculation、condition 或 string literal。</summary>
    public sealed class SetParameterCommand : KahaGameCore.Effects.EffectCommandBase
    {
        private readonly ParameterStore parameters;
        private readonly GameFlowExpressions expressions;

        public SetParameterCommand(ParameterStore parameters, GameFlowExpressions expressions)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.expressions = expressions ?? throw new ArgumentNullException(nameof(expressions));
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            if (!parameters.TryGetValue(vars[0], out ParameterValue currentValue))
            {
                throw new UnknownParameterException(vars[0]);
            }

            switch (currentValue.Type)
            {
                case ParameterType.Int:
                    parameters.Set(vars[0], expressions.CalculateInt(vars[1]));
                    break;
                case ParameterType.Float:
                    parameters.Set(vars[0], expressions.CalculateNumber(vars[1]));
                    break;
                case ParameterType.Bool:
                    parameters.Set(vars[0], expressions.Evaluate(vars[1]));
                    break;
                case ParameterType.String:
                    parameters.Set(vars[0], vars[1]);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"SetParameter does not support {currentValue.Type} parameter '{vars[0]}'.");
            }

            onCompleted?.Invoke();
        }
    }
}
