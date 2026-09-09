using System;
using KahaGameCore.Parameters;

namespace KahaGameCore.Persistence
{
    public sealed class GameSaveSnapshot
    {
        internal GameSaveSnapshot(
            string sceneKey,
            ParameterSnapshot parameters,
            SaveableObjectRecord[] objects)
        {
            SceneKey = sceneKey;
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            Objects = objects ?? Array.Empty<SaveableObjectRecord>();
        }

        public string SceneKey { get; }
        public ParameterSnapshot Parameters { get; }

        /// <summary>Runtime-spawned objects; empty for a save written before they existed.</summary>
        public SaveableObjectRecord[] Objects { get; }
    }
}
