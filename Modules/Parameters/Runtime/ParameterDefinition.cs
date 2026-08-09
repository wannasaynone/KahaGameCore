namespace KahaGameCore.Parameters
{
    public sealed class ParameterDefinition
    {
        private ParameterDefinition(
            string key,
            string displayName,
            ParameterValue initialValue,
            ParameterValue? minValue,
            ParameterValue? maxValue)
        {
            Validate(key, initialValue, minValue, maxValue);
            Key = key;
            DisplayName = displayName;
            InitialValue = initialValue;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public string Key { get; }
        public string DisplayName { get; }

        public ParameterType Type => InitialValue.Type;
        public ParameterValue InitialValue { get; }
        public ParameterValue? MinValue { get; }
        public ParameterValue? MaxValue { get; }

        public static ParameterDefinition Int(
            string key,
            string displayName,
            int initialValue,
            int minValue,
            int maxValue)
        {
            return new ParameterDefinition(
                key,
                displayName,
                ParameterValue.FromInt(initialValue),
                ParameterValue.FromInt(minValue),
                ParameterValue.FromInt(maxValue));
        }

        public static ParameterDefinition Float(
            string key,
            string displayName,
            float initialValue,
            float minValue,
            float maxValue)
        {
            return new ParameterDefinition(
                key,
                displayName,
                ParameterValue.FromFloat(initialValue),
                ParameterValue.FromFloat(minValue),
                ParameterValue.FromFloat(maxValue));
        }

        public static ParameterDefinition Bool(
            string key,
            string displayName,
            bool initialValue)
        {
            return new ParameterDefinition(
                key,
                displayName,
                ParameterValue.FromBool(initialValue),
                null,
                null);
        }

        public static ParameterDefinition String(
            string key,
            string displayName,
            string initialValue)
        {
            return new ParameterDefinition(
                key,
                displayName,
                ParameterValue.FromString(initialValue),
                null,
                null);
        }

        private static void Validate(
            string key,
            ParameterValue initialValue,
            ParameterValue? minValue,
            ParameterValue? maxValue)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidParameterDefinitionException(key, "Key is required.");
            }

            if (initialValue.Type == ParameterType.Int)
            {
                int min = minValue.Value.AsInt();
                int max = maxValue.Value.AsInt();
                int initial = initialValue.AsInt();
                if (min > max || initial < min || initial > max)
                {
                    throw new InvalidParameterDefinitionException(key, "InitialValue must be within MinValue and MaxValue.");
                }

                return;
            }

            if (initialValue.Type == ParameterType.Float)
            {
                float min = minValue.Value.AsFloat();
                float max = maxValue.Value.AsFloat();
                float initial = initialValue.AsFloat();
                if (min > max || initial < min || initial > max)
                {
                    throw new InvalidParameterDefinitionException(key, "InitialValue must be within MinValue and MaxValue.");
                }

                return;
            }

            if (minValue.HasValue || maxValue.HasValue)
            {
                throw new InvalidParameterDefinitionException(key, "Bool and String parameters cannot declare numeric bounds.");
            }

            if (initialValue.Type == ParameterType.String && initialValue.AsString() == null)
            {
                throw new InvalidParameterDefinitionException(key, "String InitialValue cannot be null.");
            }
        }
    }
}
