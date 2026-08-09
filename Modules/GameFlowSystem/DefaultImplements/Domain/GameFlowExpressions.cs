using System;
using KahaGameCore.Expressions;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    public sealed class GameFlowExpressions : IConditionEvaluator
    {
        private readonly Expressions.Expressions expressions;
        private readonly IExpressionContext context;

        public GameFlowExpressions(IGameState gameState)
        {
            if (gameState == null) throw new ArgumentNullException(nameof(gameState));
            expressions = new Expressions.Expressions();
            context = new GameStateExpressionContext(gameState);
        }

        public int CalculateInt(string formula)
        {
            ExpressionResult<float> result = expressions.Calculate(formula, context);
            if (!result.IsSuccess) throw new GameFlowExpressionException(formula, result.Error);
            return Mathf.RoundToInt(result.Value);
        }

        public bool Evaluate(string condition)
        {
            ExpressionResult<bool> result = expressions.EvaluateCondition(condition, context);
            if (!result.IsSuccess) throw new GameFlowExpressionException(condition, result.Error);
            return result.Value;
        }
    }
}
