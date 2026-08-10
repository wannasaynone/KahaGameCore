using System;
using System.Collections.Generic;
using JsonFx.Json;

namespace KahaGameCore.GameEvents
{
    public sealed class GameEventDocumentJsonCodec
    {
        public const int CurrentSchemaVersion = 1;

        private sealed class DocumentDto
        {
            public int SchemaVersion;
            public string DocumentGuid;
            public string DisplayName;
            public string TriggerTiming;
            public string Condition;
            public int Priority;
            public string Commands;
        }

        public GameEventDocument Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new GameEventException("EmptyDocument", "Game Event JSON is empty.");
            }

            DocumentDto dto;
            Dictionary<string, object> fields;
            try
            {
                fields = JsonReader.Deserialize<Dictionary<string, object>>(json);
                dto = JsonReader.Deserialize<DocumentDto>(json);
            }
            catch (Exception exception)
            {
                throw new GameEventException("InvalidJson", "Game Event JSON is invalid.", exception);
            }

            if (dto == null)
            {
                throw new GameEventException("InvalidDocument", "Game Event document is missing.");
            }

            RequireField(fields, "SchemaVersion");
            RequireField(fields, "DocumentGuid");
            RequireField(fields, "DisplayName");
            RequireField(fields, "Condition");
            RequireField(fields, "Priority");
            RequireField(fields, "Commands");

            if (dto.SchemaVersion != CurrentSchemaVersion)
            {
                throw new GameEventException(
                    "UnsupportedSchemaVersion",
                    $"Unsupported Game Event SchemaVersion {dto.SchemaVersion}.");
            }

            if (!Guid.TryParse(dto.DocumentGuid, out Guid documentGuid) || documentGuid == Guid.Empty)
            {
                throw new GameEventException("InvalidDocumentGuid", "DocumentGuid must be a valid GUID.");
            }

            if (string.IsNullOrWhiteSpace(dto.DisplayName))
            {
                throw new GameEventException("MissingDisplayName", "DisplayName is required.");
            }

            if (dto.Condition == null)
            {
                throw new GameEventException("MissingCondition", "Condition is required; use an empty string for no condition.");
            }

            if (dto.Commands == null)
            {
                throw new GameEventException("MissingCommands", "Commands is required; use an empty string for no commands.");
            }

            return new GameEventDocument(
                dto.SchemaVersion,
                documentGuid,
                dto.DisplayName,
                dto.TriggerTiming ?? string.Empty,
                dto.Condition,
                dto.Priority,
                dto.Commands);
        }

        private static void RequireField(
            IReadOnlyDictionary<string, object> fields,
            string name)
        {
            if (fields == null || !fields.ContainsKey(name))
            {
                throw new GameEventException(
                    "MissingField",
                    $"Game Event field '{name}' is required.");
            }
        }

        public string Write(GameEventDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return JsonWriter.Serialize(new DocumentDto
            {
                SchemaVersion = document.SchemaVersion,
                DocumentGuid = document.DocumentGuid.ToString("D"),
                DisplayName = document.DisplayName,
                TriggerTiming = document.TriggerTiming,
                Condition = document.Condition,
                Priority = document.Priority,
                Commands = document.Commands
            });
        }
    }
}
