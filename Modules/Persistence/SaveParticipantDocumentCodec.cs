using System;
using System.Collections.Generic;
using System.Linq;
using JsonFx.Json;

namespace KahaGameCore.Persistence
{
    public sealed class SaveParticipantDocumentCodec
    {
        public SaveParticipantDocument[] Encode(
            SaveParticipantSnapshotSet snapshots)
        {
            if (snapshots == null) throw new ArgumentNullException(nameof(snapshots));

            return snapshots.Entries
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(EncodeDocument)
                .ToArray();
        }

        private static SaveParticipantDocument EncodeDocument(
            KeyValuePair<string, SaveParticipantSnapshotSet.Entry> pair)
        {
            SaveParticipantDocument document = new SaveParticipantDocument
            {
                SaveKey = pair.Key,
                Snapshot = pair.Value.Snapshot
            };
            Validate(document);
            return document;
        }

        public SaveParticipantSnapshotSet Decode(
            SaveParticipantDocument[] documents,
            SaveParticipantRegistry registry)
        {
            return Decode(documents, registry, rejectUnknownKeys: true);
        }

        internal SaveParticipantSnapshotSet DecodeRegistered(
            SaveParticipantDocument[] documents,
            SaveParticipantRegistry registry)
        {
            return Decode(documents, registry, rejectUnknownKeys: false);
        }

        private static SaveParticipantSnapshotSet Decode(
            SaveParticipantDocument[] documents,
            SaveParticipantRegistry registry,
            bool rejectUnknownKeys)
        {
            if (documents == null) throw new ArgumentNullException(nameof(documents));
            if (registry == null) throw new ArgumentNullException(nameof(registry));

            Dictionary<string, SaveParticipantSnapshotSet.Entry> entries =
                new Dictionary<string, SaveParticipantSnapshotSet.Entry>(
                    StringComparer.Ordinal);
            HashSet<string> documentKeys =
                new HashSet<string>(StringComparer.Ordinal);
            foreach (SaveParticipantDocument document in documents)
            {
                Validate(document);
                if (!documentKeys.Add(document.SaveKey))
                {
                    throw new InvalidOperationException(
                        $"Save participant document contains duplicate key " +
                        $"'{document.SaveKey}'.");
                }

                if (!registry.TryGetSnapshotType(
                        document.SaveKey,
                        out Type snapshotType))
                {
                    if (rejectUnknownKeys)
                    {
                        throw new InvalidOperationException(
                            $"Save participant document contains unknown key " +
                            $"'{document.SaveKey}'.");
                    }

                    continue;
                }

                string snapshotJson = JsonWriter.Serialize(document.Snapshot);
                object snapshot = JsonReader.Deserialize(snapshotJson, snapshotType);
                entries.Add(
                    document.SaveKey,
                    new SaveParticipantSnapshotSet.Entry(snapshotType, snapshot));
            }

            SaveParticipantSnapshotSet snapshots =
                new SaveParticipantSnapshotSet(entries);
            registry.Validate(snapshots);
            return snapshots;
        }

        private static void Validate(SaveParticipantDocument document)
        {
            if (document == null)
            {
                throw new InvalidOperationException(
                    "Save participant document contains a null entry.");
            }

            if (string.IsNullOrWhiteSpace(document.SaveKey))
            {
                throw new InvalidOperationException(
                    "Save participant document entry is missing SaveKey.");
            }

            if (document.Snapshot == null)
            {
                throw new InvalidOperationException(
                    $"Save participant document '{document.SaveKey}' is " +
                    "missing Snapshot.");
            }
        }
    }
}
