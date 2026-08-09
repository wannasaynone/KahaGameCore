namespace KahaGameCore.Expressions
{
    public enum ExpressionValueType
    {
        Number,
        Boolean
    }

    public readonly struct ExpressionValue
    {
        private ExpressionValue(ExpressionValueType type, float number, bool boolean)
        {
            Type = type;
            Number = number;
            Boolean = boolean;
        }

        public ExpressionValueType Type { get; }
        public float Number { get; }
        public bool Boolean { get; }

        public static ExpressionValue FromNumber(float value)
        {
            return new ExpressionValue(ExpressionValueType.Number, value, false);
        }

        public static ExpressionValue FromBoolean(bool value)
        {
            return new ExpressionValue(ExpressionValueType.Boolean, 0f, value);
        }
    }
}
