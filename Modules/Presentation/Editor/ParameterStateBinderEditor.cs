using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.GameEvents;
using KahaGameCore.GameEvents.Editor;
using KahaGameCore.Parameters;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Presentation.Editor
{
    [CustomEditor(typeof(ParameterStateBinder))]
    internal sealed class ParameterStateBinderEditor : UnityEditor.Editor
    {
        internal enum SetupState
        {
            MissingEventCatalog,
            MissingConditionParameters,
            Ready
        }

        private SerializedProperty bindings;
        private SerializedProperty behaviourTargets;
        private SerializedProperty behaviourCondition;
        private GameEventCatalogAsset eventCatalog;
        private GameEventProjectAuthoringCatalog catalog;
        private ParameterAuthoringEntry[] conditionParameters =
            Array.Empty<ParameterAuthoringEntry>();
        private readonly ParameterTableWorkspace parameterWorkspace =
            new ParameterTableWorkspace();
        private string parameterTablesRevision = string.Empty;
        private bool catalogLoaded;

        private void OnEnable()
        {
            bindings = serializedObject.FindProperty("bindings");
            behaviourTargets = serializedObject.FindProperty("behaviourTargets");
            behaviourCondition = serializedObject.FindProperty("behaviourCondition");
            EditorApplication.projectChanged += OnProjectChanged;
            RefreshCatalog();
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EnsureCatalogIsCurrent();
            DrawScriptField();
            DrawDataSource();
            DrawCatalogMessages();

            SetupState setupState = GetSetupState(
                eventCatalog != null,
                conditionParameters.Length);
            if (setupState != SetupState.Ready)
            {
                DrawSetupPrompt(setupState);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            DrawBehaviourBinding();
            DrawBindings();
            serializedObject.ApplyModifiedProperties();
        }

        internal static SetupState GetSetupState(
            bool hasEventCatalog,
            int conditionParameterCount)
        {
            if (!hasEventCatalog)
            {
                return SetupState.MissingEventCatalog;
            }

            return conditionParameterCount > 0
                ? SetupState.Ready
                : SetupState.MissingConditionParameters;
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("m_Script"));
            }
        }

        private void DrawDataSource()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("條件資料源", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(
                        "Game Event 目錄",
                        eventCatalog,
                        typeof(GameEventCatalogAsset),
                        false);
                }

                if (eventCatalog != null)
                {
                    EditorGUILayout.LabelField(
                        $"可用條件參數：{conditionParameters.Length}",
                        EditorStyles.miniLabel);
                }
            }
        }

        private void DrawCatalogMessages()
        {
            if (catalog == null)
            {
                return;
            }

            if (catalog.Errors.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    string.Join("\n", catalog.Errors),
                    MessageType.Error);
            }

            if (catalog.Warnings.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    string.Join("\n", catalog.Warnings),
                    MessageType.Warning);
            }
        }

        private void DrawSetupPrompt(SetupState setupState)
        {
            string message = setupState == SetupState.MissingEventCatalog
                ? "Game Event Editor 尚未初始化事件目錄。請先建立或指定事件目錄。"
                : "目前的 Game Event 目錄沒有可用的 Bool、Int 或 Float 參數。" +
                  "請先設定參數表與條件參數。";
            string buttonLabel = setupState == SetupState.MissingEventCatalog
                ? "開啟 Game Event Editor 初始化"
                : "開啟 Game Event Editor 設定參數";

            EditorGUILayout.HelpBox(message, MessageType.Warning);
            if (GUILayout.Button(buttonLabel, GUILayout.Height(32f)))
            {
                GameEventDocumentEditorWindow.OpenWindow();
            }
        }

        private void DrawBindings()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                $"子物件狀態綁定（{bindings.arraySize}）",
                EditorStyles.boldLabel);

            int removeIndex = -1;
            for (int index = 0; index < bindings.arraySize; index++)
            {
                SerializedProperty binding = bindings.GetArrayElementAtIndex(index);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(
                            $"綁定 #{index + 1}",
                            EditorStyles.boldLabel);
                        if (GUILayout.Button("刪除", GUILayout.Width(52f)))
                        {
                            removeIndex = index;
                        }
                    }

                    SerializedProperty targetProperty =
                        binding.FindPropertyRelative("target");
                    EditorGUILayout.PropertyField(targetProperty, new GUIContent("目標子物件"));
                    DrawTargetValidation(targetProperty);
                    DrawCondition(binding.FindPropertyRelative("condition"));
                }
            }

            if (removeIndex >= 0)
            {
                bindings.DeleteArrayElementAtIndex(removeIndex);
            }

            if (GUILayout.Button("＋ 新增狀態綁定", GUILayout.Height(30f)))
            {
                AddBinding();
            }
        }

        private void DrawBehaviourBinding()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                $"行為啟用綁定（{behaviourTargets.arraySize}）",
                EditorStyles.boldLabel);

            if (behaviourTargets.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "尚未綁定 Behaviour。Actor Inspector 的 Add Param Binder " +
                    "會自動抓取所有 Child Actions。",
                    MessageType.Info);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(
                    behaviourTargets,
                    new GUIContent(
                        "控制元件",
                        "條件成立時啟用；不成立時停用。"),
                    true);
                DrawBehaviourTargetValidation();
                DrawCondition(behaviourCondition);
            }
        }

        private void DrawBehaviourTargetValidation()
        {
            ParameterStateBinder binder = (ParameterStateBinder)target;
            HashSet<UnityEngine.Object> targets =
                new HashSet<UnityEngine.Object>();
            for (int index = 0; index < behaviourTargets.arraySize; index++)
            {
                Behaviour targetBehaviour = behaviourTargets
                    .GetArrayElementAtIndex(index)
                    .objectReferenceValue as Behaviour;
                if (targetBehaviour == null)
                {
                    EditorGUILayout.HelpBox(
                        $"控制元件 #{index + 1} 尚未指定。",
                        MessageType.Error);
                    continue;
                }

                if (targetBehaviour == binder)
                {
                    EditorGUILayout.HelpBox(
                        "Parameter State Binder 不能控制自己。",
                        MessageType.Error);
                }
                else if (targetBehaviour.transform != binder.transform &&
                         !targetBehaviour.transform.IsChildOf(binder.transform))
                {
                    EditorGUILayout.HelpBox(
                        $"控制元件「{targetBehaviour.name}」必須位於 Binder " +
                        "物件本身或其子物件。",
                        MessageType.Error);
                }

                if (!targets.Add(targetBehaviour))
                {
                    EditorGUILayout.HelpBox(
                        $"控制元件「{targetBehaviour.name}」重複綁定。",
                        MessageType.Error);
                }
            }
        }

        private void DrawTargetValidation(SerializedProperty targetProperty)
        {
            GameObject targetObject = targetProperty.objectReferenceValue as GameObject;
            if (targetObject == null)
            {
                return;
            }

            ParameterStateBinder binder = (ParameterStateBinder)target;
            if (targetObject.transform == binder.transform ||
                !targetObject.transform.IsChildOf(binder.transform))
            {
                EditorGUILayout.HelpBox(
                    "目標必須是 Parameter State Binder 物件的子物件。",
                    MessageType.Error);
            }
        }

        private void DrawCondition(SerializedProperty conditionProperty)
        {
            GameEventConditionGroupDraft root;
            try
            {
                root = GameEventConditionDraftCodec.Parse(conditionProperty.stringValue);
            }
            catch (Exception)
            {
                EditorGUILayout.HelpBox(
                    "此條件使用了結構化編輯器不支援的語法。原始條件仍會保留；" +
                    "若要改用圖形介面編輯，請先清除並重建。",
                    MessageType.Error);
                if (GUILayout.Button("清除並重建", GUILayout.Width(120f)))
                {
                    conditionProperty.stringValue = CreateDefaultConditionSource();
                }

                return;
            }

            string propertyPath = conditionProperty.propertyPath;
            bool changed = GameEventConditionGui.DrawGroup(
                root,
                conditionParameters,
                () => catalog,
                true,
                "尚未設定條件，此綁定無法初始化。",
                ShowCreateParameterPopup,
                () => ApplyAsyncCondition(propertyPath, root),
                out bool _);
            if (changed)
            {
                conditionProperty.stringValue =
                    GameEventConditionDraftCodec.Serialize(root);
            }
        }

        private void AddBinding()
        {
            int index = bindings.arraySize;
            bindings.InsertArrayElementAtIndex(index);
            SerializedProperty binding = bindings.GetArrayElementAtIndex(index);
            binding.FindPropertyRelative("target").objectReferenceValue = null;
            binding.FindPropertyRelative("condition").stringValue =
                CreateDefaultConditionSource();
        }

        private string CreateDefaultConditionSource()
        {
            GameEventConditionGroupDraft root =
                GameEventConditionGui.CreateDefaultRoot(
                    conditionParameters[0].Definition);
            return GameEventConditionDraftCodec.Serialize(root);
        }

        private void ShowCreateParameterPopup(
            Rect anchorRect,
            IReadOnlyList<ParameterType> creatableTypes,
            Action<string> onSelected)
        {
            if (GameEventDocumentEditorWindow
                .HasUnsavedParameterChangesInOpenWindows())
            {
                EditorUtility.DisplayDialog(
                    "Game Event Editor 有尚未儲存的參數",
                    "請先在 Game Event Editor 儲存或捨棄參數表變更，" +
                    "再從 Parameter State Binder 新增參數，避免覆蓋資料。",
                    "開啟 Game Event Editor");
                GameEventDocumentEditorWindow.OpenWindow();
                return;
            }

            PopupWindow.Show(
                anchorRect,
                new CreateParameterPopupContent(
                    parameterWorkspace,
                    creatableTypes,
                    key =>
                    {
                        SaveCreatedParameter();
                        GameEventDocumentEditorWindow
                            .ReloadParameterWorkspacesAfterExternalSave();
                        RefreshCatalog();
                        onSelected(key);
                        Repaint();
                    }));
        }

        private void SaveCreatedParameter()
        {
            try
            {
                parameterWorkspace.SaveAll();
            }
            catch
            {
                parameterWorkspace.ReloadAll();
                throw;
            }
        }

        private void ApplyAsyncCondition(
            string propertyPath,
            GameEventConditionGroupDraft root)
        {
            if (this == null)
            {
                return;
            }

            serializedObject.Update();
            SerializedProperty conditionProperty =
                serializedObject.FindProperty(propertyPath);
            if (conditionProperty == null)
            {
                return;
            }

            conditionProperty.stringValue =
                GameEventConditionDraftCodec.Serialize(root);
            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        private void EnsureCatalogIsCurrent()
        {
            GameEventCatalogAsset selected =
                GameEventEditorProjectSettings.instance.LoadEventCatalog();
            string currentRevision = GetParameterTablesRevision(selected);
            if (!catalogLoaded ||
                selected != eventCatalog ||
                !string.Equals(
                    currentRevision,
                    parameterTablesRevision,
                    StringComparison.Ordinal))
            {
                RefreshCatalog(selected);
            }
        }

        private void RefreshCatalog()
        {
            RefreshCatalog(
                GameEventEditorProjectSettings.instance.LoadEventCatalog());
        }

        private void RefreshCatalog(GameEventCatalogAsset selected)
        {
            eventCatalog = selected;
            parameterWorkspace.Bind(
                eventCatalog?.ParameterTables ?? Array.Empty<TextAsset>());
            catalog = GameEventProjectAuthoringCatalog.Load(eventCatalog);
            conditionParameters = catalog.ParameterEntries
                .Where(entry => GameEventConditionGui.IsSupportedParameterType(
                    entry.Definition.Type))
                .ToArray();
            parameterTablesRevision = GetParameterTablesRevision(selected);
            catalogLoaded = true;
        }

        private static string GetParameterTablesRevision(
            GameEventCatalogAsset selected)
        {
            if (selected == null)
            {
                return string.Empty;
            }

            return string.Join(
                "|",
                selected.ParameterTables
                    .Where(asset => asset != null)
                    .Select(AssetDatabase.GetAssetPath)
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Select(path =>
                        path + ":" + AssetDatabase.GetAssetDependencyHash(path)));
        }

        private void OnProjectChanged()
        {
            RefreshCatalog();
            Repaint();
        }
    }
}
