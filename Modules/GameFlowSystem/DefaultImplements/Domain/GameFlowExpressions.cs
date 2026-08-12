using System;
using KahaGameCore.Expressions;
using KahaGameCore.Parameters;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    public sealed class GameFlowExpressions : IConditionEvaluator
    {
        private readonly ParameterStore parameters;

        public GameFlowExpressions(ParameterStore parameters)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        public int CalculateInt(string formula)
        {
            return Mathf.RoundToInt(CalculateNumber(formula));
        }

        internal float CalculateNumber(string formula)
        {
            ExpressionResult<float> result = parameters.Calculate(formula);
            if (!result.IsSuccess) throw new GameFlowExpressionException(formula, result.Error);
            return result.Value;
        }

        public bool Evaluate(string condition)
        {
            ExpressionResult<bool> result = parameters.EvaluateCondition(condition);
            if (!result.IsSuccess) throw new GameFlowExpressionException(condition, result.Error);
            return result.Value;
        }
    }
}
