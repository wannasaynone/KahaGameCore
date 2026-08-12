using System;
using System.IO;
using System.Text;

namespace KahaGameCore.Persistence
{
    public sealed class GameSaveSlotStore
    {
        private readonly string rootDirectory;

        public GameSaveSlotStore(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException(
                    "Save root directory is required.",
                    nameof(rootDirectory));
            }

            this.rootDirectory = rootDirectory;
        }

        public void Save(int slot, string json)
        {
            Directory.CreateDirectory(rootDirectory);
            File.WriteAllText(
                GetFilePath(slot),
                json,
                new UTF8Encoding(false));
        }

        public string Load(int slot)
        {
            return File.ReadAllText(GetFilePath(slot));
        }

        public bool Exists(int slot)
        {
            return File.Exists(GetFilePath(slot));
        }

        public bool Delete(int slot)
        {
            string filePath = GetFilePath(slot);
            if (!File.Exists(filePath))
            {
                return false;
            }

            File.Delete(filePath);
            return true;
        }

        private string GetFilePath(int slot)
        {
            if (slot < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slot),
                    slot,
                    "Save slot cannot be negative.");
            }

            return Path.Combine(rootDirectory, $"slot-{slot}.json");
        }
    }
}
