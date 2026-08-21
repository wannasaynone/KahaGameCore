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
        AssetKey
    }

    public sealed class EffectCommandParameterDefinition
    {
        public EffectCommandParameterDefinition(
            string name,
            EffectCommandParameterKind kind,
            string optionSourceKey = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Parameter name is required.", nameof(name));
            }

            Name = name;
            Kind = kind;
            OptionSourceKey = optionSourceKey?.Trim() ?? string.Empty;
        }

        public string Name { get; }
        public EffectCommandParameterKind Kind { get; }
        public string OptionSourceKey { get; }
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
            Parameters = new List<EffectCommandParameterDefinition>(
                parameters ?? throw new ArgumentNullException(nameof(parameters))).AsReadOnly();
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
