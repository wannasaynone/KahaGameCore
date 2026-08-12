using KahaGameCore.Parameters;

namespace KahaGameCore.Persistence
{
    public sealed class GameSaveDocument
    {
        public int SchemaVersion;
        public string SceneKey;
        public ParameterSnapshotDocument Parameters;
        public SaveParticipantDocument[] Participants;
    }
}
