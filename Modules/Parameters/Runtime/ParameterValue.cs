using System;

namespace KahaGameCore.Parameters
{
    public readonly struct ParameterValue : IEquatable<ParameterValue>
    {
        private readonly int intValue;
        private readonly float floatValue;
        private readonly bool boolValue;
        private readonly string stringValue;

        private ParameterValue(
            ParameterType type,
            int intValue,
            float floatValue,
            bool boolValue,
            string stringValue)
        {
            Type = type;
            this.intValue = intValue;
            this.floatValue = floatValue;
            this.boolValue = boolValue;
            this.stringValue = stringValue;
        }

        public ParameterType Type { get; }

        public static ParameterValue FromInt(int value)
        {
            return new ParameterValue(ParameterType.Int, value, 0f, false, null);
        }

        public static ParameterValue FromFloat(float value)
        {
            return new ParameterValue(ParameterType.Float, 0, value, false, null);
        }

        public static ParameterValue FromBool(bool value)
        {
            return new ParameterValue(ParameterType.Bool, 0, 0f, value, null);
        }

        public static ParameterValue FromString(string value)
        {
            return new ParameterValue(ParameterType.String, 0, 0f, false, value);
        }

        public int AsInt()
        {
            if (Type != ParameterType.Int)
            {
                throw new InvalidOperationException($"Parameter value is {Type}, not Int.");
            }

            return intValue;
        }

        public float AsFloat()
        {
            if (Type != ParameterType.Float)
            {
                throw new InvalidOperationException($"Parameter value is {Type}, not Float.");
            }

            return floatValue;
        }

        public bool AsBool()
        {
            if (Type != ParameterType.Bool)
            {
                throw new InvalidOperationException($"Parameter value is {Type}, not Bool.");
            }

            return boolValue;
        }

        public string AsString()
        {
            if (Type != ParameterType.String)
            {
                throw new InvalidOperationException($"Parameter value is {Type}, not String.");
            }

            return stringValue;
        }

        public bool Equals(ParameterValue other)
        {
            if (Type != other.Type)
            {
                return false;
            }

            switch (Type)
            {
                case ParameterType.Int:
                    return intValue == other.intValue;
                case ParameterType.Float:
                    return floatValue.Equals(other.floatValue);
                case ParameterType.Bool:
                    return boolValue == other.boolValue;
                case ParameterType.String:
                    return string.Equals(stringValue, other.stringValue, StringComparison.Ordinal);
                default:
                    return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is ParameterValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int valueHash;
                switch (Type)
                {
                    case ParameterType.Int:
                        valueHash = intValue;
                        break;
                    case ParameterType.Float:
                        valueHash = floatValue.GetHashCode();
                        break;
                    case ParameterType.Bool:
                        valueHash = boolValue.GetHashCode();
                        break;
                    case ParameterType.String:
                        valueHash = stringValue == null ? 0 : stringValue.GetHashCode();
                        break;
                    default:
                        valueHash = 0;
                        break;
                }

                return ((int)Type * 397) ^ valueHash;
            }
        }

        public static bool operator ==(ParameterValue left, ParameterValue right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ParameterValue left, ParameterValue right)
        {
            return !left.Equals(right);
        }
    }
}
