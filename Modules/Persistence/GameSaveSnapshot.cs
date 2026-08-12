using System;
using KahaGameCore.Parameters;

namespace KahaGameCore.Persistence
{
    public sealed class GameSaveSnapshot
    {
        internal GameSaveSnapshot(
            string sceneKey,
            ParameterSnapshot parameters,
            SaveParticipantSnapshotSet participants)
        {
            SceneKey = sceneKey;
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            Participants = participants ?? throw new ArgumentNullException(nameof(participants));
        }

        public string SceneKey { get; }
        public ParameterSnapshot Parameters { get; }
        public SaveParticipantSnapshotSet Participants { get; }
    }
}
