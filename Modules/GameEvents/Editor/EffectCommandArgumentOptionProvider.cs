using System;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
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
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Argument option value is required.",
                    nameof(value));
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
        IReadOnlyList<EffectCommandArgumentOption> GetOptions();
        void StopPreview();
    }
}
