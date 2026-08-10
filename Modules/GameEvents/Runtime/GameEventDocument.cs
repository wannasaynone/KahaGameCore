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
            int priority,
            string commands)
        {
            SchemaVersion = schemaVersion;
            DocumentGuid = documentGuid;
            DisplayName = displayName;
            TriggerTiming = triggerTiming;
            Condition = condition;
            Priority = priority;
            Commands = commands;
        }

        public int SchemaVersion { get; }
        public Guid DocumentGuid { get; }
        public string DisplayName { get; }
        public string TriggerTiming { get; }
        public string Condition { get; }
        public int Priority { get; }
        public string Commands { get; }
    }
}
