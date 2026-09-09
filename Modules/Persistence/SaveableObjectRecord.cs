using System;

namespace KahaGameCore.Persistence
{
    /// <summary>
    /// One runtime-spawned object as pure data: what it is, and where.
    /// This is the exact shape written to the save file.
    /// </summary>
    [Serializable]
    public sealed class SaveableObjectRecord
    {
        public string Id;
        public string ResourcePath;
        public string ScenePath;
        public float X;
        public float Y;
        public float Z;
    }
}
