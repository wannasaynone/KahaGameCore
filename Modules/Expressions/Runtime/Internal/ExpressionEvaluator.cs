using System;

namespace KahaGameCore.Expressions.Internal
{
    internal sealed class ExpressionEvaluation
    {
        private ExpressionEvaluation(ExpressionValue value, ExpressionError error) { Value = value; Error = error; }
        public ExpressionValue Value { get; }
        public ExpressionError Error { get; }
        public bool IsSuccess => Error == null;
        public static ExpressionEvaluation Success(ExpressionValue value) => new ExpressionEvaluation(value, null);
        public static ExpressionEvaluation Failure(ExpressionError error) => new ExpressionEvaluation(default, error);
    }

    internal sealed class ExpressionEvaluator
    {
        private readonly IExpressionContext context;
        private readonly bool allowRandom;
        private readonly Random random;

        public ExpressionEvaluator(IExpressionContext context, bool allowRandom, Random random)
        {
            this.context = context;
            this.allowRandom = allowRandom;
            this.random = random;
        }

        public ExpressionEvaluation Evaluate(ExpressionSyntax syntax)
        {
            if (syntax is LiteralExpressionSyntax literal) return ExpressionEvaluation.Success(literal.Value);
            if (syntax is SymbolExpressionSyntax symbol) return EvaluateSymbol(symbol);
            if (syntax is UnaryExpressionSyntax unary) return EvaluateUnary(unary);
            if (syntax is BinaryExpressionSyntax binary) return EvaluateBinary(binary);
            if (syntax is FunctionExpressionSyntax function) return EvaluateFunction(function);
            return Failure(ExpressionErrorCode.UnexpectedToken, "Unknown expression node.", syntax);
        }

        private ExpressionEvaluation EvaluateSymbol(SymbolExpressionSyntax syntax)
        {
            if (context != null && context.TryResolve(syntax.Symbol, out ExpressionValue value)) return ExpressionEvaluation.Success(value);
            return Failure(ExpressionErrorCode.UnknownSymbol, $"Unknown symbol '{syntax.Symbol}'.", syntax);
        }

        private ExpressionEvaluation EvaluateUnary(UnaryExpressionSyntax syntax)
        {
            ExpressionEvaluation operand = Evaluate(syntax.Operand);
            if (!operand.IsSuccess) return operand;
            if (syntax.Operation == ExpressionTokenKind.Minus && operand.Value.Type == ExpressionValueType.Number)
                return ExpressionEvaluation.Success(ExpressionValue.FromNumber(-operand.Value.Number));
            if (syntax.Operation == ExpressionTokenKind.Not && operand.Value.Type == ExpressionValueType.Boolean)
                return ExpressionEvaluation.Success(ExpressionValue.FromBoolean(!operand.Value.Boolean));
            return Failure(ExpressionErrorCode.TypeMismatch, syntax.Operation == ExpressionTokenKind.Minus ? "Expected a number." : "Expected a boolean.", syntax);
        }

