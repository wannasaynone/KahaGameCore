using System;

namespace KahaGameCore.GameEvents
{
    public sealed class GameEventDocument
    {
        public GameEventDocument(
            int schemaVersion,
            Guid documentGuid,
            string displayName,
            string triggerTiming,
            string condition,
            string commands)
        {
            if (documentGuid == Guid.Empty)
            {
                throw new ArgumentException("DocumentGuid must not be empty.", nameof(documentGuid));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("DisplayName is required.", nameof(displayName));
            }

            if (triggerTiming == null) throw new ArgumentNullException(nameof(triggerTiming));
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            if (commands == null) throw new ArgumentNullException(nameof(commands));

            SchemaVersion = schemaVersion;
            DocumentGuid = documentGuid;
            DisplayName = displayName;
            TriggerTiming = triggerTiming;
            Condition = condition;
            Commands = commands;
        }

        public int SchemaVersion { get; }
        public Guid DocumentGuid { get; }
        public string DisplayName { get; }
        public string TriggerTiming { get; }
        public string Condition { get; }
        public string Commands { get; }
    }
}
