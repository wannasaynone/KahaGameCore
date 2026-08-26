using System;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.GameEvents
{
    public sealed class GameEventCatalog
    {
        internal sealed class Entry
        {
            public Entry(GameEventDocument document)
            {
                Document = document;
            }

            public GameEventDocument Document { get; }
        }

        private readonly List<Entry> entries = new List<Entry>();
        private readonly Dictionary<Guid, Entry> entriesByDocumentGuid =
            new Dictionary<Guid, Entry>();

        public GameEventCatalog(
            GameEventCatalogAsset asset,
            GameEventDocumentJsonCodec codec)
            : this(
                asset != null
                    ? asset.Files
                    : throw new ArgumentNullException(nameof(asset)),
                codec)
        {
        }

        public GameEventCatalog(
            IEnumerable<TextAsset> files,
            GameEventDocumentJsonCodec codec)
        {
            if (files == null) throw new ArgumentNullException(nameof(files));
            if (codec == null) throw new ArgumentNullException(nameof(codec));

            HashSet<Guid> documentGuids = new HashSet<Guid>();
            foreach (TextAsset file in files)
            {
                if (file == null)
                {
                    throw new GameEventException("MissingFile", "Game Event catalog contains a null TextAsset.");
                }

                GameEventDocument document = codec.Read(file.text);
                if (!documentGuids.Add(document.DocumentGuid))
                {
                    throw new GameEventException(
                        "DuplicateDocumentGuid",
                        $"Duplicate Game Event DocumentGuid '{document.DocumentGuid:D}'.");
                }

                Entry entry = new Entry(document);
                entries.Add(entry);
                entriesByDocumentGuid.Add(document.DocumentGuid, entry);
            }
        }

        internal IReadOnlyList<Entry> Entries => entries;

        internal bool TryGetDocument(Guid documentGuid, out GameEventDocument document)
        {
            if (entriesByDocumentGuid.TryGetValue(documentGuid, out Entry entry))
            {
                document = entry.Document;
                return true;
            }

            document = null;
            return false;
        }
    }
}
