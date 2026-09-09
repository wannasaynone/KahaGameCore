using UnityEngine.SceneManagement;

namespace KahaGameCore.Persistence
{
    /// <summary>
    /// Owns the process-wide registry and respawns saved objects when their
    /// Scene loads. Scene connections load Single, so the registry cannot live
    /// on a Scene object.
    /// </summary>
    public static class SaveableObjects
    {
        private static SaveableObjectRegistry registry;

        public static SaveableObjectRegistry Registry
        {
            get
            {
                if (registry == null)
                {
                    registry = new SaveableObjectRegistry();
                    SceneManager.sceneLoaded += OnSceneLoaded;
                }

                return registry;
            }
        }

        /// <summary>
        /// Swaps in the registry a loaded save slot produced. Spawning is left
        /// to the Scene load that follows, so objects never appear in the room
        /// the player is leaving.
        /// </summary>
        public static void Reset(SaveableObjectRegistry replacement)
        {
            _ = Registry;
            registry = replacement ?? new SaveableObjectRegistry();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Registry.SpawnInto(scene.path);
        }
    }
}
