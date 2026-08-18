using System;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Parameters.Editor
{
    public sealed class ParameterTableEditorWindow : EditorWindow
    {
        [SerializeField] private ParameterTableEditorPanel editor =
            new ParameterTableEditorPanel();
        [SerializeField] private Vector2 scrollPosition;

        public string TableGuid => Panel.TableGuid;
        public string TableDisplayName => Panel.TableDisplayName;
        public string AssetPath => Panel.AssetPath;
        public int ParameterCount => Panel.ParameterCount;
        public bool IsDirty => Panel.IsDirty;

        private ParameterTableEditorPanel Panel
        {
            get
            {
                if (editor == null)
                {
                    editor = new ParameterTableEditorPanel();
                }

                return editor;
            }
        }

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
            minSize = new Vector2(920f, 500f);
            saveChangesMessage = "Save changes to this Parameter Table?";
            Panel.InitializeIfNeeded();
            SyncUnsavedState();
        }

        private void OnGUI()
        {
            DrawToolbar();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.Space();
            Panel.Draw();
            EditorGUILayout.EndScrollView();
            SyncUnsavedState();
        }

        public override void SaveChanges()
        {
            if (SaveToCurrentOrDialog())
            {
                base.SaveChanges();
            }
        }

        public override void DiscardChanges()
        {
            Panel.MarkClean();
            SyncUnsavedState();
            base.DiscardChanges();
        }

        public void NewTable()
        {
            Panel.NewTable();
        }

        public void SetTableDisplayName(string displayName)
        {
            Panel.SetTableDisplayName(displayName);
        }

        public void AddInt(
            string key,
            string displayName,
            int initialValue,
            int minValue,
            int maxValue)
        {
            Panel.AddInt(key, displayName, initialValue, minValue, maxValue);
        }

        public void AddFloat(
            string key,
            string displayName,
            float initialValue,
            float minValue,
            float maxValue)
        {
            Panel.AddFloat(key, displayName, initialValue, minValue, maxValue);
        }

        public void AddBool(string key, string displayName, bool initialValue)
        {
            Panel.AddBool(key, displayName, initialValue);
        }

        public void AddString(string key, string displayName, string initialValue)
        {
            Panel.AddString(key, displayName, initialValue);
        }

        public void RemoveParameterAt(int index)
        {
            Panel.RemoveParameterAt(index);
        }

        public ParameterTable ValidateTable()
        {
            return Panel.ValidateTable();
        }

        public void LoadTable(string tableAssetPath)
        {
            Panel.LoadTable(tableAssetPath);
        }

        public void SaveTable(string tableAssetPath)
        {
            Panel.SaveTable(tableAssetPath);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("New", EditorStyles.toolbarButton) &&
                    ConfirmDiscardChanges())
                {
                    Panel.NewTable();
                    Panel.SetStatus("New Parameter Table created.", MessageType.Info);
                }

                if (GUILayout.Button("Load", EditorStyles.toolbarButton) &&
                    ConfirmDiscardChanges())
                {
                    LoadFromDialog();
                }

                if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
                {
                    RunCommand(
                        Panel.ValidateTable,
                        table => $"Valid: {table.DisplayName} " +
                            $"({table.Definitions.Count} parameters).");
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
                    Panel.LoadTable(selectedPath);
                    return Panel.ValidateTable();
                },
                table => $"Loaded: {table.DisplayName} " +
                    $"({table.Definitions.Count} parameters).");
        }

        private bool SaveToCurrentOrDialog()
        {
            if (string.IsNullOrEmpty(Panel.AssetPath))
            {
                return SaveFromDialog();
            }

            return RunCommand(
                () =>
                {
                    Panel.SaveTable(Panel.AssetPath);
                    return Panel.ValidateTable();
                },
                table => $"Saved: {table.DisplayName} " +
                    $"({table.Definitions.Count} parameters).");
        }

        private bool SaveFromDialog()
        {
            string selectedPath = EditorUtility.SaveFilePanelInProject(
                "Save Parameter Table",
                Panel.GetDefaultFileName(),
                "json",
                "Save the canonical Parameter Table JSON under Assets/.");
            if (string.IsNullOrEmpty(selectedPath))
            {
                return false;
            }

            return RunCommand(
                () =>
                {
                    Panel.SaveTable(selectedPath);
                    return Panel.ValidateTable();
                },
                table => $"Saved: {table.DisplayName} " +
                    $"({table.Definitions.Count} parameters).");
        }

        private bool ConfirmDiscardChanges()
        {
            return !Panel.IsDirty || EditorUtility.DisplayDialog(
                "Unsaved Parameter Table",
                "Discard the current unsaved changes?",
                "Discard",
                "Cancel");
        }

        private bool RunCommand(
            Func<ParameterTable> command,
            Func<ParameterTable, string> successMessage)
        {
            try
            {
                ParameterTable table = command();
                Panel.SetStatus(successMessage(table), MessageType.Info);
                SyncUnsavedState();
                Repaint();
                return true;
            }
            catch (Exception exception)
            {
                Panel.SetStatus(exception.Message, MessageType.Error);
                Repaint();
                return false;
            }
        }

        private void SyncUnsavedState()
        {
            hasUnsavedChanges = Panel.IsDirty;
        }
    }
}
