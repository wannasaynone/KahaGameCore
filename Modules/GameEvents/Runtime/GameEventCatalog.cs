using System;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.GameEvents
{
    public sealed class GameEventCatalog
    {
        internal sealed class Entry
        {
            public Entry(GameEventDocument document, int inputOrder)
            {
                Document = document;
                InputOrder = inputOrder;
            }

            public GameEventDocument Document { get; }
            public int InputOrder { get; }
        }

        private readonly List<Entry> entries = new List<Entry>();

        public GameEventCatalog(
            IEnumerable<TextAsset> files,
            GameEventDocumentJsonCodec codec)
        {
            if (files == null) throw new ArgumentNullException(nameof(files));
            if (codec == null) throw new ArgumentNullException(nameof(codec));

            HashSet<Guid> documentGuids = new HashSet<Guid>();
            int inputOrder = 0;
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

                entries.Add(new Entry(document, inputOrder++));
            }
        }

        internal IReadOnlyList<Entry> Entries => entries;
    }
}
