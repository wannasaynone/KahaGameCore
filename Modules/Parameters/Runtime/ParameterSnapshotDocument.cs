namespace KahaGameCore.Parameters
{
    public sealed class ParameterSnapshotDocument
    {
        public int SchemaVersion;
        public ParameterSnapshotValueDocument[] Values;
    }

    public sealed class ParameterSnapshotValueDocument
    {
        public string Key;
        public string Type;
        public string Value;
    }
}
