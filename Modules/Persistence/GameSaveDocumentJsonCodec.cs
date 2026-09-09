using System;
using JsonFx.Json;
using KahaGameCore.Parameters;

namespace KahaGameCore.Persistence
{
    public sealed class GameSaveDocumentJsonCodec
    {
        public const int CurrentSchemaVersion = 1;

        private readonly ParameterSnapshotDocumentCodec parameterCodec =
            new ParameterSnapshotDocumentCodec();

        public string Write(string sceneKey, ParameterSnapshot parameters)
        {
            return Write(sceneKey, parameters, null);
        }

        public string Write(
            string sceneKey,
            ParameterSnapshot parameters,
            SaveableObjectRecord[] objects)
        {
            if (string.IsNullOrWhiteSpace(sceneKey))
            {
                throw new ArgumentException(
                    "Game save requires SceneKey.",
                    nameof(sceneKey));
            }

            GameSaveDocument document = new GameSaveDocument
            {
                SchemaVersion = CurrentSchemaVersion,
                SceneKey = sceneKey,
                Parameters = parameterCodec.Encode(parameters),
                Objects = objects ?? Array.Empty<SaveableObjectRecord>()
            };
            return JsonWriter.Serialize(document);
        }

        public GameSaveSnapshot Read(string json)
        {
            GameSaveDocument document = ReadDocument(json);
            return new GameSaveSnapshot(
                document.SceneKey,
                parameterCodec.Decode(document.Parameters),
                document.Objects);
        }

        internal GameSaveDocument ReadDocument(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            GameSaveDocument document =
                JsonReader.Deserialize<GameSaveDocument>(json);
            Validate(document);
            return document;
        }

        internal ParameterSnapshot DecodeParameters(GameSaveDocument document)
        {
            Validate(document);
            return parameterCodec.Decode(document.Parameters);
        }

        private static void Validate(GameSaveDocument document)
        {
            if (document == null)
            {
                throw new InvalidOperationException(
                    "Game save document is null.");
            }

            if (document.SchemaVersion != CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Unsupported game save schema version " +
                    $"'{document.SchemaVersion}'.");
            }

            if (string.IsNullOrWhiteSpace(document.SceneKey))
            {
                throw new InvalidOperationException(
                    "Game save document is missing SceneKey.");
            }

            if (document.Parameters == null)
            {
                throw new InvalidOperationException(
                    "Game save document is missing Parameters.");
            }
        }
    }
}
