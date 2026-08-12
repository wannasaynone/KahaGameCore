using KahaGameCore.Expressions;

namespace KahaGameCore.ValueContainer
{
    public sealed class ValueContainerExpressions
    {
        private readonly Expressions.Expressions expressions = new Expressions.Expressions();
        private readonly IExpressionContext context;

        public ValueContainerExpressions(
            IValueContainer caster,
            IValueContainer target,
            bool baseOnly = false)
        {
            context = new ValueContainerExpressionContext(caster, target, baseOnly);
        }

        public ExpressionResult<float> Calculate(string formula)
        {
            return expressions.Calculate(formula, context);
        }

        public ExpressionResult<bool> EvaluateCondition(string condition)
        {
            return expressions.EvaluateCondition(condition, context);
        }
    }
}