        private ExpressionEvaluation EvaluateBinary(BinaryExpressionSyntax syntax)
        {
            ExpressionEvaluation left = Evaluate(syntax.Left);
            if (!left.IsSuccess) return left;

            ExpressionEvaluation right = Evaluate(syntax.Right);
            if (!right.IsSuccess) return right;

            switch (syntax.Operation)
            {
                case ExpressionTokenKind.Plus:
                case ExpressionTokenKind.Minus:
                case ExpressionTokenKind.Star:
                case ExpressionTokenKind.Slash:
                    if (!Numbers(left.Value, right.Value)) return TypeFailure("Arithmetic operands must be numbers.", syntax);
                    if (syntax.Operation == ExpressionTokenKind.Slash && right.Value.Number == 0f) return Failure(ExpressionErrorCode.DivisionByZero, "Division by zero.", syntax);
                    float number = syntax.Operation == ExpressionTokenKind.Plus ? left.Value.Number + right.Value.Number
                        : syntax.Operation == ExpressionTokenKind.Minus ? left.Value.Number - right.Value.Number
                        : syntax.Operation == ExpressionTokenKind.Star ? left.Value.Number * right.Value.Number
                        : left.Value.Number / right.Value.Number;
                    return ExpressionEvaluation.Success(ExpressionValue.FromNumber(number));
                case ExpressionTokenKind.Greater:
                case ExpressionTokenKind.GreaterOrEqual:
                case ExpressionTokenKind.Less:
                case ExpressionTokenKind.LessOrEqual:
                    if (!Numbers(left.Value, right.Value)) return TypeFailure("Comparison operands must be numbers.", syntax);
                    bool comparison = syntax.Operation == ExpressionTokenKind.Greater ? left.Value.Number > right.Value.Number
                        : syntax.Operation == ExpressionTokenKind.GreaterOrEqual ? left.Value.Number >= right.Value.Number
                        : syntax.Operation == ExpressionTokenKind.Less ? left.Value.Number < right.Value.Number
                        : left.Value.Number <= right.Value.Number;
                    return ExpressionEvaluation.Success(ExpressionValue.FromBoolean(comparison));
                case ExpressionTokenKind.Equal:
                case ExpressionTokenKind.NotEqual:
                    if (left.Value.Type != right.Value.Type) return TypeFailure("Equality operands must have the same type.", syntax);
                    bool equal = left.Value.Type == ExpressionValueType.Number ? left.Value.Number == right.Value.Number : left.Value.Boolean == right.Value.Boolean;
                    return ExpressionEvaluation.Success(ExpressionValue.FromBoolean(syntax.Operation == ExpressionTokenKind.Equal ? equal : !equal));
                case ExpressionTokenKind.And:
                case ExpressionTokenKind.Or:
                    if (left.Value.Type != ExpressionValueType.Boolean || right.Value.Type != ExpressionValueType.Boolean) return TypeFailure("Logical operands must be booleans.", syntax);
                    return ExpressionEvaluation.Success(ExpressionValue.FromBoolean(syntax.Operation == ExpressionTokenKind.And ? left.Value.Boolean && right.Value.Boolean : left.Value.Boolean || right.Value.Boolean));
                default:
                    return Failure(ExpressionErrorCode.UnexpectedToken, "Unsupported operator.", syntax);
            }
        }

        private ExpressionEvaluation EvaluateFunction(FunctionExpressionSyntax syntax)
        {
            if (syntax.Name != "Random") return Failure(ExpressionErrorCode.UnknownFunction, $"Unknown function '{syntax.Name}'.", syntax);
            if (!allowRandom) return Failure(ExpressionErrorCode.FunctionNotAllowed, "Random is not allowed in conditions.", syntax);
            if (syntax.Arguments.Count != 2) return Failure(ExpressionErrorCode.InvalidArguments, "Random expects two arguments.", syntax);
            ExpressionEvaluation minimum = Evaluate(syntax.Arguments[0]);
            if (!minimum.IsSuccess) return minimum;
            ExpressionEvaluation maximum = Evaluate(syntax.Arguments[1]);
            if (!maximum.IsSuccess) return maximum;
            if (!Numbers(minimum.Value, maximum.Value)) return TypeFailure("Random arguments must be numbers.", syntax);
            if (maximum.Value.Number < minimum.Value.Number) return Failure(ExpressionErrorCode.InvalidArguments, "Random maximum must be greater than or equal to minimum.", syntax);
            double sample;
            lock (random) sample = random.NextDouble();
            return ExpressionEvaluation.Success(ExpressionValue.FromNumber(minimum.Value.Number + (float)sample * (maximum.Value.Number - minimum.Value.Number)));
        }

        private static bool Numbers(ExpressionValue left, ExpressionValue right) => left.Type == ExpressionValueType.Number && right.Type == ExpressionValueType.Number;
        private static ExpressionEvaluation TypeFailure(string message, ExpressionSyntax syntax) => Failure(ExpressionErrorCode.TypeMismatch, message, syntax);
        private static ExpressionEvaluation Failure(ExpressionErrorCode code, string message, ExpressionSyntax syntax) => ExpressionEvaluation.Failure(new ExpressionError(code, message, syntax.Position, syntax.Length));
    }
}
