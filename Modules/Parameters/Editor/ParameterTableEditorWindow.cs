using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Parameters.Editor
{
    public sealed class ParameterTableEditorWindow : EditorWindow
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
        private const float DELETE_WIDTH = 28f;

        private readonly ParameterTableJsonCodec codec = new ParameterTableJsonCodec();

        [SerializeField] private string tableGuid;
        [SerializeField] private string tableDisplayName;
        [SerializeField] private List<ParameterRowDraft> rows = new List<ParameterRowDraft>();
        [SerializeField] private string assetPath;
        [SerializeField] private string statusMessage;
        [SerializeField] private MessageType statusType = MessageType.Info;
        [SerializeField] private Vector2 scrollPosition;

        public string TableGuid => tableGuid;
        public string TableDisplayName => tableDisplayName;
        public string AssetPath => assetPath;
        public int ParameterCount => rows.Count;

        [MenuItem("KahaGameCore/Parameters/Parameter Table Editor")]
        public static void OpenWindow()
        {
            ParameterTableEditorWindow window = GetWindow<ParameterTableEditorWindow>();
            window.titleContent = new GUIContent("Parameter Table");
            window.minSize = new Vector2(920f, 500f);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Parameter Table");
            if (string.IsNullOrEmpty(tableGuid))
            {
                NewTable();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.Space();
            DrawTableIdentity();
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            DrawParameterGrid();
            if (EditorGUI.EndChangeCheck())
            {
                SetStatus("Table changed; validation required.", MessageType.Info);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("+ Add Parameter", GUILayout.Width(140f)))
            {
                AddInt(CreateUniqueKey(), string.Empty, 0, 0, 100);
                SetStatus("Parameter added; validation required.", MessageType.Info);
            }

            EditorGUILayout.Space();
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("New", EditorStyles.toolbarButton))
                {
                    NewTable();
                    SetStatus("New Parameter Table created.", MessageType.Info);
                }

                if (GUILayout.Button("Load", EditorStyles.toolbarButton))
                {
                    LoadFromDialog();
                }

                if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
                {
                    RunCommand(
                        () => ValidateTable(),
                        table => $"Valid: {table.DisplayName} ({table.Definitions.Count} parameters).");
                }

                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                {
                    SaveToCurrentOrDialog();
                }

                if (GUILayout.Button("Save As", EditorStyles.toolbarButton))
                {
                    SaveFromDialog();
                }
            }
        }

        private void DrawTableIdentity()
        {
            EditorGUILayout.LabelField("Parameter Table", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Asset", AssetPath ?? "Unsaved");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Table GUID", tableGuid ?? string.Empty);
            }

            tableDisplayName = EditorGUILayout.TextField(
                "Display Name",
                tableDisplayName ?? string.Empty);
        }

        private void DrawParameterGrid()
        {
            DrawGridHeader();

            int deleteIndex = -1;
            for (int index = 0; index < rows.Count; index++)
            {
                ParameterRowDraft row = rows[index];
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField((index + 1).ToString(), GUILayout.Width(INDEX_WIDTH));
                    row.Key = EditorGUILayout.TextField(row.Key ?? string.Empty, GUILayout.Width(KEY_WIDTH));
                    row.DisplayName = EditorGUILayout.TextField(
                        row.DisplayName ?? string.Empty,
                        GUILayout.Width(DISPLAY_NAME_WIDTH));
                    row.Type = (ParameterType)EditorGUILayout.EnumPopup(row.Type, GUILayout.Width(TYPE_WIDTH));
                    DrawInitialValue(row);
                    DrawMinimumValue(row);
                    DrawMaximumValue(row);

                    if (GUILayout.Button("×", GUILayout.Width(DELETE_WIDTH)))
                    {
                        deleteIndex = index;
                    }
                }
            }

            if (deleteIndex >= 0)
            {
                rows.RemoveAt(deleteIndex);
                SetStatus("Parameter removed; validation required.", MessageType.Info);
            }
        }

        private static void DrawGridHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("#", EditorStyles.boldLabel, GUILayout.Width(INDEX_WIDTH));
                EditorGUILayout.LabelField("Key", EditorStyles.boldLabel, GUILayout.Width(KEY_WIDTH));
                EditorGUILayout.LabelField("Display Name", EditorStyles.boldLabel, GUILayout.Width(DISPLAY_NAME_WIDTH));
                EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(TYPE_WIDTH));
                EditorGUILayout.LabelField("Initial", EditorStyles.boldLabel, GUILayout.Width(VALUE_WIDTH));
                EditorGUILayout.LabelField("Minimum", EditorStyles.boldLabel, GUILayout.Width(VALUE_WIDTH));
                EditorGUILayout.LabelField("Maximum", EditorStyles.boldLabel, GUILayout.Width(VALUE_WIDTH));
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

        public void NewTable()
        {
            tableGuid = Guid.NewGuid().ToString();
            tableDisplayName = "New Parameter Table";
            rows.Clear();
            assetPath = null;
        }

        public void SetTableDisplayName(string displayName)
        {
            tableDisplayName = displayName;
        }

        public void AddInt(
            string key,
            string displayName,
            int initialValue,
            int minValue,
            int maxValue)
        {
            rows.Add(new ParameterRowDraft
            {
                Key = key,
                DisplayName = displayName,
                Type = ParameterType.Int,
                IntInitial = initialValue,
                IntMin = minValue,
                IntMax = maxValue
            });
        }

        public void AddFloat(
            string key,
            string displayName,
            float initialValue,
            float minValue,
            float maxValue)
        {
            rows.Add(new ParameterRowDraft
            {
                Key = key,
                DisplayName = displayName,
                Type = ParameterType.Float,
                FloatInitial = initialValue,
                FloatMin = minValue,
                FloatMax = maxValue
            });
        }

        public void AddBool(string key, string displayName, bool initialValue)
        {
            rows.Add(new ParameterRowDraft
            {
                Key = key,
                DisplayName = displayName,
                Type = ParameterType.Bool,
                BoolInitial = initialValue
            });
        }

        public void AddString(string key, string displayName, string initialValue)
        {
            rows.Add(new ParameterRowDraft
            {
                Key = key,
                DisplayName = displayName,
                Type = ParameterType.String,
                StringInitial = initialValue
            });
        }

        public void RemoveParameterAt(int index)
        {
            rows.RemoveAt(index);
        }

        public ParameterTable ValidateTable()
        {
            ParameterDefinition[] definitions = rows
                .Select(row => row.ToDefinition())
                .ToArray();
            return new ParameterTable(tableGuid, tableDisplayName, definitions);
        }

        public void LoadTable(string tableAssetPath)
        {
            string normalizedPath = NormalizeAssetPath(tableAssetPath);
            ParameterTable table = codec.Read(File.ReadAllText(ToFullPath(normalizedPath)));

            tableGuid = table.TableGuid;
            tableDisplayName = table.DisplayName;
            rows = table.Definitions.Select(ParameterRowDraft.FromDefinition).ToList();
            assetPath = normalizedPath;
        }

        public void SaveTable(string tableAssetPath)
        {
            string normalizedPath = NormalizeAssetPath(tableAssetPath);
            string canonicalJson = codec.Write(ValidateTable());

            File.WriteAllText(ToFullPath(normalizedPath), canonicalJson, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(normalizedPath, ImportAssetOptions.ForceSynchronousImport);
            assetPath = normalizedPath;
        }

        private string CreateUniqueKey()
        {
            int suffix = rows.Count + 1;
            string candidate;
            do
            {
                candidate = $"NewParameter{suffix}";
                suffix++;
            }
            while (rows.Any(row => row.Key == candidate));

            return candidate;
        }

        private void LoadFromDialog()
        {
            string selectedPath = EditorUtility.OpenFilePanel(
                "Load Parameter Table",
                Application.dataPath,
                "json");
            if (string.IsNullOrEmpty(selectedPath))
            {
                return;
            }

            RunCommand(
                () =>
                {
                    LoadTable(selectedPath);
                    return ValidateTable();
                },
                table => $"Loaded: {table.DisplayName} ({table.Definitions.Count} parameters).");
        }

        private void SaveToCurrentOrDialog()
        {
            if (string.IsNullOrEmpty(AssetPath))
            {
                SaveFromDialog();
                return;
            }

            RunCommand(
                () =>
                {
                    SaveTable(AssetPath);
                    return ValidateTable();
                },
                table => $"Saved: {table.DisplayName} ({table.Definitions.Count} parameters).");
        }

        private void SaveFromDialog()
        {
            string selectedPath = EditorUtility.SaveFilePanelInProject(
                "Save Parameter Table",
                GetDefaultFileName(),
                "json",
                "Save the canonical Parameter Table JSON under Assets/.");
            if (string.IsNullOrEmpty(selectedPath))
            {
                return;
            }

            RunCommand(
                () =>
                {
                    SaveTable(selectedPath);
                    return ValidateTable();
                },
                table => $"Saved: {table.DisplayName} ({table.Definitions.Count} parameters).");
        }

        private string GetDefaultFileName()
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

        private void RunCommand(
            Func<ParameterTable> command,
            Func<ParameterTable, string> successMessage)
        {
            try
            {
                ParameterTable table = command();
                SetStatus(successMessage(table), MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
            }
        }

        private void SetStatus(string message, MessageType messageType)
        {
            statusMessage = message;
            statusType = messageType;
            Repaint();
        }

        private static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Parameter Table asset path is required.", nameof(path));
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
                    "Parameter Table must be a .parameters.json file under Assets/.",
                    nameof(path));
            }

            string normalizedPath = "Assets/" +
                fullPath.Substring(assetsRoot.Length).Replace('\\', '/');
            if (!normalizedPath.EndsWith(".parameters.json", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Parameter Table must be a .parameters.json file under Assets/.",
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
