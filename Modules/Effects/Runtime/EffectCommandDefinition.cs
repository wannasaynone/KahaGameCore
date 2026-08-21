using System;
using System.Collections.Generic;

namespace KahaGameCore.Effects
{
    public enum EffectCommandParameterKind
    {
        Literal,
        NumberExpression,
        ConditionExpression,
        ParameterKey,
        TextKey,
        AssetKey,
        ParameterValue
    }

    public sealed class EffectCommandParameterDefinition
    {
        public EffectCommandParameterDefinition(
            string name,
            EffectCommandParameterKind kind,
            string optionSourceKey = null,
            int parameterKeySourceIndex = -1)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Parameter name is required.", nameof(name));
            }

            Name = name;
            Kind = kind;
            OptionSourceKey = optionSourceKey?.Trim() ?? string.Empty;
            if (kind == EffectCommandParameterKind.ParameterValue)
            {
                if (parameterKeySourceIndex < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(parameterKeySourceIndex),
                        "ParameterValue requires a ParameterKey source argument.");
                }
            }
            else if (parameterKeySourceIndex >= 0)
            {
                throw new ArgumentException(
                    "Only ParameterValue can specify a ParameterKey source argument.",
                    nameof(parameterKeySourceIndex));
            }

            ParameterKeySourceIndex = parameterKeySourceIndex;
        }

        public string Name { get; }
        public EffectCommandParameterKind Kind { get; }
        public string OptionSourceKey { get; }
        public int ParameterKeySourceIndex { get; }
    }

    public sealed class EffectCommandDescriptor
    {
        public EffectCommandDescriptor(
            string name,
            string displayName,
            string category,
            IEnumerable<EffectCommandParameterDefinition> parameters)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Command name is required.", nameof(name));
            }

            Name = name;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName;
            Category = category ?? string.Empty;
            var collectedParameters = new List<EffectCommandParameterDefinition>(
                parameters ?? throw new ArgumentNullException(nameof(parameters)));
            for (int index = 0; index < collectedParameters.Count; index++)
            {
                EffectCommandParameterDefinition parameter = collectedParameters[index];
                if (parameter.Kind != EffectCommandParameterKind.ParameterValue)
                {
                    continue;
                }

                if (parameter.ParameterKeySourceIndex >= collectedParameters.Count ||
                    collectedParameters[parameter.ParameterKeySourceIndex].Kind !=
                        EffectCommandParameterKind.ParameterKey)
                {
                    throw new ArgumentException(
                        $"ParameterValue '{parameter.Name}' must reference a " +
                        "ParameterKey argument in the same command.",
                        nameof(parameters));
                }
            }

            Parameters = collectedParameters.AsReadOnly();
        }

        public string Name { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public IReadOnlyList<EffectCommandParameterDefinition> Parameters { get; }
    }

    public sealed class EffectCommandDefinition
    {
        public EffectCommandDefinition(
            string name,
            string displayName,
            string category,
            IEnumerable<EffectCommandParameterDefinition> parameters,
            IEffectCommand command)
            : this(
                new EffectCommandDescriptor(name, displayName, category, parameters),
                command)
        {
        }

        public EffectCommandDefinition(
            EffectCommandDescriptor descriptor,
            IEffectCommand command)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public EffectCommandDescriptor Descriptor { get; }
        public string Name => Descriptor.Name;
        public string DisplayName => Descriptor.DisplayName;
        public string Category => Descriptor.Category;
        public IReadOnlyList<EffectCommandParameterDefinition> Parameters => Descriptor.Parameters;
        internal IEffectCommand Command { get; }
    }
}
