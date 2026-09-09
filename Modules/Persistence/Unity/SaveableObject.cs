using System;
using UnityEngine;

namespace KahaGameCore.Persistence
{
    /// <summary>
    /// Marks a runtime-spawned object that must survive Scene unloading and
    /// saving. The prefab has to live under a Resources folder, because the
    /// save file stores its Resources path as the object's identity.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SaveableObject : MonoBehaviour
    {
        [Tooltip("Resources path of this prefab, without extension.")]
        [SerializeField] private string resourcePath;

        private static string restoringId;
        private static SaveableObjectRegistry restoringRegistry;

        private SaveableObjectRegistry registry;

        public string Id { get; private set; }
        public string ResourcePath => resourcePath;

        internal static void BeginRestore(
            string id,
            SaveableObjectRegistry targetRegistry)
        {
            restoringId = id;
            restoringRegistry = targetRegistry;
        }

        internal static void EndRestore()
        {
            restoringId = null;
            restoringRegistry = null;
        }

        private void Awake()
        {
            if (restoringId != null)
            {
                Id = restoringId;
                registry = restoringRegistry;
            }
            else
            {
                Id = Guid.NewGuid().ToString("N");
                registry = SaveableObjects.Registry;
            }

            registry.Attach(this);
        }

        private void OnDestroy()
        {
            // Also fires on Scene unload, so this only drops the live link.
            // The record stays until Discard says the object is really gone.
            registry?.Detach(this);
        }

        /// <summary>
        /// Forgets this object permanently: it will not come back on reload or
        /// re-entering the Scene. Call this when the object is consumed —
        /// exploded, picked up, cleaned up — never when it merely leaves the
        /// Scene. Destroying the GameObject is left to the caller, so a death
        /// animation can still play afterwards.
        /// </summary>
        public void Discard()
        {
            registry?.Discard(Id);
            registry = null;
        }

        /// <summary>
        /// Sets the Resources path from code. The object must still be inactive,
        /// because Awake registers it.
        /// </summary>
        public void Configure(string newResourcePath)
        {
            resourcePath = newResourcePath;
        }
    }
}
