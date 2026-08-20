using System;
using System.IO;
using System.Text;
using KahaGameCore.Effects;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    [Serializable]
    public sealed class GameEventEditorSession
    {
        [SerializeField] private string documentGuid;
        [SerializeField] private string displayName;
        [SerializeField] private string triggerTiming;
        [SerializeField] private string condition;
        [SerializeField] private string commands;
        [SerializeField] private string assetPath;
        [SerializeField] private bool isDirty;

        [NonSerialized] private GameEventDocumentJsonCodec codec;
        [NonSerialized] private EffectRuntime effectRuntime;

        public int SchemaVersion => GameEventDocumentJsonCodec.CurrentSchemaVersion;
        public string AssetPath => assetPath;
        public bool IsDirty => isDirty;
        public bool HasOpenFile => !string.IsNullOrEmpty(assetPath);

        public string DocumentGuid => documentGuid;

        public string DisplayName
        {
            get => displayName;
            set => SetValue(ref displayName, value);
        }

        public string TriggerTiming
        {
            get => triggerTiming;
            set => SetValue(ref triggerTiming, value);
        }

        public string Condition
        {
            get => condition;
            set => SetValue(ref condition, value);
        }

        public string Commands
        {
            get => commands;
            set => SetValue(ref commands, value);
        }

        public void NewDocument()
        {
            documentGuid = Guid.NewGuid().ToString("D");
            displayName = "新遊戲事件";
            triggerTiming = string.Empty;
            condition = string.Empty;
            commands = string.Empty;
            assetPath = null;
            isDirty = true;
        }

        internal void ClearDocument()
        {
            documentGuid = string.Empty;
            displayName = string.Empty;
            triggerTiming = string.Empty;
            condition = string.Empty;
            commands = string.Empty;
            assetPath = null;
            isDirty = false;
        }

        public void RegenerateDocumentGuid()
        {
            documentGuid = Guid.NewGuid().ToString("D");
            isDirty = true;
        }

        public void SetDocumentGuid(string value)
        {
            SetValue(ref documentGuid, value);
        }

        public GameEventDocument ValidateDocument(string targetAssetPath = null)
        {
            if (!Guid.TryParse(documentGuid, out Guid parsedGuid) || parsedGuid == Guid.Empty)
            {
                throw new InvalidOperationException("Document GUID must be a valid, non-empty GUID.");
            }

            GameEventDocument document = new GameEventDocument(
                SchemaVersion,
                parsedGuid,
                displayName,
                triggerTiming,
                condition,
                commands);

            GameEventDocument roundTripped = Codec.Read(Codec.Write(document));
            EffectParseResult commandParse = EffectRuntime.Parse(roundTripped.Commands);
            if (!commandParse.IsSuccess)
            {
                throw new InvalidOperationException(
                    "Commands are invalid: " + commandParse.FormatDiagnostics());
            }

            ValidateUniqueDocumentGuid(
                roundTripped.DocumentGuid,
                targetAssetPath ?? assetPath);
            return roundTripped;
        }

        public void LoadDocument(string documentAssetPath)
        {
            string normalizedPath = NormalizeAssetPath(documentAssetPath);
            GameEventDocument document = Codec.Read(
                File.ReadAllText(ToFullPath(normalizedPath)));

            documentGuid = document.DocumentGuid.ToString("D");
            displayName = document.DisplayName;
            triggerTiming = document.TriggerTiming;
            condition = document.Condition;
            commands = document.Commands;
            assetPath = normalizedPath;
            isDirty = false;
        }

        public void SaveDocument(string documentAssetPath)
        {
            string normalizedPath = NormalizeAssetPath(documentAssetPath);
            GameEventDocument document = ValidateDocument(normalizedPath);
            string canonicalJson = Codec.Write(document);

            File.WriteAllText(
                ToFullPath(normalizedPath),
                canonicalJson,
                new UTF8Encoding(false));
            AssetDatabase.ImportAsset(
                normalizedPath,
                ImportAssetOptions.ForceSynchronousImport);
            assetPath = normalizedPath;
            isDirty = false;
        }

        public void MarkClean()
        {
            isDirty = false;
        }

        internal bool ResetIfAssetMissing()
        {
            if (string.IsNullOrEmpty(assetPath) ||
                AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath) != null)
            {
                return false;
            }

            ClearDocument();
            return true;
        }

        private GameEventDocumentJsonCodec Codec
        {
            get
            {
                if (codec == null)
                {
                    codec = new GameEventDocumentJsonCodec();
                }

                return codec;
            }
        }

        private EffectRuntime EffectRuntime
        {
            get
            {
                if (effectRuntime == null)
                {
                    effectRuntime = new EffectRuntime(new EffectCommandRegistry());
                }

                return effectRuntime;
            }
        }

        private void ValidateUniqueDocumentGuid(Guid candidate, string ignoredAssetPath)
        {
            string normalizedIgnoredPath = string.IsNullOrWhiteSpace(ignoredAssetPath)
                ? null
                : NormalizeAssetPath(ignoredAssetPath);
            string[] assetGuids = AssetDatabase.FindAssets("t:TextAsset");
            for (int index = 0; index < assetGuids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[index]);
                if (!path.EndsWith(".gameevent.json", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(path, normalizedIgnoredPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset == null)
                {
                    continue;
                }

                GameEventDocument existing;
                try
                {
                    existing = Codec.Read(asset.text);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"Existing Game Event '{path}' is invalid: {exception.Message}",
                        exception);
                }

                if (existing.DocumentGuid == candidate)
                {
                    throw new InvalidOperationException(
                        $"Document GUID '{candidate:D}' is already used by '{path}'.");
                }
            }
        }

        private void SetValue(ref string field, string value)
        {
            if (string.Equals(field, value, StringComparison.Ordinal))
            {
                return;
            }

            field = value;
            isDirty = true;
        }

        private static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Game Event asset path is required.", nameof(path));
            }

            string fullPath = Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            string assetsRoot = Path.GetFullPath(Application.dataPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Game Event must be a .gameevent.json file under Assets/.",
                    nameof(path));
            }

            string normalizedPath = "Assets/" +
                fullPath.Substring(assetsRoot.Length).Replace('\\', '/');
            if (!normalizedPath.EndsWith(
                    ".gameevent.json",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Game Event must be a .gameevent.json file under Assets/.",
                    nameof(path));
            }

            return normalizedPath;
        }

        private static string ToFullPath(string documentAssetPath)
        {
            return Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", documentAssetPath));
        }
    }
}
