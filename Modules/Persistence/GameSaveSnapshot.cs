using System;
using KahaGameCore.Parameters;

namespace KahaGameCore.Persistence
{
    public sealed class GameSaveSnapshot
    {
        internal GameSaveSnapshot(string sceneKey, ParameterSnapshot parameters)
        {
            SceneKey = sceneKey;
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        public string SceneKey { get; }
        public ParameterSnapshot Parameters { get; }
    }
}
