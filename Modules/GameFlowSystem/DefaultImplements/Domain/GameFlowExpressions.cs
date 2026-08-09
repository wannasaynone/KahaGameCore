using System;
using KahaGameCore.Expressions;
using KahaGameCore.Parameters;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    public sealed class GameFlowExpressions : IConditionEvaluator
    {
        private readonly Expressions.Expressions expressions;
        private readonly IExpressionContext context;

        public GameFlowExpressions(ParameterStore parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            expressions = new Expressions.Expressions();
            context = new ParameterExpressionContext(parameters);
        }

        public int CalculateInt(string formula)
        {
            return Mathf.RoundToInt(CalculateNumber(formula));
        }

        internal float CalculateNumber(string formula)
        {
            ExpressionResult<float> result = expressions.Calculate(formula, context);
            if (!result.IsSuccess) throw new GameFlowExpressionException(formula, result.Error);
            return result.Value;
        }

        public bool Evaluate(string condition)
        {
            ExpressionResult<bool> result = expressions.EvaluateCondition(condition, context);
            if (!result.IsSuccess) throw new GameFlowExpressionException(condition, result.Error);
            return result.Value;
        }
    }
}
