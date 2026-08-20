using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.Parameters;
using KahaGameCore.Parameters.Editor;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    [Serializable]
    internal sealed class ParameterTableWorkspace
    {
        [Serializable]
        internal sealed class Session
        {
            [SerializeField] private string assetPath;
            [SerializeField] private ParameterTableEditorPanel editor =
                new ParameterTableEditorPanel();
            [SerializeField] private bool expanded;
            [SerializeField] private List<string> expandedReferenceKeys =
                new List<string>();

            public string AssetPath => assetPath;
            public ParameterTableEditorPanel Editor => editor ??=
                new ParameterTableEditorPanel();
            public bool Expanded
            {
                get => expanded;
                set => expanded = value;
            }

            private List<string> ExpandedReferenceKeys =>
                expandedReferenceKeys ??= new List<string>();

            public bool IsReferenceExpanded(string parameterKey)
            {
                return ExpandedReferenceKeys.Contains(parameterKey ?? string.Empty);
            }

            public void SetReferenceExpanded(string parameterKey, bool value)
            {
                string key = parameterKey ?? string.Empty;
                if (value)
                {
                    if (!ExpandedReferenceKeys.Contains(key))
                    {
                        ExpandedReferenceKeys.Add(key);
                    }
                }
                else
                {
                    ExpandedReferenceKeys.Remove(key);
                }
            }

            public void PruneReferenceExpansion(IEnumerable<string> parameterKeys)
            {
                HashSet<string> validKeys = new HashSet<string>(
                    parameterKeys ?? Array.Empty<string>(),
                    StringComparer.Ordinal);
                ExpandedReferenceKeys.RemoveAll(key => !validKeys.Contains(key));
            }

            public void Load(string path)
            {
                assetPath = NormalizePath(path);
                Editor.LoadTable(assetPath);
            }
        }

        [SerializeField] private List<Session> sessions = new List<Session>();
        [SerializeField] private string overviewSearchText;
        [NonSerialized] private Dictionary<string, string> loadErrors;

        public IReadOnlyList<Session> Sessions => SessionsList;
        public string OverviewSearchText
        {
            get => overviewSearchText ?? string.Empty;
            set => overviewSearchText = value ?? string.Empty;
        }

        public bool HasUnsavedChanges => SessionsList.Any(item => item.Editor.IsDirty);
        public int DirtyCount => SessionsList.Count(item => item.Editor.IsDirty);

        private List<Session> SessionsList => sessions ??= new List<Session>();
        private Dictionary<string, string> LoadErrors => loadErrors ??=
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void Bind(IEnumerable<TextAsset> tableAssets)
        {
            string[] paths = (tableAssets ?? Array.Empty<TextAsset>())
                .Where(asset => asset != null)
                .Select(AssetDatabase.GetAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(NormalizePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            SessionsList.RemoveAll(session =>
                !paths.Any(path => SamePath(path, session.AssetPath)));
            foreach (string stalePath in LoadErrors.Keys
                         .Where(path => !paths.Any(active => SamePath(path, active)))
                         .ToArray())
            {
                LoadErrors.Remove(stalePath);
            }

            foreach (string path in paths)
            {
                if (TryGetSession(path, out Session _))
                {
                    continue;
                }

                try
                {
                    Session session = new Session();
                    session.Load(path);
                    SessionsList.Add(session);
                    LoadErrors.Remove(path);
                }
                catch (Exception exception)
                {
                    LoadErrors[path] = $"無法載入參數表「{path}」：{exception.Message}";
                }
            }

        }

        public bool TryGetSession(string assetPath, out Session session)
        {
            session = SessionsList.FirstOrDefault(
                item => SamePath(item.AssetPath, assetPath));
            return session != null;
        }

        public bool Expand(string assetPath)
        {
            if (!TryGetSession(assetPath, out Session session))
            {
                return false;
            }

            session.Expanded = true;
            return true;
        }

        public IReadOnlyList<ParameterAuthoringEntry> BuildEntries(
            out IReadOnlyList<string> errors)
        {
            Dictionary<string, ParameterAuthoringEntry> entries =
                new Dictionary<string, ParameterAuthoringEntry>(StringComparer.Ordinal);
            List<string> collectedErrors = LoadErrors.Values.ToList();
            foreach (Session session in SessionsList)
            {
                ParameterTable table;
                try
                {
                    table = session.Editor.ValidateTable();
                }
                catch (Exception exception)
                {
                    collectedErrors.Add(
                        $"參數表「{session.AssetPath}」無效：{exception.Message}");
                    continue;
                }

                foreach (ParameterDefinition definition in table.Definitions)
                {
                    ParameterAuthoringEntry entry = new ParameterAuthoringEntry(
                        table.TableGuid,
                        table.DisplayName,
                        session.AssetPath,
                        definition);
                    if (entries.TryGetValue(
                            definition.Key,
                            out ParameterAuthoringEntry existing))
                    {
                        collectedErrors.Add(
                            $"參數鍵「{definition.Key}」同時存在於「" +
                            $"{existing.AssetPath}」與「{session.AssetPath}」。" +
                            "參數鍵必須跨表唯一。");
                        continue;
                    }

                    entries.Add(definition.Key, entry);
                }
            }

            errors = collectedErrors;
            return entries.Values
                .OrderBy(entry => entry.TableDisplayName, StringComparer.Ordinal)
                .ThenBy(entry => entry.Definition.Key, StringComparer.Ordinal)
                .ToArray();
        }

        public void AddParameter(string assetPath, ParameterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            IReadOnlyList<ParameterAuthoringEntry> entries = BuildEntries(out _);
            ParameterAuthoringEntry duplicate = entries.FirstOrDefault(
                entry => string.Equals(
                    entry.Definition.Key,
                    definition.Key,
                    StringComparison.Ordinal));
            if (duplicate != null)
            {
                throw new InvalidOperationException(
                    $"參數鍵「{definition.Key}」已存在於「{duplicate.TableDisplayName}」。");
            }

            if (!TryGetSession(assetPath, out Session session))
            {
                throw new InvalidOperationException("請選擇要新增參數的參數表。");
            }

            switch (definition.Type)
            {
                case ParameterType.Int:
                    session.Editor.AddInt(
                        definition.Key,
                        definition.DisplayName,
                        definition.InitialValue.AsInt(),
                        definition.MinValue.Value.AsInt(),
                        definition.MaxValue.Value.AsInt());
                    break;
                case ParameterType.Float:
                    session.Editor.AddFloat(
                        definition.Key,
                        definition.DisplayName,
                        definition.InitialValue.AsFloat(),
                        definition.MinValue.Value.AsFloat(),
                        definition.MaxValue.Value.AsFloat());
                    break;
                case ParameterType.Bool:
                    session.Editor.AddBool(
                        definition.Key,
                        definition.DisplayName,
                        definition.InitialValue.AsBool());
                    break;
                case ParameterType.String:
                    session.Editor.AddString(
                        definition.Key,
                        definition.DisplayName,
                        definition.InitialValue.AsString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            session.Expanded = true;
        }

        public void SaveAll()
        {
            IReadOnlyList<ParameterAuthoringEntry> _ = BuildEntries(
                out IReadOnlyList<string> errors);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }

            foreach (Session session in SessionsList.Where(item => item.Editor.IsDirty))
            {
                session.Editor.SaveTable(session.AssetPath);
            }
        }

        public void ReloadAll()
        {
            foreach (Session session in SessionsList)
            {
                session.Editor.Reload();
            }
        }

        public bool IsDirty(string assetPath)
        {
            return TryGetSession(assetPath, out Session session) && session.Editor.IsDirty;
        }

        private static bool SamePath(string left, string right)
        {
            return string.Equals(
                NormalizePath(left),
                NormalizePath(right),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            return path?.Replace('\\', '/');
        }
    }
}
