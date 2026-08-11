using System;
using System.Collections.Generic;

namespace KahaGameCore.Persistence
{
    /// <summary>
    /// Opaque heterogeneous participant state produced and consumed by a registry.
    /// </summary>
    public sealed class SaveParticipantSnapshotSet
    {
        private readonly Dictionary<string, Entry> entries;

        internal SaveParticipantSnapshotSet(Dictionary<string, Entry> entries)
        {
            this.entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        internal IReadOnlyDictionary<string, Entry> Entries => entries;

        internal sealed class Entry
        {
            public Entry(Type snapshotType, object snapshot)
            {
                SnapshotType = snapshotType;
                Snapshot = snapshot;
            }

            public Type SnapshotType { get; }
            public object Snapshot { get; }
        }
    }
}
