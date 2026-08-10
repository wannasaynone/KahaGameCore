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
        public EffectCommandParameterDefinition(string name, EffectCommandParameterKind kind)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Parameter name is required.", nameof(name));
            }

            Name = name;
            Kind = kind;
        }

        public string Name { get; }
        public EffectCommandParameterKind Kind { get; }
    }

    public sealed class EffectCommandDefinition
    {
        public EffectCommandDefinition(
            string name,
            string displayName,
            string category,
            IEnumerable<EffectCommandParameterDefinition> parameters,
            IEffectCommand command)
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
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public string Name { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public IReadOnlyList<EffectCommandParameterDefinition> Parameters { get; }
        internal IEffectCommand Command { get; }
    }
}
