using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Parameters.Editor
{
    [Serializable]
    public sealed class ParameterTableEditorPanel
    {
        [Serializable]
        private sealed class ParameterRowDraft
        {
            public string Key;
            public string DisplayName;
            public ParameterType Type;
            public int IntInitial;
            public int IntMin;
            public int IntMax = 100;
            public float FloatInitial;
            public float FloatMin;
            public float FloatMax = 100f;
            public bool BoolInitial;
            public string StringInitial = string.Empty;

            public ParameterDefinition ToDefinition()
            {
                switch (Type)
                {
                    case ParameterType.Int:
                        return ParameterDefinition.Int(Key, DisplayName, IntInitial, IntMin, IntMax);
                    case ParameterType.Float:
                        return ParameterDefinition.Float(Key, DisplayName, FloatInitial, FloatMin, FloatMax);
                    case ParameterType.Bool:
                        return ParameterDefinition.Bool(Key, DisplayName, BoolInitial);
                    case ParameterType.String:
                        return ParameterDefinition.String(Key, DisplayName, StringInitial ?? string.Empty);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public static ParameterRowDraft FromDefinition(ParameterDefinition definition)
            {
                ParameterRowDraft row = new ParameterRowDraft
                {
                    Key = definition.Key,
                    DisplayName = definition.DisplayName,
                    Type = definition.Type
                };

                switch (definition.Type)
                {
                    case ParameterType.Int:
                        row.IntInitial = definition.InitialValue.AsInt();
                        row.IntMin = definition.MinValue.Value.AsInt();
                        row.IntMax = definition.MaxValue.Value.AsInt();
                        break;
                    case ParameterType.Float:
                        row.FloatInitial = definition.InitialValue.AsFloat();
                        row.FloatMin = definition.MinValue.Value.AsFloat();
                        row.FloatMax = definition.MaxValue.Value.AsFloat();
                        break;
                    case ParameterType.Bool:
                        row.BoolInitial = definition.InitialValue.AsBool();
                        break;
                    case ParameterType.String:
                        row.StringInitial = definition.InitialValue.AsString();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return row;
            }
        }

        private const float INDEX_WIDTH = 28f;
        private const float KEY_WIDTH = 160f;
        private const float DISPLAY_NAME_WIDTH = 160f;
        private const float TYPE_WIDTH = 80f;
        private const float VALUE_WIDTH = 120f;
        private const float SUPPLEMENTAL_WIDTH = 100f;
        private const float DELETE_WIDTH = 28f;
        private static readonly ParameterType[] ParameterTypes =
            { ParameterType.Int, ParameterType.Float, ParameterType.Bool, ParameterType.String };
        private static readonly string[] ParameterTypeLabels =
            { "整數", "小數", "布林", "文字" };

        [SerializeField] private string tableGuid;
        [SerializeField] private string tableDisplayName;
        [SerializeField] private List<ParameterRowDraft> rows = new List<ParameterRowDraft>();
        [SerializeField] private string assetPath;
        [SerializeField] private bool isDirty;
        [SerializeField] private string statusMessage;
        [SerializeField] private MessageType statusType = MessageType.Info;
        [SerializeField] private string searchText;

        [NonSerialized] private ParameterTableJsonCodec codec;

        public string TableGuid => tableGuid;
        public string TableDisplayName => tableDisplayName;
        public string AssetPath => assetPath;
        public int ParameterCount => Rows.Count;
        public bool IsDirty => isDirty;
        public bool HasTable => !string.IsNullOrEmpty(tableGuid);

        private List<ParameterRowDraft> Rows
        {
            get
            {
                if (rows == null)
                {
                    rows = new List<ParameterRowDraft>();
                }

                return rows;
            }
        }

        private ParameterTableJsonCodec Codec
        {
            get
            {
                if (codec == null)
                {
                    codec = new ParameterTableJsonCodec();
                }

                return codec;
            }
        }

        public void InitializeIfNeeded()
        {
            if (!HasTable)
            {
                NewTable();
            }
        }

        public void Draw()
        {
            Draw(null, null, null);
        }

        public void Draw(
            Func<string, bool> includeParameter,
            Action<string> drawParameterSupplementalCell,
            Action<string> drawParameterDetails)
        {
            InitializeIfNeeded();

            EditorGUI.BeginChangeCheck();
            DrawTableIdentity();
            if (EditorGUI.EndChangeCheck())
            {
                MarkDirty("參數表已變更，請驗證後再儲存。");
            }

            EditorGUILayout.Space();
            DrawSearchToolbar(includeParameter);

            bool rowsChanged = DrawParameterGrid(
                includeParameter,
                drawParameterSupplementalCell,
                drawParameterDetails);

            EditorGUILayout.Space();
            if (GUILayout.Button("＋ 新增參數", GUILayout.Width(140f), GUILayout.Height(28f)))
            {
                AddInt(CreateUniqueKey(), string.Empty, 0, 0, 100);
            }

            if (rowsChanged)
            {
                MarkDirty("參數表已變更，請驗證後再儲存。");
            }

            EditorGUILayout.Space();
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }
        }

        public void NewTable()
        {
            tableGuid = Guid.NewGuid().ToString();
            tableDisplayName = "新參數表";
            Rows.Clear();
            assetPath = null;
            searchText = string.Empty;
            isDirty = false;
            ClearStatus();
        }

        public void Clear()
        {
            tableGuid = null;
            tableDisplayName = null;
            Rows.Clear();
            assetPath = null;
            searchText = string.Empty;
            isDirty = false;
            ClearStatus();
        }

        public void MarkClean()
        {
            isDirty = false;
        }

        public void SetTableDisplayName(string displayName)
        {
            if (tableDisplayName == displayName)
            {
                return;
            }

            tableDisplayName = displayName;
            MarkDirty("參數表已變更，請驗證後再儲存。");
        }

        public void AddInt(
            string key,
            string displayName,
            int initialValue,
            int minValue,
            int maxValue)
        {
            Rows.Add(new ParameterRowDraft
            {
                Key = key,
                DisplayName = displayName,
                Type = ParameterType.Int,
                IntInitial = initialValue,
                IntMin = minValue,
                IntMax = maxValue
            });
            MarkDirty("已新增參數，請驗證後再儲存。");
        }

        public void AddFloat(
            string key,
            string displayName,
            float initialValue,
            float minValue,
            float maxValue)
        {
            Rows.Add(new ParameterRowDraft
            {
                Key = key,
                DisplayName = displayName,
                Type = ParameterType.Float,
                FloatInitial = initialValue,
                FloatMin = minValue,
                FloatMax = maxValue
            });
            MarkDirty("已新增參數，請驗證後再儲存。");
        }

        public void AddBool(string key, string displayName, bool initialValue)
        {
            Rows.Add(new ParameterRowDraft
            {
                Key = key,
                DisplayName = displayName,
                Type = ParameterType.Bool,
                BoolInitial = initialValue
            });
            MarkDirty("已新增參數，請驗證後再儲存。");
        }

        public void AddString(string key, string displayName, string initialValue)
        {
            Rows.Add(new ParameterRowDraft
            {
                Key = key,
                DisplayName = displayName,
                Type = ParameterType.String,
                StringInitial = initialValue
            });
            MarkDirty("已新增參數，請驗證後再儲存。");
        }

        public void RemoveParameterAt(int index)
        {
            Rows.RemoveAt(index);
            MarkDirty("已移除參數，請驗證後再儲存。");
        }

        public ParameterTable ValidateTable()
        {
            ParameterDefinition[] definitions = Rows
                .Select(row => row.ToDefinition())
                .ToArray();
            return new ParameterTable(tableGuid, tableDisplayName, definitions);
        }

        public void LoadTable(string tableAssetPath)
        {
            string normalizedPath = NormalizeAssetPath(tableAssetPath);
            ParameterTable table = Codec.Read(File.ReadAllText(ToFullPath(normalizedPath)));

            tableGuid = table.TableGuid;
            tableDisplayName = table.DisplayName;
            rows = table.Definitions.Select(ParameterRowDraft.FromDefinition).ToList();
            assetPath = normalizedPath;
            searchText = string.Empty;
            isDirty = false;
            SetStatus(
                $"已開啟：{table.DisplayName}（{table.Definitions.Count} 個參數）。",
                MessageType.Info);
        }

        public void Reload()
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("此參數表尚未儲存。");
            }

            LoadTable(assetPath);
        }

        public void SaveTable(string tableAssetPath)
        {
            string normalizedPath = NormalizeAssetPath(tableAssetPath);
            ParameterTable table = ValidateTable();
            string canonicalJson = Codec.Write(table);

            File.WriteAllText(ToFullPath(normalizedPath), canonicalJson, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(normalizedPath, ImportAssetOptions.ForceSynchronousImport);
            assetPath = normalizedPath;
            isDirty = false;
            SetStatus(
                $"已儲存：{table.DisplayName}（{table.Definitions.Count} 個參數）。",
                MessageType.Info);
        }

        public string GetDefaultFileName()
        {
            string fileName = string.IsNullOrWhiteSpace(tableDisplayName)
                ? "NewParameterTable"
                : tableDisplayName;
            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidCharacter, '_');
            }

            return fileName + ".parameters";
        }

        public void SetStatus(string message, MessageType messageType)
        {
            statusMessage = message;
            statusType = messageType;
        }

        private void DrawTableIdentity()
        {
            EditorGUILayout.LabelField("參數表資料", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("資產路徑", AssetPath ?? "尚未儲存");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("參數表 GUID", tableGuid ?? string.Empty);
            }

            tableDisplayName = EditorGUILayout.TextField(
                "顯示名稱",
                tableDisplayName ?? string.Empty);
        }

        private bool DrawParameterGrid(
            Func<string, bool> includeParameter,
            Action<string> drawParameterSupplementalCell,
            Action<string> drawParameterDetails)
        {
            DrawGridHeader(drawParameterSupplementalCell != null);

            int deleteIndex = -1;
            int visibleCount = 0;
            bool changed = false;
            for (int index = 0; index < Rows.Count; index++)
            {
                ParameterRowDraft row = Rows[index];
                if (!MatchesSearch(row) ||
                    (includeParameter != null &&
                     !includeParameter(row.Key ?? string.Empty)))
                {
                    continue;
                }

                visibleCount++;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.LabelField(
                            (index + 1).ToString(),
                            GUILayout.Width(INDEX_WIDTH));
                        row.Key = EditorGUILayout.TextField(
                            row.Key ?? string.Empty,
                            GUILayout.Width(KEY_WIDTH));
                        row.DisplayName = EditorGUILayout.TextField(
                            row.DisplayName ?? string.Empty,
                            GUILayout.Width(DISPLAY_NAME_WIDTH));
                        int typeIndex = Math.Max(0, Array.IndexOf(ParameterTypes, row.Type));
                        typeIndex = EditorGUILayout.Popup(
                            typeIndex,
                            ParameterTypeLabels,
                            GUILayout.Width(TYPE_WIDTH));
                        row.Type = ParameterTypes[typeIndex];
                        DrawInitialValue(row);
                        DrawMinimumValue(row);
                        DrawMaximumValue(row);
                        changed |= EditorGUI.EndChangeCheck();

                        if (drawParameterSupplementalCell != null)
                        {
                            using (new EditorGUILayout.VerticalScope(
                                       GUILayout.Width(SUPPLEMENTAL_WIDTH)))
                            {
                                drawParameterSupplementalCell(row.Key ?? string.Empty);
                            }
                        }

                        if (GUILayout.Button("×", GUILayout.Width(DELETE_WIDTH)))
                        {
                            deleteIndex = index;
                        }
                    }

                    drawParameterDetails?.Invoke(row.Key ?? string.Empty);
                }
            }

            if (visibleCount == 0 && Rows.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "找不到符合目前搜尋條件的參數。",
                    MessageType.Info);
            }

            if (deleteIndex >= 0)
            {
                RemoveParameterAt(deleteIndex);
            }

            return changed;
        }

        private void DrawSearchToolbar(Func<string, bool> includeParameter)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField("搜尋", GUILayout.Width(34f));
                searchText = EditorGUILayout.TextField(
                    searchText ?? string.Empty,
                    EditorStyles.toolbarSearchField);
                int visibleCount = Rows.Count(row =>
                    MatchesSearch(row) &&
                    (includeParameter == null ||
                     includeParameter(row.Key ?? string.Empty)));
                EditorGUILayout.LabelField(
                    $"{visibleCount} / {Rows.Count}",
                    EditorStyles.miniLabel,
                    GUILayout.Width(58f));
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(searchText)))
                {
                    if (GUILayout.Button(
                            "×",
                            EditorStyles.toolbarButton,
                            GUILayout.Width(24f)))
                    {
                        searchText = string.Empty;
                        GUI.FocusControl(null);
                    }
                }
            }
        }

        private bool MatchesSearch(ParameterRowDraft row)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            string key = row.Key ?? string.Empty;
            string displayName = row.DisplayName ?? string.Empty;
            string[] terms = searchText.Split(
                new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);
            return terms.All(term =>
                key.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                displayName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void DrawGridHeader(bool hasSupplementalCell)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("#", EditorStyles.boldLabel, GUILayout.Width(INDEX_WIDTH));
                EditorGUILayout.LabelField("參數鍵", EditorStyles.boldLabel, GUILayout.Width(KEY_WIDTH));
                EditorGUILayout.LabelField("顯示名稱", EditorStyles.boldLabel, GUILayout.Width(DISPLAY_NAME_WIDTH));
                EditorGUILayout.LabelField("類型", EditorStyles.boldLabel, GUILayout.Width(TYPE_WIDTH));
                EditorGUILayout.LabelField("初始值", EditorStyles.boldLabel, GUILayout.Width(VALUE_WIDTH));
                EditorGUILayout.LabelField("最小值", EditorStyles.boldLabel, GUILayout.Width(VALUE_WIDTH));
                EditorGUILayout.LabelField("最大值", EditorStyles.boldLabel, GUILayout.Width(VALUE_WIDTH));
                if (hasSupplementalCell)
                {
                    EditorGUILayout.LabelField(
                        "Reference",
                        EditorStyles.boldLabel,
                        GUILayout.Width(SUPPLEMENTAL_WIDTH));
                }
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(DELETE_WIDTH));
            }
        }

        private static void DrawInitialValue(ParameterRowDraft row)
        {
            switch (row.Type)
            {
                case ParameterType.Int:
                    row.IntInitial = EditorGUILayout.IntField(row.IntInitial, GUILayout.Width(VALUE_WIDTH));
                    break;
                case ParameterType.Float:
                    row.FloatInitial = EditorGUILayout.FloatField(row.FloatInitial, GUILayout.Width(VALUE_WIDTH));
                    break;
                case ParameterType.Bool:
                    row.BoolInitial = EditorGUILayout.Toggle(row.BoolInitial, GUILayout.Width(VALUE_WIDTH));
                    break;
                case ParameterType.String:
                    row.StringInitial = EditorGUILayout.TextField(
                        row.StringInitial ?? string.Empty,
                        GUILayout.Width(VALUE_WIDTH));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void DrawMinimumValue(ParameterRowDraft row)
        {
            switch (row.Type)
            {
                case ParameterType.Int:
                    row.IntMin = EditorGUILayout.IntField(row.IntMin, GUILayout.Width(VALUE_WIDTH));
                    break;
                case ParameterType.Float:
                    row.FloatMin = EditorGUILayout.FloatField(row.FloatMin, GUILayout.Width(VALUE_WIDTH));
                    break;
                default:
                    EditorGUILayout.LabelField("—", GUILayout.Width(VALUE_WIDTH));
                    break;
            }
        }

        private static void DrawMaximumValue(ParameterRowDraft row)
        {
            switch (row.Type)
            {
                case ParameterType.Int:
                    row.IntMax = EditorGUILayout.IntField(row.IntMax, GUILayout.Width(VALUE_WIDTH));
                    break;
                case ParameterType.Float:
                    row.FloatMax = EditorGUILayout.FloatField(row.FloatMax, GUILayout.Width(VALUE_WIDTH));
                    break;
                default:
                    EditorGUILayout.LabelField("—", GUILayout.Width(VALUE_WIDTH));
                    break;
            }
        }

        private string CreateUniqueKey()
        {
            int suffix = Rows.Count + 1;
            string candidate;
            do
            {
                candidate = $"NewParameter{suffix}";
                suffix++;
            }
            while (Rows.Any(row => row.Key == candidate));

            return candidate;
        }

        private void MarkDirty(string message)
        {
            isDirty = true;
            SetStatus(message, MessageType.Info);
        }

        private void ClearStatus()
        {
            statusMessage = null;
            statusType = MessageType.Info;
        }

        private static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("必須提供參數表資產路徑。", nameof(path));
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
                    "參數表必須是 Assets/ 下的 .parameters.json 檔案。",
                    nameof(path));
            }

            string normalizedPath = "Assets/" +
                fullPath.Substring(assetsRoot.Length).Replace('\\', '/');
            if (!normalizedPath.EndsWith(".parameters.json", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "參數表必須是 Assets/ 下的 .parameters.json 檔案。",
                    nameof(path));
            }

            return normalizedPath;
        }

        private static string ToFullPath(string tableAssetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", tableAssetPath));
        }
    }
}
