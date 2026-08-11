using System;
using System.Collections.Generic;

namespace KahaGameCore.Persistence
{
    /// <summary>
    /// Coordinates explicitly registered save participants. Participants must not
    /// depend on restore order.
    /// </summary>
    public sealed class SaveParticipantRegistry
    {
        private readonly Dictionary<string, IParticipantAdapter> participants =
            new Dictionary<string, IParticipantAdapter>(StringComparer.Ordinal);

        public void Register<TSnapshot>(ISaveParticipant<TSnapshot> participant)
        {
            if (participant == null)
                throw new ArgumentNullException(nameof(participant));
            if (string.IsNullOrWhiteSpace(participant.SaveKey))
                throw new ArgumentException(
                    "Save participant requires a SaveKey.",
                    nameof(participant));
            if (participants.ContainsKey(participant.SaveKey))
                throw new InvalidOperationException(
                    $"Save participant key '{participant.SaveKey}' is registered more than once.");

            ParticipantAdapter<TSnapshot> adapter =
                new ParticipantAdapter<TSnapshot>(participant);
            participants.Add(participant.SaveKey, adapter);
        }

        public SaveParticipantSnapshotSet Capture()
        {
            Dictionary<string, SaveParticipantSnapshotSet.Entry> entries =
                new Dictionary<string, SaveParticipantSnapshotSet.Entry>(
                    StringComparer.Ordinal);
            foreach (KeyValuePair<string, IParticipantAdapter> pair in participants)
            {
                entries.Add(
                    pair.Key,
                    new SaveParticipantSnapshotSet.Entry(
                        pair.Value.SnapshotType,
                        pair.Value.Capture()));
            }

            return new SaveParticipantSnapshotSet(entries);
        }

        public void Restore(SaveParticipantSnapshotSet snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            foreach (KeyValuePair<string, IParticipantAdapter> pair in participants)
            {
                if (!snapshot.Entries.TryGetValue(
                    pair.Key,
                    out SaveParticipantSnapshotSet.Entry entry))
                {
                    throw new InvalidOperationException(
                        $"Save participant snapshot is missing key '{pair.Key}'.");
                }

                if (entry.SnapshotType != pair.Value.SnapshotType)
                {
                    throw new InvalidOperationException(
                        $"Save participant snapshot type for '{pair.Key}' is " +
                        $"'{entry.SnapshotType.FullName}', expected " +
                        $"'{pair.Value.SnapshotType.FullName}'.");
                }
            }

            foreach (KeyValuePair<string, SaveParticipantSnapshotSet.Entry> pair in
                snapshot.Entries)
            {
                if (!participants.ContainsKey(pair.Key))
                {
                    throw new InvalidOperationException(
                        $"Save participant snapshot contains unknown key '{pair.Key}'.");
                }
            }

            foreach (KeyValuePair<string, IParticipantAdapter> pair in participants)
            {
                SaveParticipantSnapshotSet.Entry entry = snapshot.Entries[pair.Key];
                pair.Value.Restore(entry.Snapshot);
            }
        }

        private interface IParticipantAdapter
        {
            Type SnapshotType { get; }
            object Capture();
            void Restore(object snapshot);
        }

        private sealed class ParticipantAdapter<TSnapshot> : IParticipantAdapter
        {
            private readonly ISaveParticipant<TSnapshot> participant;

            public ParticipantAdapter(ISaveParticipant<TSnapshot> participant)
            {
                this.participant = participant ??
                    throw new ArgumentNullException(nameof(participant));
            }

            public Type SnapshotType => typeof(TSnapshot);

            public object Capture()
            {
                return participant.Capture();
            }

            public void Restore(object snapshot)
            {
                participant.Restore((TSnapshot)snapshot);
            }
        }
    }
}
