using System;
using KahaGameCore.Expressions.Internal;

namespace KahaGameCore.Expressions
{
    public sealed class Expressions
    {
        private readonly ExpressionCache cache = new ExpressionCache();
        private readonly Random random = new Random();

        public ExpressionResult<float> Calculate(string formula, IExpressionContext context)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return ExpressionResult<float>.Failure(new ExpressionError(ExpressionErrorCode.EmptyExpression, "Calculation is empty.", 0, 0));
            CompiledExpression compiled = cache.GetOrCompile(formula, ExpressionEntryPoint.Calculation);
            if (!compiled.IsSuccess) return ExpressionResult<float>.Failure(compiled.Error);
            ExpressionEvaluation evaluated = new ExpressionEvaluator(context, true, random).Evaluate(compiled.Root);
            if (!evaluated.IsSuccess) return ExpressionResult<float>.Failure(evaluated.Error);
            if (evaluated.Value.Type != ExpressionValueType.Number)
                return ExpressionResult<float>.Failure(new ExpressionError(ExpressionErrorCode.TypeMismatch, "Calculation must produce a number.", 0, formula.Length));
            return ExpressionResult<float>.Success(evaluated.Value.Number);
        }

        public ExpressionResult<bool> EvaluateCondition(string condition, IExpressionContext context)
        {
            if (string.IsNullOrWhiteSpace(condition)) return ExpressionResult<bool>.Success(true);
            CompiledExpression compiled = cache.GetOrCompile(condition, ExpressionEntryPoint.Condition);
            if (!compiled.IsSuccess) return ExpressionResult<bool>.Failure(compiled.Error);
            ExpressionEvaluation evaluated = new ExpressionEvaluator(context, false, random).Evaluate(compiled.Root);
            if (!evaluated.IsSuccess) return ExpressionResult<bool>.Failure(evaluated.Error);
            if (evaluated.Value.Type != ExpressionValueType.Boolean)
                return ExpressionResult<bool>.Failure(new ExpressionError(ExpressionErrorCode.TypeMismatch, "Condition must produce a boolean.", 0, condition.Length));
            return ExpressionResult<bool>.Success(evaluated.Value.Boolean);
        }
    }
}
