namespace KahaGameCore.Expressions.Internal
{
    internal enum ExpressionTokenKind
    {
        Number, Identifier, Parameter,
        Plus, Minus, Star, Slash,
        LeftParenthesis, RightParenthesis, Comma,
        Greater, GreaterOrEqual, Less, LessOrEqual, Equal, NotEqual,
        And, Or, Not, End
    }

    internal readonly struct ExpressionToken
    {
        public ExpressionToken(ExpressionTokenKind kind, string text, int position, int length, float number = 0f)
        {
            Kind = kind;
            Text = text;
            Position = position;
            Length = length;
            Number = number;
        }

        public ExpressionTokenKind Kind { get; }
        public string Text { get; }
        public int Position { get; }
        public int Length { get; }
        public float Number { get; }
    }
}
