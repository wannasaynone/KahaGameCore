using System;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    public sealed class EffectCommandArgumentOptionContext
    {
        public EffectCommandArgumentOptionContext(
            string commandName,
            int argumentIndex,
            IReadOnlyList<string> arguments)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                throw new ArgumentException(
                    "Command name is required.",
                    nameof(commandName));
            }

            if (argumentIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(argumentIndex));
            }

            CommandName = commandName.Trim();
            ArgumentIndex = argumentIndex;
            Arguments = new List<string>(
                    arguments ?? Array.Empty<string>())
                .AsReadOnly();
        }

        public string CommandName { get; }
        public int ArgumentIndex { get; }
        public IReadOnlyList<string> Arguments { get; }
    }

    public sealed class EffectCommandArgumentOption
    {
        public EffectCommandArgumentOption(
            string value,
            string label,
            string group = null,
            string description = null,
            UnityEngine.Object target = null,
            Action preview = null)
        {
            if (value == null)
            {
                throw new ArgumentException(
                    "Argument option value is required.",
                    nameof(value));
            }

            if (string.IsNullOrWhiteSpace(value) &&
                string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException(
                    "An empty argument option requires a label.",
                    nameof(label));
            }

            Value = value.Trim();
            Label = string.IsNullOrWhiteSpace(label) ? Value : label.Trim();
            Group = group?.Trim() ?? string.Empty;
            Description = description?.Trim() ?? string.Empty;
            Target = target;
            Preview = preview;
        }

        public string Value { get; }
        public string Label { get; }
        public string Group { get; }
        public string Description { get; }
        public UnityEngine.Object Target { get; }
        public Action Preview { get; }
    }

    public interface IEffectCommandArgumentOptionProvider
    {
        string SourceKey { get; }
        IReadOnlyList<EffectCommandArgumentOption> GetOptions(
            EffectCommandArgumentOptionContext context);
        void StopPreview();
    }
}
