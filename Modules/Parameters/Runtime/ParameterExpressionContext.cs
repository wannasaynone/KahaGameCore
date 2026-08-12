using KahaGameCore.Expressions;

namespace KahaGameCore.Parameters
{
    internal sealed class ParameterExpressionContext : IExpressionContext
    {
        private readonly ParameterStore parameters;

        public ParameterExpressionContext(ParameterStore parameters)
        {
            this.parameters = parameters;
        }

        public bool TryResolve(string symbol, out ExpressionValue value)
        {
            if (!parameters.TryGetValue(symbol, out ParameterValue parameterValue))
            {
                value = default;
                return false;
            }

            switch (parameterValue.Type)
            {
                case ParameterType.Int:
                    value = ExpressionValue.FromNumber(parameterValue.AsInt());
                    return true;
                case ParameterType.Float:
                    value = ExpressionValue.FromNumber(parameterValue.AsFloat());
                    return true;
                case ParameterType.Bool:
                    value = ExpressionValue.FromBoolean(parameterValue.AsBool());
                    return true;
                default:
                    value = default;
                    return false;
            }
        }
    }
}
