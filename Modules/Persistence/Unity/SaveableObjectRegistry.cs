using System;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Persistence
{
    /// <summary>
    /// Holds one record per runtime-spawned object that must outlive its Scene.
    /// Records for the loaded Scene are backed by a live component; records for
    /// unloaded Scenes are data only. The registry must outlive Scene loading.
    /// </summary>
    public sealed class SaveableObjectRegistry
    {
        private readonly Dictionary<string, SaveableObjectRecord> records =
            new Dictionary<string, SaveableObjectRecord>(StringComparer.Ordinal);
        private readonly Dictionary<string, SaveableObject> live =
            new Dictionary<string, SaveableObject>(StringComparer.Ordinal);

        /// <summary>
        /// Links a live object to its record, creating the record the first time
        /// the object is spawned. Called from SaveableObject.Awake.
        /// </summary>
        public void Attach(SaveableObject saveable)
        {
            if (saveable == null) throw new ArgumentNullException(nameof(saveable));
            if (string.IsNullOrWhiteSpace(saveable.ResourcePath))
            {
                throw new InvalidOperationException(
                    $"'{saveable.name}' is saveable but has no Resources path.");
            }

            if (!records.TryGetValue(saveable.Id, out SaveableObjectRecord record))
            {
                record = new SaveableObjectRecord
                {
                    Id = saveable.Id,
                    ResourcePath = saveable.ResourcePath,
                    ScenePath = saveable.gameObject.scene.path
                };
                records.Add(record.Id, record);
            }

            live[saveable.Id] = saveable;
            WritePosition(record, saveable.transform.position);
        }

        /// <summary>
        /// Drops the live link and keeps the record. Called from
        /// SaveableObject.OnDestroy, which also fires on Scene unload — an
        /// unloaded object must stay in the save, so this never removes records.
        /// Use Discard for objects that are actually gone.
        /// </summary>
        public void Detach(SaveableObject saveable)
        {
            if (saveable == null) throw new ArgumentNullException(nameof(saveable));
            if (!live.TryGetValue(saveable.Id, out SaveableObject current) ||
                current != saveable)
            {
                return;
            }

            live.Remove(saveable.Id);
            if (records.TryGetValue(saveable.Id, out SaveableObjectRecord record))
            {
                WritePosition(record, saveable.transform.position);
            }
        }

        /// <summary>Permanently forgets an object. It will not be spawned again.</summary>
        public bool Discard(string id)
        {
            live.Remove(id);
            return records.Remove(id);
        }

        public bool Contains(string id)
        {
            return records.ContainsKey(id);
        }

        /// <summary>
        /// Every record, with live objects reporting their current position so
        /// objects that moved since spawning are captured where they are now.
        /// </summary>
        public SaveableObjectRecord[] Capture()
        {
            foreach (KeyValuePair<string, SaveableObject> pair in live)
            {
                if (pair.Value != null &&
                    records.TryGetValue(pair.Key, out SaveableObjectRecord record))
                {
                    WritePosition(record, pair.Value.transform.position);
                }
            }

            SaveableObjectRecord[] captured =
                new SaveableObjectRecord[records.Count];
            records.Values.CopyTo(captured, 0);
            Array.Sort(captured, (left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            return captured;
        }

        /// <summary>
        /// Replaces every record. Objects spawned since the loaded save was
        /// written are dropped, which is what checkpoint saving means.
        /// </summary>
        public void Restore(IEnumerable<SaveableObjectRecord> restored)
        {
            if (restored == null) throw new ArgumentNullException(nameof(restored));

            records.Clear();
            live.Clear();
            foreach (SaveableObjectRecord record in restored)
            {
                if (record == null ||
                    string.IsNullOrWhiteSpace(record.Id) ||
                    string.IsNullOrWhiteSpace(record.ResourcePath))
                {
                    Debug.LogWarning(
                        "[SaveableObjectRegistry] Skipped a save record with no Id or Resources path.");
                    continue;
                }

                records[record.Id] = record;
            }
        }

        /// <summary>
        /// Instantiates every record belonging to scenePath that has no live
        /// object yet. A record whose Resources path no longer resolves is
        /// skipped and kept, so a moved prefab does not destroy the save.
        /// </summary>
        public void SpawnInto(string scenePath)
        {
            // Capture returns a copy; Spawn mutates the live map while we iterate.
            foreach (SaveableObjectRecord record in Capture())
            {
                if (string.Equals(record.ScenePath, scenePath, StringComparison.Ordinal) &&
                    !live.ContainsKey(record.Id))
                {
                    Spawn(record);
                }
            }
        }

        private void Spawn(SaveableObjectRecord record)
        {
            GameObject prefab = Resources.Load<GameObject>(record.ResourcePath);
            if (prefab == null || prefab.GetComponent<SaveableObject>() == null)
            {
                Debug.LogWarning(
                    $"[SaveableObjectRegistry] '{record.ResourcePath}' did not resolve to a " +
                    $"SaveableObject prefab; record '{record.Id}' was not spawned.");
                return;
            }

            // Awake runs inside Instantiate, so the identity has to be handed
            // over before the call or the object would register a fresh record.
            SaveableObject.BeginRestore(record.Id, this);
            try
            {
                UnityEngine.Object.Instantiate(
                    prefab,
                    new Vector3(record.X, record.Y, record.Z),
                    Quaternion.identity);
            }
            finally
            {
                SaveableObject.EndRestore();
            }
        }

        private static void WritePosition(
            SaveableObjectRecord record,
            Vector3 position)
        {
            record.X = position.x;
            record.Y = position.y;
            record.Z = position.z;
        }
    }
}
