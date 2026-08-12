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
        private readonly SaveParticipantDocumentCodec participantCodec =
            new SaveParticipantDocumentCodec();

        public string Write(
            string sceneKey,
            ParameterSnapshot parameters,
            SaveParticipantSnapshotSet participants)
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
                Participants = participantCodec.Encode(participants)
            };
            return JsonWriter.Serialize(document);
        }

        public GameSaveSnapshot Read(
            string json,
            SaveParticipantRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));

            GameSaveDocument document = ReadDocument(json);
            return new GameSaveSnapshot(
                document.SceneKey,
                parameterCodec.Decode(document.Parameters),
                participantCodec.Decode(document.Participants, registry));
        }

        internal GameSaveDocument ReadDocument(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            GameSaveDocument document =
                JsonReader.Deserialize<GameSaveDocument>(json);
            Validate(document);
            return document;
        }

        internal ParameterSnapshot DecodeParameters(
            GameSaveDocument document)
        {
            Validate(document);
            return parameterCodec.Decode(document.Parameters);
        }

        internal SaveParticipantSnapshotSet DecodeRegisteredParticipants(
            GameSaveDocument document,
            SaveParticipantRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));

            Validate(document);
            return participantCodec.DecodeRegistered(
                document.Participants,
                registry);
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

            if (document.Participants == null)
            {
                throw new InvalidOperationException(
                    "Game save document is missing Participants.");
            }
        }
    }
}
