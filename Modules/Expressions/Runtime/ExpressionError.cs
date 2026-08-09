namespace KahaGameCore.Expressions
{
    public enum ExpressionErrorCode
    {
        EmptyExpression,
        UnexpectedToken,
        InvalidNumber,
        UnknownSymbol,
        TypeMismatch,
        DivisionByZero,
        UnknownFunction,
        FunctionNotAllowed,
        InvalidArguments
    }

    public sealed class ExpressionError
    {
        public ExpressionError(ExpressionErrorCode code, string message, int position, int length)
        {
            Code = code;
            Message = message;
            Position = position;
            Length = length;
        }

        public ExpressionErrorCode Code { get; }
        public string Message { get; }
        public int Position { get; }
        public int Length { get; }

        public override string ToString()
        {
            return $"{Code}: {Message} (position {Position}, length {Length})";
        }
    }
}
