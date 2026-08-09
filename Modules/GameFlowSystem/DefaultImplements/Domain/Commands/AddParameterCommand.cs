using System;
using KahaGameCore.Parameters;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>AddParameter(Key, Formula)：將 Int／Float Parameter 加上公式結果。</summary>
    public sealed class AddParameterCommand : KahaGameCore.Effects.EffectCommandBase
    {
        private readonly ParameterStore parameters;
        private readonly GameFlowExpressions expressions;

        public AddParameterCommand(ParameterStore parameters, GameFlowExpressions expressions)
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
                    parameters.Add(vars[0], expressions.CalculateInt(vars[1]));
                    break;
                case ParameterType.Float:
                    parameters.Add(vars[0], expressions.CalculateNumber(vars[1]));
                    break;
                default:
                    throw new InvalidOperationException(
                        $"AddParameter does not support {currentValue.Type} parameter '{vars[0]}'.");
            }

            onCompleted?.Invoke();
        }
    }
}
