namespace KahaGameCore.Expressions
{
    public readonly struct ExpressionResult<T>
    {
        private ExpressionResult(bool isSuccess, T value, ExpressionError error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public bool IsSuccess { get; }
        public T Value { get; }
        public ExpressionError Error { get; }

        public static ExpressionResult<T> Success(T value)
        {
            return new ExpressionResult<T>(true, value, null);
        }

        public static ExpressionResult<T> Failure(ExpressionError error)
        {
            return new ExpressionResult<T>(false, default, error);
        }
    }
}
