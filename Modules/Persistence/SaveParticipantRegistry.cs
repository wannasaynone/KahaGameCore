using System;
using System.Collections.Generic;

namespace KahaGameCore.Persistence
{
    /// <summary>
    /// Coordinates explicitly registered save participants. Capture and Restore
    /// traverse registration order for deterministic behavior, but participants
    /// must not use that order to model gameplay dependencies.
    /// </summary>
    public sealed class SaveParticipantRegistry
    {
        private readonly Dictionary<string, IParticipantAdapter> participants =
            new Dictionary<string, IParticipantAdapter>(StringComparer.Ordinal);
        private readonly List<string> registrationOrder = new List<string>();

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
            registrationOrder.Add(participant.SaveKey);
        }

        public SaveParticipantSnapshotSet Capture()
        {
            Dictionary<string, SaveParticipantSnapshotSet.Entry> entries =
                new Dictionary<string, SaveParticipantSnapshotSet.Entry>(
                    StringComparer.Ordinal);
            foreach (string saveKey in registrationOrder)
            {
                entries.Add(
                    saveKey,
                    new SaveParticipantSnapshotSet.Entry(
                        participants[saveKey].SnapshotType,
                        participants[saveKey].Capture()));
            }

            return new SaveParticipantSnapshotSet(entries);
        }

        internal bool TryGetSnapshotType(string saveKey, out Type snapshotType)
        {
            if (participants.TryGetValue(
                    saveKey,
                    out IParticipantAdapter participant))
            {
                snapshotType = participant.SnapshotType;
                return true;
            }

            snapshotType = null;
            return false;
        }

        public void Restore(SaveParticipantSnapshotSet snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            Validate(snapshot);

            foreach (string saveKey in registrationOrder)
            {
                SaveParticipantSnapshotSet.Entry entry = snapshot.Entries[saveKey];
                participants[saveKey].Restore(entry.Snapshot);
            }
        }

        internal void Validate(SaveParticipantSnapshotSet snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            foreach (string saveKey in registrationOrder)
            {
                if (!snapshot.Entries.TryGetValue(
                    saveKey,
                    out SaveParticipantSnapshotSet.Entry entry))
                {
                    throw new InvalidOperationException(
                        $"Save participant snapshot is missing key '{saveKey}'.");
                }

                if (entry.SnapshotType != participants[saveKey].SnapshotType)
                {
                    throw new InvalidOperationException(
                        $"Save participant snapshot type for '{saveKey}' is " +
                        $"'{entry.SnapshotType.FullName}', expected " +
                        $"'{participants[saveKey].SnapshotType.FullName}'.");
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
