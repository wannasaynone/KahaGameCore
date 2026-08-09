using System.Collections.Generic;

namespace KahaGameCore.Expressions.Internal
{
    internal abstract class ExpressionSyntax
    {
        protected ExpressionSyntax(int position, int length) { Position = position; Length = length; }
        public int Position { get; }
        public int Length { get; }
    }

    internal sealed class LiteralExpressionSyntax : ExpressionSyntax
    {
        public LiteralExpressionSyntax(ExpressionValue value, int position, int length) : base(position, length) { Value = value; }
        public ExpressionValue Value { get; }
    }

    internal sealed class SymbolExpressionSyntax : ExpressionSyntax
    {
        public SymbolExpressionSyntax(string symbol, int position, int length) : base(position, length) { Symbol = symbol; }
        public string Symbol { get; }
    }

    internal sealed class UnaryExpressionSyntax : ExpressionSyntax
    {
        public UnaryExpressionSyntax(ExpressionTokenKind operation, ExpressionSyntax operand, int position, int length) : base(position, length) { Operation = operation; Operand = operand; }
        public ExpressionTokenKind Operation { get; }
        public ExpressionSyntax Operand { get; }
    }

    internal sealed class BinaryExpressionSyntax : ExpressionSyntax
    {
        public BinaryExpressionSyntax(ExpressionSyntax left, ExpressionToken operation, ExpressionSyntax right) : base(operation.Position, operation.Length) { Left = left; Operation = operation.Kind; Right = right; }
        public ExpressionSyntax Left { get; }
        public ExpressionTokenKind Operation { get; }
        public ExpressionSyntax Right { get; }
    }

    internal sealed class FunctionExpressionSyntax : ExpressionSyntax
    {
        public FunctionExpressionSyntax(string name, IReadOnlyList<ExpressionSyntax> arguments, int position, int length) : base(position, length) { Name = name; Arguments = arguments; }
        public string Name { get; }
        public IReadOnlyList<ExpressionSyntax> Arguments { get; }
    }
}
