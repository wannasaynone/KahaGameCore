using System;
using System.Collections.Generic;

namespace KahaGameCore.GameEvents.Editor
{
    public sealed class EffectCommandArgumentEditorContext
    {
        private readonly Action<string> setValue;

        public EffectCommandArgumentEditorContext(
            string documentGuid,
            string commandName,
            int argumentIndex,
            IReadOnlyList<string> arguments,
            Action<string> setValue)
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

            DocumentGuid = documentGuid?.Trim() ?? string.Empty;
            CommandName = commandName.Trim();
            ArgumentIndex = argumentIndex;
            Arguments = new List<string>(
                    arguments ?? Array.Empty<string>())
                .AsReadOnly();
            this.setValue = setValue ?? throw new ArgumentNullException(nameof(setValue));
        }

        public string DocumentGuid { get; }
        public string CommandName { get; }
        public int ArgumentIndex { get; }
        public IReadOnlyList<string> Arguments { get; }
        public string Value => ArgumentIndex < Arguments.Count
            ? Arguments[ArgumentIndex] ?? string.Empty
            : string.Empty;

        public void SetValue(string value)
        {
            setValue(value ?? string.Empty);
        }
    }

    public interface IEffectCommandArgumentEditorProvider
    {
        string SourceKey { get; }
        void Draw(EffectCommandArgumentEditorContext context);
    }
}
