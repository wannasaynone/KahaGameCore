using System;

namespace KahaGameCore.Parameters
{
    public readonly struct ParameterRuntimeValue
    {
        public ParameterRuntimeValue(
            ParameterDefinition definition,
            ParameterValue value)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Value = value;
        }

        public ParameterDefinition Definition { get; }
        public ParameterValue Value { get; }
    }
}
