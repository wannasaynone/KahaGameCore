using System;

namespace KahaGameCore.Parameters
{
    public abstract class ParameterException : Exception
    {
        protected ParameterException(string message) : base(message)
        {
        }
    }

    public sealed class UnknownParameterException : ParameterException
    {
        public UnknownParameterException(string key)
            : base($"Unknown parameter key '{key}'.")
        {
            Key = key;
        }

        public string Key { get; }
    }

    public sealed class ParameterTypeMismatchException : ParameterException
    {
        public ParameterTypeMismatchException(
            string key,
            ParameterType expectedType,
            ParameterType actualType)
            : base($"Parameter '{key}' expects {expectedType}, but received {actualType}.")
        {
            Key = key;
            ExpectedType = expectedType;
            ActualType = actualType;
        }

        public string Key { get; }
        public ParameterType ExpectedType { get; }
        public ParameterType ActualType { get; }
    }

    public sealed class InvalidParameterDefinitionException : ParameterException
    {
        public InvalidParameterDefinitionException(string key, string reason)
            : base($"Invalid parameter definition '{key}': {reason}")
        {
            Key = key;
        }

        public string Key { get; }
    }

    public sealed class InvalidParameterTableException : ParameterException
    {
        public InvalidParameterTableException(string tableGuid, string reason)
            : base($"Invalid parameter table '{tableGuid}': {reason}")
        {
            TableGuid = tableGuid;
        }

        public string TableGuid { get; }
    }

    public sealed class ParameterSnapshotException : ParameterException
    {
        public ParameterSnapshotException(string message) : base(message)
        {
        }
    }
}
