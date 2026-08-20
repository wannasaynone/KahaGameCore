using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using KahaGameCore.Effects;
using KahaGameCore.Parameters;
using KahaGameCore.Parameters.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KahaGameCore.GameEvents.Editor
{
    public sealed class GameEventDocumentEditorWindow : EditorWindow
    {
        private enum EditorTab
        {
            GameEvent,
            EventCatalog,
            ParameterTables,
            TriggerTimings,
            Commands
        }

        internal enum ConditionSetupState
        {
            SelectParameterTable,
            AddConditionParameter,
            Ready
        }

        private static readonly string[] NumericOperatorLabels =
            { "等於", "不等於", "大於", "大於或等於", "小於", "小於或等於" };
        private static readonly string[] NumericOperatorSymbols =
            { "==", "!=", ">", ">=", "<", "<=" };
        private static readonly string[] BoolOperatorLabels = { "為真", "為假" };

        private static GUIStyle cardStyle;
        private static GUIStyle sectionTitleStyle;
        private static GUIStyle commandTitleStyle;

        [SerializeField] private GameEventEditorSession session;
        [SerializeField] private List<GameEventCommandDraft> commandDrafts =
            new List<GameEventCommandDraft>();
        [SerializeField] private string statusMessage;
        [SerializeField] private MessageType statusType = MessageType.Info;
        [SerializeField] private EditorTab selectedTab;
        [SerializeField] private Vector2 gameEventScrollPosition;
        [SerializeField] private Vector2 eventCatalogScrollPosition;
        [SerializeField] private Vector2 parameterTableScrollPosition;
        [SerializeField] private Vector2 commandCatalogScrollPosition;
        [SerializeField] private Vector2 timingCatalogScrollPosition;
        [SerializeField] private string conditionEditorError;
        [SerializeField] private bool parameterTablesFoldout = true;
        [SerializeField] private bool parameterEditorFoldout = true;
        [SerializeField] private bool serializedPreviewFoldout;
        [SerializeField] private ParameterTableEditorPanel parameterEditor =
            new ParameterTableEditorPanel();

        [NonSerialized] private GameEventProjectAuthoringCatalog catalog;
        [NonSerialized] private GameEventCatalogAsset selectedEventCatalog;
        [NonSerialized] private List<TextAsset> selectedParameterTables;
        [NonSerialized] private GameEventConditionGroupDraft conditionRoot;
        [NonSerialized] private GameEventCatalogEditorPanel eventCatalogEditor;

        private GameEventCatalogEditorPanel EventCatalogEditor
        {
            get
            {
                if (eventCatalogEditor == null)
                {
                    eventCatalogEditor = new GameEventCatalogEditorPanel();
                    eventCatalogEditor.SetCatalog(GetSelectedEventCatalog());
                }

                return eventCatalogEditor;
            }
        }

        private ParameterTableEditorPanel ParameterEditor
        {
            get
            {
                if (parameterEditor == null)
                {
                    parameterEditor = new ParameterTableEditorPanel();
                }

                return parameterEditor;
            }
        }

        private static GUIStyle CardStyle
        {
            get
            {
                if (cardStyle == null)
                {
                    cardStyle = new GUIStyle(EditorStyles.helpBox)
                    {
                        padding = new RectOffset(14, 14, 12, 12),
                        margin = new RectOffset(8, 8, 5, 8)
                    };
                }

                return cardStyle;
            }
        }

        private static GUIStyle SectionTitleStyle
        {
            get
            {
                if (sectionTitleStyle == null)
                {
                    sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 15,
                        margin = new RectOffset(0, 0, 0, 3)
                    };
                }

                return sectionTitleStyle;
            }
        }

        private static GUIStyle CommandTitleStyle
        {
            get
            {
                if (commandTitleStyle == null)
                {
                    commandTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12
                    };
                }

                return commandTitleStyle;
            }
        }

        [MenuItem("KahaGameCore/遊戲事件/遊戲事件編輯器")]
        public static void OpenWindow()
        {
            GameEventDocumentEditorWindow window =
                GetWindow<GameEventDocumentEditorWindow>();
            window.titleContent = new GUIContent("遊戲事件");
            window.minSize = new Vector2(980f, 680f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.hierarchyChanged -= Repaint;
            EditorApplication.hierarchyChanged += Repaint;
            titleContent = new GUIContent("遊戲事件");
            minSize = new Vector2(980f, 680f);
            saveChangesMessage =
                "要儲存此遊戲事件與目前開啟的參數表變更嗎？";
            if (session == null)
            {
                session = new GameEventEditorSession();
                session.ClearDocument();
            }

            RefreshProjectAssets();
            RefreshConditionDrafts();
            RefreshCommandDrafts();
            SyncUnsavedState();
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= Repaint;
        }

        private void OnFocus()
        {
            RefreshProjectAssets();
        }

        private void OnProjectChange()
        {
            RefreshProjectAssets();
        }

        private void RefreshProjectAssets()
        {
            string deletedEventPath = session != null ? session.AssetPath : null;
            bool eventWasDeleted = session != null && session.ResetIfAssetMissing();

            string deletedParameterPath = ParameterEditor.AssetPath;
            bool parameterWasDeleted =
                !string.IsNullOrEmpty(deletedParameterPath) &&
                AssetDatabase.LoadAssetAtPath<TextAsset>(deletedParameterPath) == null;
            if (parameterWasDeleted)
            {
                ParameterEditor.Clear();
            }

            LoadEventCatalogSettings();
            LoadParameterTableSettings();
            EventCatalogEditor.SetCatalog(GetSelectedEventCatalog());
            RefreshCatalog(false);

            if (eventWasDeleted)
            {
                RefreshConditionDrafts();
                RefreshCommandDrafts();
                SetStatus(
                    $"已關閉被刪除的遊戲事件：{deletedEventPath}",
                    MessageType.Warning);
            }
            else if (parameterWasDeleted)
            {
                SetStatus(
                    $"已關閉被刪除的參數表：{deletedParameterPath}",
                    MessageType.Warning);
            }

            Repaint();
        }

        private void OnGUI()
        {
            if (selectedEventCatalog == null)
            {
                DrawEventCatalogSetup();
                SyncUnsavedState();
                return;
            }

            if (!session.HasOpenFile)
            {
                DrawGameEventDocumentSetup();
                SyncUnsavedState();
                return;
            }

            DrawTabs();
            DrawEventCatalogSelector();
            switch (selectedTab)
            {
                case EditorTab.GameEvent:
                    DrawToolbar();
                    gameEventScrollPosition = EditorGUILayout.BeginScrollView(
                        gameEventScrollPosition);
                    EditorGUILayout.Space();
                    DrawDocumentFields();
                    EditorGUILayout.Space();
                    break;
                case EditorTab.EventCatalog:
                    eventCatalogScrollPosition = EditorGUILayout.BeginScrollView(
                        eventCatalogScrollPosition);
                    EditorGUILayout.Space();
                    DrawEventCatalogSettings();
                    EditorGUILayout.Space();
                    break;
                case EditorTab.ParameterTables:
                    parameterTableScrollPosition = EditorGUILayout.BeginScrollView(
                        parameterTableScrollPosition);
                    EditorGUILayout.Space();
                    DrawParameterTableSettings();
                    EditorGUILayout.Space();
                    break;
                case EditorTab.Commands:
                    commandCatalogScrollPosition = EditorGUILayout.BeginScrollView(
                        commandCatalogScrollPosition);
                    EditorGUILayout.Space();
                    DrawCommandCatalogSettings();
                    EditorGUILayout.Space();
                    break;
                case EditorTab.TriggerTimings:
                    timingCatalogScrollPosition = EditorGUILayout.BeginScrollView(
                        timingCatalogScrollPosition);
                    EditorGUILayout.Space();
                    DrawTriggerTimingSettings();
                    EditorGUILayout.Space();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }

            EditorGUILayout.EndScrollView();
            SyncUnsavedState();
        }

        private void DrawTabs()
        {
            string gameEventLabel = session != null && session.IsDirty
                ? "事件編輯 *"
                : "事件編輯";
            string parameterLabel = ParameterEditor.IsDirty
                ? "參數表 *"
                : "參數表";
            selectedTab = (EditorTab)GUILayout.Toolbar(
                (int)selectedTab,
                new[]
                {
                    gameEventLabel,
                    "事件目錄",
                    parameterLabel,
                    "觸發時機",
                    "指令設定"
                },
                GUILayout.Height(26f));
        }

        public override void SaveChanges()
        {
            if (ParameterEditor.IsDirty && !SaveEmbeddedParameterTable())
            {
                return;
            }

            if (session != null && session.IsDirty && !SaveCurrentDocument())
            {
                return;
            }

            base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            session?.MarkClean();
            ParameterEditor?.MarkClean();
            SyncUnsavedState();
            base.DiscardChanges();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("新增", EditorStyles.toolbarButton) && ConfirmDiscardChanges())
                {
                    CreateNewDocumentFile();
                }

                if (GUILayout.Button("開啟", EditorStyles.toolbarButton) && ConfirmDiscardChanges())
                {
                    LoadFromDialog();
                }

                if (GUILayout.Button("重新整理選項", EditorStyles.toolbarButton))
                {
                    RefreshCatalog(true);
                }

                if (GUILayout.Button("驗證", EditorStyles.toolbarButton))
                {
                    RunCommand(
                        ValidateEditorDocument,
                        document =>
                            $"結構、指令語法與 GUID 均正確：" +
                            $"{document.DisplayName}（{document.DocumentGuid:D}）。");
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("另存新檔", EditorStyles.toolbarButton))
                {
                    SaveAsFromDialog();
                }
            }
        }

        private void DrawDocumentFields()
        {
            TextAsset documentAsset;
            using (new EditorGUILayout.VerticalScope(CardStyle))
            {
                DrawSectionHeader(
                    "事件基本資料",
                    "設定事件名稱、觸發時機與識別資訊。");
                documentAsset = DrawDocumentAssetHeader();

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("資料格式版本", session.SchemaVersion);
                }

                DrawDocumentGuid();
                session.DisplayName = EditorGUILayout.TextField(
                    "顯示名稱",
                    session.DisplayName ?? string.Empty);
                DrawTriggerTiming();
                DrawDocumentSaveButton();
            }

            using (new EditorGUILayout.VerticalScope(CardStyle))
            {
                DrawSceneTriggerReferences(documentAsset);
            }

            using (new EditorGUILayout.VerticalScope(CardStyle))
            {
                DrawConditionEditor();
            }

            using (new EditorGUILayout.VerticalScope(CardStyle))
            {
                DrawCommandsEditor();
            }
        }

        private static void DrawSectionHeader(string title, string description = null)
        {
            EditorGUILayout.LabelField(title, SectionTitleStyle);
            if (!string.IsNullOrWhiteSpace(description))
            {
                EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.Space(6f);
        }

        private void DrawDocumentSaveButton()
        {
            if (session == null || !session.IsDirty)
            {
                return;
            }

            Color previousBackgroundColor = GUI.backgroundColor;
            Color previousContentColor = GUI.contentColor;
            GUI.backgroundColor = new Color(1f, 0.3f, 0.22f);
            GUI.contentColor = Color.white;

            GUIContent saveLabel = new GUIContent(
                "儲存變更 *",
                "此遊戲事件有尚未儲存的變更。");
            bool saveClicked = GUILayout.Button(saveLabel, GUILayout.Height(30f));
            GUI.backgroundColor = previousBackgroundColor;
            GUI.contentColor = previousContentColor;
            if (saveClicked)
            {
                SaveCurrentDocument();
            }
        }

        private TextAsset DrawDocumentAssetHeader()
        {
            TextAsset documentAsset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(session.AssetPath);
            GUIStyle fileNameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft
            };

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(
                    Path.GetFileName(session.AssetPath),
                    fileNameStyle,
                    GUILayout.Height(32f));
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(documentAsset == null))
                {
                    if (GUILayout.Button("定位", GUILayout.Width(56f), GUILayout.Height(28f)))
                    {
                        EditorGUIUtility.PingObject(documentAsset);
                    }
                }
            }

            return documentAsset;
        }

        private static void DrawSceneTriggerReferences(TextAsset documentAsset)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            IReadOnlyList<Component> references =
                FindSceneTriggersReferencing(documentAsset, activeScene);

            DrawSectionHeader(
                $"場景觸發器引用（{references.Count}）",
                "列出目前場景中使用此事件的物件。");
            EditorGUILayout.LabelField(
                activeScene.IsValid()
                    ? $"目前場景：{activeScene.name}"
                    : "目前沒有開啟場景。",
                EditorStyles.miniLabel);

            if (references.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "目前場景中沒有觸發器引用此遊戲事件。",
                    MessageType.Info);
                return;
            }

            foreach (Component trigger in references)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(GetSceneObjectPath(trigger), EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(
                        trigger is SceneGameEventTrigger2D ? "2D" : "3D",
                        EditorStyles.miniLabel,
                        GUILayout.Width(24f));
                    if (GUILayout.Button("定位", GUILayout.Width(48f)))
                    {
                        EditorGUIUtility.PingObject(trigger.gameObject);
                    }
                }
            }
        }

        internal static IReadOnlyList<Component> FindSceneTriggersReferencing(
            TextAsset documentAsset,
            Scene scene)
        {
            if (documentAsset == null || !scene.IsValid() || !scene.isLoaded)
            {
                return Array.Empty<Component>();
            }

            List<Component> references = new List<Component>();
            SceneGameEventTrigger[] triggers3D =
                UnityEngine.Object.FindObjectsByType<SceneGameEventTrigger>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            foreach (SceneGameEventTrigger trigger in triggers3D)
            {
                if (trigger.gameObject.scene == scene &&
                    trigger.GameEventFile == documentAsset)
                {
                    references.Add(trigger);
                }
            }

            SceneGameEventTrigger2D[] triggers2D =
                UnityEngine.Object.FindObjectsByType<SceneGameEventTrigger2D>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            foreach (SceneGameEventTrigger2D trigger in triggers2D)
            {
                if (trigger.gameObject.scene == scene &&
                    trigger.GameEventFile == documentAsset)
                {
                    references.Add(trigger);
                }
            }

            references.Sort((left, right) => string.Compare(
                GetSceneObjectPath(left),
                GetSceneObjectPath(right),
                StringComparison.Ordinal));
            return references;
        }

        private static string GetSceneObjectPath(Component component)
        {
            string path = component.gameObject.name;
            Transform parent = component.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private void DrawEventCatalogSettings()
        {
            DrawSectionHeader(
                "執行事件目錄",
                "事件目錄同時決定可編輯的事件清單，以及相同觸發時機下的執行順序。");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(
                    "事件目錄", selectedEventCatalog, typeof(GameEventCatalogAsset), false);

                using (new EditorGUI.DisabledScope(EventCatalogEditor.Catalog == null))
                {
                    if (GUILayout.Button("定位", GUILayout.Width(48f)))
                    {
                        EditorGUIUtility.PingObject(EventCatalogEditor.Catalog);
                    }
                }
            }

            EditorGUILayout.Space();
            TextAsset currentEvent = string.IsNullOrEmpty(session.AssetPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<TextAsset>(session.AssetPath);
            try
            {
                EventCatalogEditor.Draw(currentEvent, OpenCatalogEvent);
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
            }
        }

        private void DrawCommandCatalogSettings()
        {
            DrawSectionHeader(
                "指令來源範圍",
                "只會掃描下列 asmdef 中提供的指令描述；這些技術名稱需保留原文。");

            IReadOnlyList<string> availableAssemblies =
                EffectCommandAssemblyCatalog.GetProviderAssemblyNames();
            List<string> selectedAssemblies = selectedEventCatalog.CommandAssemblyNames.ToList();
            bool changed = false;
            foreach (string assemblyName in availableAssemblies)
            {
                bool wasSelected = selectedAssemblies.Contains(assemblyName);
                bool isSelected = EditorGUILayout.ToggleLeft(assemblyName, wasSelected);
                if (isSelected == wasSelected) continue;
                if (isSelected) selectedAssemblies.Add(assemblyName);
                else selectedAssemblies.Remove(assemblyName);
                changed = true;
            }

            if (availableAssemblies.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "找不到包含 IEffectCommandDescriptorProvider 實作的 asmdef。",
                    MessageType.Warning);
            }

            if (changed)
            {
                Undo.RecordObject(selectedEventCatalog, "Change Game Event Command Assemblies");
                selectedEventCatalog.SetCommandAssemblyNames(selectedAssemblies);
                EditorUtility.SetDirty(selectedEventCatalog);
                RefreshCatalog(false);
            }

            EditorGUILayout.Space();
            DrawSectionHeader(
                "可用指令",
                "勾選要開放給此事件目錄使用的指令；只有勾選的指令會出現在事件編輯頁。");

            List<string> discoveryWarnings = new List<string>();
            List<EffectCommandDescriptor> registeredCommands =
                EffectCommandAssemblyCatalog.GetDescriptors(
                        selectedEventCatalog.CommandAssemblyNames,
                        discoveryWarnings)
                    .ToList();
            foreach (string warning in discoveryWarnings)
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            if (registeredCommands.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "選取的 asmdef 範圍中找不到任何指令。",
                    MessageType.Warning);
                return;
            }

            List<string> selectedNames = selectedEventCatalog.EnabledCommandNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            HashSet<string> selected =
                new HashSet<string>(selectedNames, StringComparer.Ordinal);
            changed = false;

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("全部啟用", GUILayout.Width(96f)))
                {
                    selectedNames = registeredCommands
                        .Select(command => command.Name)
                        .ToList();
                    selected = new HashSet<string>(selectedNames, StringComparer.Ordinal);
                    changed = true;
                }

                if (GUILayout.Button("全部清除", GUILayout.Width(96f)))
                {
                    selectedNames.Clear();
                    selected.Clear();
                    changed = true;
                }
            }

            foreach (IGrouping<string, EffectCommandDescriptor> group in
                     registeredCommands.GroupBy(command => command.Category ?? string.Empty))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(
                    string.IsNullOrWhiteSpace(group.Key)
                        ? "未分類"
                        : group.Key,
                    EditorStyles.miniBoldLabel);
                foreach (EffectCommandDescriptor descriptor in group)
                {
                    bool wasSelected = selected.Contains(descriptor.Name);
                    bool isSelected = EditorGUILayout.ToggleLeft(
                        FormatCommandLabel(descriptor),
                        wasSelected);
                    if (isSelected == wasSelected)
                    {
                        continue;
                    }

                    if (isSelected)
                    {
                        selected.Add(descriptor.Name);
                        selectedNames.Add(descriptor.Name);
                    }
                    else
                    {
                        selected.Remove(descriptor.Name);
                        selectedNames.RemoveAll(name => name == descriptor.Name);
                    }

                    changed = true;
                }
            }

            HashSet<string> registeredNames = new HashSet<string>(
                registeredCommands.Select(command => command.Name),
                StringComparer.Ordinal);
            List<string> missingNames = selectedNames
                .Where(name => !registeredNames.Contains(name))
                .ToList();
            if (missingNames.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("遺失的指令註冊", EditorStyles.miniBoldLabel);
                foreach (string missingName in missingNames)
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField($"遺失／{missingName}");
                        if (GUILayout.Button("移除", GUILayout.Width(72f)))
                        {
                            selectedNames.RemoveAll(name => name == missingName);
                            changed = true;
                        }
                    }
                }
            }

            if (!changed)
            {
                return;
            }

            Undo.RecordObject(selectedEventCatalog, "Change Game Event Commands");
            selectedEventCatalog.SetEnabledCommandNames(selectedNames);
            EditorUtility.SetDirty(selectedEventCatalog);
            RefreshCatalog(false);
            SetStatus(
                $"已更新可用指令，共選取 {selectedNames.Count} 個。",
                MessageType.Info);
        }

        private void DrawEventCatalogSetup()
        {
            EditorGUILayout.Space(24f);
            using (new EditorGUILayout.VerticalScope(CardStyle))
            {
                DrawSectionHeader("需要事件目錄", "建立或指定事件目錄後，才能開始編輯遊戲事件。");
                EditorGUILayout.HelpBox(
                    "事件目錄會管理事件清單、執行順序、觸發時機、參數表與可用指令。",
                    MessageType.Warning);

                GameEventCatalogAsset existing = (GameEventCatalogAsset)EditorGUILayout.ObjectField(
                    "使用既有目錄",
                    null,
                    typeof(GameEventCatalogAsset),
                    false);
                if (existing != null)
                {
                    SetEventCatalog(existing);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.Space(8f);
                if (GUILayout.Button(
                        "建立事件目錄",
                        GUILayout.Height(34f)))
                {
                    CreateEventCatalog();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "建立完成後會自動選取。事件目錄只保存事件編輯與執行所需資料。",
                EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawGameEventDocumentSetup()
        {
            EditorGUILayout.Space(24f);
            using (new EditorGUILayout.VerticalScope(CardStyle))
            {
                DrawSectionHeader("尚未開啟遊戲事件", "建立新事件，或開啟既有的 .gameevent.json 檔案。");
                EditorGUILayout.HelpBox(
                    "開啟事件後，即可設定觸發時機、執行條件與指令。",
                    MessageType.Info);

                if (GUILayout.Button("建立新事件", GUILayout.Height(34f)))
                {
                    if (CreateNewDocumentFile())
                    {
                        GUIUtility.ExitGUI();
                    }
                }

                if (GUILayout.Button("開啟既有事件", GUILayout.Height(30f)))
                {
                    LoadFromDialog();
                    if (session.HasOpenFile)
                    {
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        private bool CreateNewDocumentFile()
        {
            string selectedPath = EditorUtility.SaveFilePanelInProject(
                "建立遊戲事件",
                "NewGameEvent.gameevent",
                "json",
                "在 Assets/ 下建立並開啟遊戲事件檔案。");
            if (string.IsNullOrEmpty(selectedPath))
            {
                return false;
            }

            selectedPath = EnsureGameEventExtension(selectedPath);
            if (AssetDatabase.LoadMainAssetAtPath(selectedPath) != null)
            {
                SetStatus(
                    $"路徑「{selectedPath}」已存在遊戲事件，請改用其他檔名。",
                    MessageType.Error);
                return false;
            }

            try
            {
                GameEventCatalogAsset eventCatalog =
                    GetOrCreateEventCatalogForNewEvent(selectedPath);
                GameEventEditorSession created = new GameEventEditorSession();
                created.NewDocument();
                created.SaveDocument(selectedPath);
                TextAsset createdAsset =
                    AssetDatabase.LoadAssetAtPath<TextAsset>(selectedPath);
                if (createdAsset == null)
                {
                    throw new InvalidOperationException(
                        $"無法載入剛建立的遊戲事件：{selectedPath}");
                }

                EventCatalogEditor.SetCatalog(eventCatalog);
                EventCatalogEditor.AddEvent(createdAsset);

                session = created;
                selectedTab = EditorTab.GameEvent;
                RefreshConditionDrafts();
                RefreshCommandDrafts();
                EventCatalogEditor.Refresh();
                Selection.activeObject = createdAsset;
                SetStatus(
                    $"已建立、加入目錄並開啟：{selectedPath}",
                    MessageType.Info);
                SyncUnsavedState();
                return true;
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
                return false;
            }
        }

        private GameEventCatalogAsset GetOrCreateEventCatalogForNewEvent(
            string eventAssetPath)
        {
            GameEventCatalogAsset eventCatalog = GetSelectedEventCatalog();
            if (eventCatalog != null)
            {
                return eventCatalog;
            }

            string catalogPath = GetDefaultEventCatalogPath(eventAssetPath);
            UnityEngine.Object existingAsset =
                AssetDatabase.LoadMainAssetAtPath(catalogPath);
            if (existingAsset != null && !(existingAsset is GameEventCatalogAsset))
            {
                throw new InvalidOperationException(
                    $"無法建立事件目錄，因為「{catalogPath}」已被其他資產類型使用。");
            }

            eventCatalog = existingAsset as GameEventCatalogAsset;
            if (eventCatalog == null)
            {
                eventCatalog = CreateInstance<GameEventCatalogAsset>();
                AssetDatabase.CreateAsset(eventCatalog, catalogPath);
                AssetDatabase.SaveAssets();
            }

            SetEventCatalog(eventCatalog);
            return eventCatalog;
        }

        internal static string GetDefaultEventCatalogPath(string eventAssetPath)
        {
            string directory = Path.GetDirectoryName(eventAssetPath)
                ?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException(
                    "遊戲事件資產必須位於資料夾內。",
                    nameof(eventAssetPath));
            }

            return directory + "/GameEventCatalog.asset";
        }

        private void DrawEventCatalogSelector()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GameEventCatalogAsset selected =
                    (GameEventCatalogAsset)EditorGUILayout.ObjectField(
                    "事件目錄",
                    selectedEventCatalog,
                    typeof(GameEventCatalogAsset),
                    false);
                if (selected != selectedEventCatalog)
                {
                    SetEventCatalog(selected);
                }

                if (GUILayout.Button("建立新的", GUILayout.Width(92f)))
                {
                    CreateEventCatalog();
                    GUIUtility.ExitGUI();
                }

                using (new EditorGUI.DisabledScope(selectedEventCatalog == null))
                {
                    if (GUILayout.Button("定位", GUILayout.Width(48f)))
                    {
                        EditorGUIUtility.PingObject(selectedEventCatalog);
                    }
                }
            }
        }

        private void SetEventCatalog(GameEventCatalogAsset selected)
        {
            try
            {
                GameEventEditorProjectSettings.instance.SetEventCatalog(selected);
                selectedEventCatalog = selected;
                LoadParameterTableSettings();
                EventCatalogEditor.SetCatalog(selected);
                RefreshCatalog(false);
                SetStatus(
                    selected == null
                        ? "已清除事件目錄選擇。"
                        : $"已選擇事件目錄：{AssetDatabase.GetAssetPath(selected)}",
                    MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
            }
        }

        private void CreateEventCatalog()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "建立事件目錄",
                "GameEventCatalog",
                "asset",
                "在 Assets/ 下建立執行時使用的事件目錄。");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                GameEventCatalogAsset created =
                    CreateInstance<GameEventCatalogAsset>();
                AssetDatabase.CreateAsset(created, path);
                AssetDatabase.SaveAssets();
                SetEventCatalog(created);
                Selection.activeObject = created;
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
            }
        }

        private void OpenCatalogEvent(TextAsset eventAsset)
        {
            if (eventAsset == null || !ConfirmDiscardChanges())
            {
                return;
            }

            bool loaded = RunCommand(
                () =>
                {
                    session.LoadDocument(AssetDatabase.GetAssetPath(eventAsset));
                    return session.ValidateDocument();
                },
                document => $"已從目錄開啟：{document.DisplayName}。");
            if (!loaded)
            {
                return;
            }

            RefreshCatalog(false);
            RefreshConditionDrafts();
            RefreshCommandDrafts();
            selectedTab = EditorTab.GameEvent;
        }

        private void DrawDocumentGuid()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("事件 GUID", session.DocumentGuid ?? string.Empty);
                }

                if (GUILayout.Button("複製", GUILayout.Width(52f)))
                {
                    EditorGUIUtility.systemCopyBuffer = session.DocumentGuid ?? string.Empty;
                    SetStatus("已複製事件 GUID。", MessageType.Info);
                }

                if (GUILayout.Button("重新產生", GUILayout.Width(82f)) &&
                    EditorUtility.DisplayDialog(
                        "要重新產生事件 GUID 嗎？",
                        "目前引用此 GUID 的資料將無法再識別這份事件。",
                        "重新產生",
                        "取消"))
                {
                    session.RegenerateDocumentGuid();
                    SetStatus("已重新產生事件 GUID。", MessageType.Warning);
                }
            }
        }

        private void DrawParameterTableSettings()
        {
            if (selectedParameterTables == null)
            {
                LoadParameterTableSettings();
            }

            int selectedCount = selectedParameterTables.Count(table => table != null);
            parameterTablesFoldout = EditorGUILayout.Foldout(
                parameterTablesFoldout,
                $"編輯用參數表（{selectedCount}）",
                true);
            if (parameterTablesFoldout)
            {
                EditorGUILayout.HelpBox(
                    "只有此處選取的參數表會出現在條件與參數鍵選項中。" +
                    "可建立新表或加入既有表，儲存後會立即重新整理選項。",
                    MessageType.Info);

                int removeIndex = -1;
                for (int index = 0; index < selectedParameterTables.Count; index++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        TextAsset current = selectedParameterTables[index];
                        TextAsset selected = (TextAsset)EditorGUILayout.ObjectField(
                            $"參數表 {index + 1}",
                            current,
                            typeof(TextAsset),
                            false);
                        if (selected != current)
                        {
                            TrySetParameterTable(index, selected);
                        }

                        using (new EditorGUI.DisabledScope(current == null))
                        {
                            if (GUILayout.Button("編輯", GUILayout.Width(48f)))
                            {
                                OpenEmbeddedParameterTable(current);
                            }
                        }

                        if (GUILayout.Button("×", GUILayout.Width(28f)))
                        {
                            removeIndex = index;
                        }
                    }
                }

                if (removeIndex >= 0)
                {
                    RemoveParameterTableAt(removeIndex);
                }

                using (new EditorGUI.DisabledScope(selectedEventCatalog == null))
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("＋ 建立新參數表", GUILayout.Width(160f)))
                    {
                        CreateParameterTableFromDialog();
                    }

                    if (GUILayout.Button("＋ 加入既有參數表", GUILayout.Width(170f)))
                    {
                        selectedParameterTables.Add(null);
                    }
                }

                if (selectedCount == 0)
                {
                    EditorGUILayout.HelpBox(
                        selectedEventCatalog == null
                            ? "請先選擇或建立事件目錄，再加入參數表。"
                            : "目前未選取參數表；專案與範例資料不會自動載入。",
                        MessageType.Warning);
                }
            }

            DrawEmbeddedParameterEditor();
        }

        private void CreateParameterTableFromDialog()
        {
            if (!ConfirmDiscardParameterTableChanges())
            {
                return;
            }

            string selectedPath = EditorUtility.SaveFilePanelInProject(
                "建立參數表",
                "NewParameterTable.parameters",
                "json",
                "在 Assets/ 下建立參數表 JSON。");
            if (string.IsNullOrEmpty(selectedPath))
            {
                return;
            }

            CreateParameterTableAtPath(EnsureParameterTableExtension(selectedPath));
        }

        internal bool CreateParameterTableAtPath(string assetPath)
        {
            try
            {
                if (selectedParameterTables == null)
                {
                    LoadParameterTableSettings();
                }

                ParameterEditor.NewTable();
                ParameterEditor.SaveTable(assetPath);

                TextAsset created = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    ParameterEditor.AssetPath);
                if (created == null)
                {
                    throw new InvalidOperationException(
                        $"無法載入剛建立的參數表：{ParameterEditor.AssetPath}");
                }

                bool alreadySelected = selectedParameterTables.Any(
                    table => table != null && string.Equals(
                        AssetDatabase.GetAssetPath(table),
                        ParameterEditor.AssetPath,
                        StringComparison.OrdinalIgnoreCase));
                int emptyIndex = selectedParameterTables.FindIndex(table => table == null);
                if (alreadySelected)
                {
                    if (emptyIndex >= 0)
                    {
                        selectedParameterTables.RemoveAt(emptyIndex);
                    }
                }
                else if (emptyIndex >= 0)
                {
                    selectedParameterTables[emptyIndex] = created;
                }
                else
                {
                    selectedParameterTables.Add(created);
                }

                SaveParameterTableSettings();
                parameterEditorFoldout = true;
                SetStatus(
                    $"已建立並開啟參數表：{ParameterEditor.AssetPath}",
                    MessageType.Info);
                SyncUnsavedState();
                return true;
            }
            catch (Exception exception)
            {
                ParameterEditor.SetStatus(exception.Message, MessageType.Error);
                SetStatus(exception.Message, MessageType.Error);
                SyncUnsavedState();
                return false;
            }
        }

        internal static string EnsureParameterTableExtension(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) ||
                assetPath.EndsWith(".parameters.json", StringComparison.OrdinalIgnoreCase))
            {
                return assetPath;
            }

            return assetPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? assetPath.Substring(0, assetPath.Length - ".json".Length) +
                    ".parameters.json"
                : assetPath + ".parameters.json";
        }

        private void TrySetParameterTable(int index, TextAsset selected)
        {
            if (selected != null)
            {
                string path = AssetDatabase.GetAssetPath(selected);
                if (!path.EndsWith(".parameters.json", StringComparison.OrdinalIgnoreCase))
                {
                    SetStatus(
                        $"「{path}」不是 .parameters.json 資產。",
                        MessageType.Error);
                    return;
                }

                for (int tableIndex = 0;
                     tableIndex < selectedParameterTables.Count;
                     tableIndex++)
                {
                    if (tableIndex != index && selectedParameterTables[tableIndex] == selected)
                    {
                        SetStatus(
                            $"參數表「{path}」已經選取。",
                            MessageType.Error);
                        return;
                    }
                }
            }

            TextAsset current = selectedParameterTables[index];
            if (IsEditingParameterTable(current) && !ConfirmDiscardParameterTableChanges())
            {
                return;
            }

            if (IsEditingParameterTable(current))
            {
                ParameterEditor.Clear();
            }

            selectedParameterTables[index] = selected;
            SaveParameterTableSettings();
        }

        private void RemoveParameterTableAt(int index)
        {
            TextAsset current = selectedParameterTables[index];
            if (IsEditingParameterTable(current) && !ConfirmDiscardParameterTableChanges())
            {
                return;
            }

            if (IsEditingParameterTable(current))
            {
                ParameterEditor.Clear();
            }

            selectedParameterTables.RemoveAt(index);
            SaveParameterTableSettings();
        }

        private void OpenEmbeddedParameterTable(TextAsset tableAsset)
        {
            if (tableAsset == null)
            {
                return;
            }

            string path = AssetDatabase.GetAssetPath(tableAsset);
            if (string.Equals(
                path,
                ParameterEditor.AssetPath,
                StringComparison.OrdinalIgnoreCase))
            {
                parameterEditorFoldout = true;
                return;
            }

            if (!ConfirmDiscardParameterTableChanges())
            {
                return;
            }

            try
            {
                ParameterEditor.LoadTable(path);
                parameterEditorFoldout = true;
                SetStatus($"正在編輯參數表：{path}", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
            }
        }

        private void DrawEmbeddedParameterEditor()
        {
            if (string.IsNullOrEmpty(ParameterEditor.AssetPath))
            {
                return;
            }

            EditorGUILayout.Space();
            string title = string.IsNullOrWhiteSpace(ParameterEditor.TableDisplayName)
                ? "參數表編輯器"
                : "參數表編輯器 — " + ParameterEditor.TableDisplayName;
            parameterEditorFoldout = EditorGUILayout.Foldout(
                parameterEditorFoldout,
                title,
                true);
            if (!parameterEditorFoldout)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    bool isDirty = ParameterEditor.IsDirty;
                    Color previousBackgroundColor = GUI.backgroundColor;
                    Color previousContentColor = GUI.contentColor;
                    if (isDirty)
                    {
                        GUI.backgroundColor = new Color(1f, 0.58f, 0.16f);
                        GUI.contentColor = Color.white;
                    }

                    GUIContent saveLabel = new GUIContent(
                        isDirty ? "儲存參數表 *" : "儲存參數表",
                        isDirty
                            ? "此參數表有尚未儲存的變更。"
                            : "儲存此參數表。");
                    bool saveClicked = GUILayout.Button(
                        saveLabel,
                        EditorStyles.toolbarButton,
                        GUILayout.Width(104f));
                    GUI.backgroundColor = previousBackgroundColor;
                    GUI.contentColor = previousContentColor;
                    if (saveClicked)
                    {
                        SaveEmbeddedParameterTable();
                    }

                    if (GUILayout.Button("重新載入", EditorStyles.toolbarButton) &&
                        ConfirmDiscardParameterTableChanges())
                    {
                        ReloadEmbeddedParameterTable();
                    }

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("關閉", EditorStyles.toolbarButton) &&
                        ConfirmDiscardParameterTableChanges())
                    {
                        ParameterEditor.Clear();
                        SyncUnsavedState();
                    }
                }

                if (!string.IsNullOrEmpty(ParameterEditor.AssetPath))
                {
                    ParameterEditor.Draw();
                }
            }
        }

        private bool SaveEmbeddedParameterTable()
        {
            if (string.IsNullOrEmpty(ParameterEditor.AssetPath))
            {
                return true;
            }

            try
            {
                string path = ParameterEditor.AssetPath;
                ParameterEditor.SaveTable(path);
                RefreshCatalog(false);
                SetStatus(
                    $"已儲存參數表並重新整理事件選項：{path}",
                    MessageType.Info);
                SyncUnsavedState();
                return true;
            }
            catch (Exception exception)
            {
                ParameterEditor.SetStatus(exception.Message, MessageType.Error);
                SetStatus(exception.Message, MessageType.Error);
                SyncUnsavedState();
                return false;
            }
        }

        private void ReloadEmbeddedParameterTable()
        {
            try
            {
                ParameterEditor.Reload();
                RefreshCatalog(false);
                SetStatus("已從磁碟重新載入參數表。", MessageType.Info);
                SyncUnsavedState();
            }
            catch (Exception exception)
            {
                ParameterEditor.SetStatus(exception.Message, MessageType.Error);
                SetStatus(exception.Message, MessageType.Error);
            }
        }

        private bool ConfirmDiscardParameterTableChanges()
        {
            return !ParameterEditor.IsDirty || EditorUtility.DisplayDialog(
                "參數表尚未儲存",
                $"要捨棄「{ParameterEditor.TableDisplayName}」尚未儲存的變更嗎？",
                "捨棄變更",
                "取消");
        }

        private bool IsEditingParameterTable(TextAsset tableAsset)
        {
            return tableAsset != null &&
                string.Equals(
                    AssetDatabase.GetAssetPath(tableAsset),
                    ParameterEditor.AssetPath,
                    StringComparison.OrdinalIgnoreCase);
        }

        private void LoadEventCatalogSettings()
        {
            selectedEventCatalog =
                GameEventEditorProjectSettings.instance.LoadEventCatalog();
        }

        private GameEventCatalogAsset GetSelectedEventCatalog()
        {
            return selectedEventCatalog;
        }

        private void LoadParameterTableSettings()
        {
            selectedParameterTables = selectedEventCatalog != null
                ? new List<TextAsset>(selectedEventCatalog.ParameterTables)
                : new List<TextAsset>();
        }

        private void SaveParameterTableSettings()
        {
            try
            {
                if (selectedEventCatalog == null)
                    throw new InvalidOperationException("請先選擇事件目錄。");
                Undo.RecordObject(selectedEventCatalog, "Change Game Event Parameter Tables");
                selectedEventCatalog.SetParameterTables(
                    selectedParameterTables.Where(table => table != null));
                EditorUtility.SetDirty(selectedEventCatalog);
                RefreshCatalog(true);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
            }
        }

        private void DrawTriggerTimingSettings()
        {
            DrawSectionHeader(
                "觸發時機",
                "管理可供事件選擇的觸發契約；修改名稱不會自動更新既有事件。");
            EditorGUILayout.HelpBox(
                "可在此新增、重新命名或移除觸發時機。請避免建立重複名稱。",
                MessageType.Info);

            List<string> values = selectedEventCatalog.TriggerTimings.ToList();
            bool changed = false;
            int removeIndex = -1;
            for (int index = 0; index < values.Count; index++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string edited = EditorGUILayout.TextField($"時機 {index + 1}", values[index]);
                    if (!string.Equals(edited, values[index], StringComparison.Ordinal))
                    {
                        values[index] = edited;
                        changed = true;
                    }

                    if (GUILayout.Button("×", GUILayout.Width(28f)))
                        removeIndex = index;
                }
            }

            if (removeIndex >= 0)
            {
                values.RemoveAt(removeIndex);
                changed = true;
            }

            if (GUILayout.Button("＋ 新增觸發時機", GUILayout.Width(170f), GUILayout.Height(28f)))
            {
                values.Add("NewTiming");
                changed = true;
            }

            IEnumerable<string> normalized = values
                .Select(value => value?.Trim())
                .Where(value => !string.IsNullOrEmpty(value));
            if (normalized.Count() != normalized.Distinct(StringComparer.Ordinal).Count())
            {
                EditorGUILayout.HelpBox(
                    "觸發時機名稱不可重複；儲存時會忽略重複項目。",
                    MessageType.Warning);
            }

            if (!changed) return;
            Undo.RecordObject(selectedEventCatalog, "Change Game Event Trigger Timings");
            selectedEventCatalog.SetTriggerTimings(values);
            EditorUtility.SetDirty(selectedEventCatalog);
            RefreshCatalog(false);
        }

        private void DrawTriggerTiming()
        {
            EnsureCatalog();
            List<string> values = new List<string> { string.Empty };
            values.AddRange(catalog.TriggerTimings.Where(value => !string.IsNullOrWhiteSpace(value)));
            bool currentTimingIsUnconfigured =
                !string.IsNullOrWhiteSpace(session.TriggerTiming) &&
                !values.Contains(session.TriggerTiming);
            if (currentTimingIsUnconfigured)
            {
                values.Add(session.TriggerTiming);
            }

            string[] labels = values
                .Select(value => string.IsNullOrEmpty(value)
                    ? "由場景直接觸發（無指定時機）"
                    : value)
                .ToArray();
            int selectedIndex = Math.Max(0, values.IndexOf(session.TriggerTiming ?? string.Empty));
            int newIndex = EditorGUILayout.Popup("觸發時機", selectedIndex, labels);
            session.TriggerTiming = values[newIndex];
            if (!string.IsNullOrWhiteSpace(session.TriggerTiming) &&
                !catalog.TriggerTimings.Contains(session.TriggerTiming))
            {
                EditorGUILayout.HelpBox(
                    $"此事件使用「{session.TriggerTiming}」，但事件目錄中尚未設定這個觸發時機。",
                    MessageType.Warning);
            }
        }

        private void DrawConditionEditor()
        {
            EnsureCatalog();
            if (conditionRoot == null)
            {
                RefreshConditionDrafts();
            }

            DrawSectionHeader(
                "執行條件",
                "只有條件成立時才會執行下方指令；沒有條件代表永遠執行。");

            if (!string.IsNullOrEmpty(conditionEditorError))
            {
                EditorGUILayout.HelpBox(
                    "此事件使用了結構化編輯器不支援的條件語法。原始條件仍會保留；" +
                    "若要改用圖形介面編輯，請先清除並重建。",
                    MessageType.Error);
                if (GUILayout.Button("清除並重建", GUILayout.Width(130f)))
                {
                    conditionRoot = new GameEventConditionGroupDraft();
                    conditionEditorError = null;
                    session.Condition = string.Empty;
                }

                return;
            }

            ParameterDefinition[] conditionParameters = catalog.Parameters
                .Where(parameter => parameter.Type != ParameterType.String)
                .ToArray();
            ConditionSetupState setupState = GetConditionSetupState(
                selectedParameterTables != null &&
                selectedParameterTables.Any(table => table != null),
                conditionParameters.Length);
            if (conditionParameters.Length == 0)
            {
                if (conditionRoot.Children.Count == 0)
                {
                    EditorGUILayout.LabelField("永遠執行。", EditorStyles.miniLabel);
                    if (GUILayout.Button(
                            setupState == ConditionSetupState.SelectParameterTable
                                ? "先選擇參數表，才能新增條件"
                                : "先新增布林或數值參數，才能建立條件",
                            GUILayout.Height(28f)))
                    {
                        selectedTab = EditorTab.ParameterTables;
                        parameterTablesFoldout = true;
                        parameterTableScrollPosition = Vector2.zero;
                        GUI.FocusControl(null);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "條件引用的參數不在目前選取的參數表中。",
                        MessageType.Error);
                    if (GUILayout.Button("清除條件", GUILayout.Width(130f)))
                    {
                        conditionRoot = new GameEventConditionGroupDraft();
                        session.Condition = string.Empty;
                    }
                }

                return;
            }

            bool changed = DrawConditionGroup(
                conditionRoot,
                conditionParameters,
                true,
                out bool _);
            if (changed)
            {
                session.Condition = GameEventConditionDraftCodec.Serialize(conditionRoot);
            }
        }

        internal static ConditionSetupState GetConditionSetupState(
            bool hasSelectedParameterTable,
            int conditionParameterCount)
        {
            return conditionParameterCount > 0
                ? ConditionSetupState.Ready
                : hasSelectedParameterTable
                    ? ConditionSetupState.AddConditionParameter
                    : ConditionSetupState.SelectParameterTable;
        }

        private bool DrawConditionGroup(
            GameEventConditionGroupDraft group,
            IReadOnlyList<ParameterDefinition> parameters,
            bool isRoot,
            out bool removeRequested)
        {
            bool changed = false;
            removeRequested = false;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(
                        isRoot ? "主要條件群組" : "條件群組",
                        EditorStyles.boldLabel,
                        GUILayout.Width(110f));
                    EditorGUI.BeginChangeCheck();
                    group.Mode = (GameEventConditionGroupMode)EditorGUILayout.Popup(
                        (int)group.Mode,
                        new[] { "全部成立（AND）", "任一成立（OR）" });
                    changed |= EditorGUI.EndChangeCheck();

                    if (!isRoot && GUILayout.Button("×", GUILayout.Width(28f)))
                    {
                        removeRequested = true;
                        return true;
                    }
                }

                int removeIndex = -1;
                for (int index = 0; index < group.Children.Count; index++)
                {
                    GameEventConditionDraft child = group.Children[index];
                    if (child is GameEventConditionClauseDraft clause)
                    {
                        changed |= DrawConditionClause(
                            clause,
                            parameters,
                            index,
                            out bool removeClause);
                        if (removeClause)
                        {
                            removeIndex = index;
                        }
                    }
                    else if (child is GameEventConditionGroupDraft childGroup)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(18f);
                            changed |= DrawConditionGroup(
                                childGroup,
                                parameters,
                                false,
                                out bool removeGroup);
                            if (removeGroup)
                            {
                                removeIndex = index;
                            }
                        }
                    }
                }

                if (removeIndex >= 0)
                {
                    group.Children.RemoveAt(removeIndex);
                    changed = true;
                }

                if (group.Children.Count == 0)
                {
                    if (isRoot)
                    {
                        EditorGUILayout.HelpBox("沒有條件，事件會永遠執行。", MessageType.Info);
                    }
                    else
                    {
                        removeRequested = true;
                        return true;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("＋ 新增條件", GUILayout.Width(110f)))
                    {
                        group.Children.Add(CreateDefaultCondition(parameters[0]));
                        changed = true;
                    }

                    if (GUILayout.Button("＋ 新增群組", GUILayout.Width(100f)))
                    {
                        GameEventConditionGroupDraft childGroup =
                            new GameEventConditionGroupDraft();
                        childGroup.Children.Add(CreateDefaultCondition(parameters[0]));
                        group.Children.Add(childGroup);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private bool DrawConditionClause(
            GameEventConditionClauseDraft draft,
            IReadOnlyList<ParameterDefinition> parameters,
            int index,
            out bool removeRequested)
        {
            removeRequested = false;
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"#{index + 1}", GUILayout.Width(28f));
                    string previousKey = draft.ParameterKey;
                    draft.ParameterKey = DrawConditionParameterPopup(
                        draft.ParameterKey,
                        parameters);
                    if (!string.Equals(
                        previousKey,
                        draft.ParameterKey,
                        StringComparison.Ordinal))
                    {
                        ResetConditionForParameter(draft, parameters);
                    }

                    if (GUILayout.Button("×", GUILayout.Width(28f)))
                    {
                        removeRequested = true;
                    }
                }

                if (catalog.TryGetParameter(
                    draft.ParameterKey,
                    out ParameterDefinition selectedParameter))
                {
                    DrawConditionComparison(draft, selectedParameter);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        $"找不到參數「{draft.ParameterKey}」。",
                        MessageType.Error);
                }
            }

            return EditorGUI.EndChangeCheck() || removeRequested;
        }

        private string DrawConditionParameterPopup(
            string currentValue,
            IReadOnlyList<ParameterDefinition> parameters)
        {
            int selectedIndex = parameters
                .Select(parameter => parameter.Key)
                .ToList()
                .IndexOf(currentValue ?? string.Empty);
            bool isKnown = selectedIndex >= 0;
            string[] labels;
            if (isKnown)
            {
                labels = parameters.Select(FormatParameterLabel).ToArray();
            }
            else
            {
                labels = new[] { $"遺失／{currentValue}" }
                    .Concat(parameters.Select(FormatParameterLabel))
                    .ToArray();
                selectedIndex = 0;
            }

            int newIndex = EditorGUILayout.Popup(selectedIndex, labels);
            if (!isKnown && newIndex == 0)
            {
                return currentValue;
            }

            return parameters[isKnown ? newIndex : newIndex - 1].Key;
        }

        private static void DrawConditionComparison(
            GameEventConditionClauseDraft draft,
            ParameterDefinition parameter)
        {
            if (parameter.Type == ParameterType.Bool)
            {
                bool expected = GetExpectedBoolean(draft);
                int selectedIndex = expected ? 0 : 1;
                selectedIndex = EditorGUILayout.Popup(
                    "狀態",
                    selectedIndex,
                    BoolOperatorLabels);
                draft.Operator = "==";
                draft.Value = selectedIndex == 0 ? "true" : "false";
                return;
            }

            int operatorIndex = Array.IndexOf(
                NumericOperatorSymbols,
                draft.Operator ?? string.Empty);
            operatorIndex = Mathf.Max(0, operatorIndex);
            operatorIndex = EditorGUILayout.Popup(
                "比較方式",
                operatorIndex,
                NumericOperatorLabels);
            draft.Operator = NumericOperatorSymbols[operatorIndex];

            if (parameter.Type == ParameterType.Int)
            {
                int.TryParse(
                    draft.Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int value);
                value = EditorGUILayout.IntField("比較值", value);
                draft.Value = value.ToString(CultureInfo.InvariantCulture);
                return;
            }

            float.TryParse(
                draft.Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float floatValue);
            floatValue = EditorGUILayout.FloatField("比較值", floatValue);
            draft.Value = floatValue.ToString("R", CultureInfo.InvariantCulture);
        }

        private static bool GetExpectedBoolean(GameEventConditionClauseDraft draft)
        {
            bool value = string.Equals(
                draft.Value,
                "true",
                StringComparison.OrdinalIgnoreCase);
            return draft.Operator == "!=" ? !value : value;
        }

        private static void ResetConditionForParameter(
            GameEventConditionClauseDraft draft,
            IReadOnlyList<ParameterDefinition> parameters)
        {
            ParameterDefinition parameter = parameters.First(
                candidate => candidate.Key == draft.ParameterKey);
            draft.Operator = "==";
            draft.Value = parameter.Type == ParameterType.Bool ? "true" : "0";
        }

        private static GameEventConditionClauseDraft CreateDefaultCondition(
            ParameterDefinition parameter)
        {
            return new GameEventConditionClauseDraft
            {
                ParameterKey = parameter.Key,
                Operator = "==",
                Value = parameter.Type == ParameterType.Bool ? "true" : "0"
            };
        }

        private void DrawCommandsEditor()
        {
            EnsureCatalog();
            DrawSectionHeader(
                $"執行指令（{commandDrafts.Count}）",
                "指令會依照由上到下的順序執行；欄位中的英文 ID 是實際儲存值。");
            if (catalog.Commands.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "目前的事件目錄尚未啟用任何指令。",
                    MessageType.Warning);
                if (GUILayout.Button("前往設定可用指令", GUILayout.Height(30f)))
                {
                    selectedTab = EditorTab.Commands;
                }
                return;
            }

            int removeIndex = -1;
            int moveFrom = -1;
            int moveTo = -1;
            for (int index = 0; index < commandDrafts.Count; index++)
            {
                GameEventCommandDraft draft = commandDrafts[index];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(
                            $"指令 {index + 1:00}",
                            CommandTitleStyle,
                            GUILayout.Width(80f));
                        GUILayout.FlexibleSpace();
                        using (new EditorGUI.DisabledScope(index == 0))
                        {
                            if (GUILayout.Button(
                                    new GUIContent("上移", "將此指令往前移動"),
                                    GUILayout.Width(52f),
                                    GUILayout.Height(22f)))
                            {
                                moveFrom = index;
                                moveTo = index - 1;
                            }
                        }

                        using (new EditorGUI.DisabledScope(index == commandDrafts.Count - 1))
                        {
                            if (GUILayout.Button(
                                    new GUIContent("下移", "將此指令往後移動"),
                                    GUILayout.Width(52f),
                                    GUILayout.Height(22f)))
                            {
                                moveFrom = index;
                                moveTo = index + 1;
                            }
                        }

                        if (GUILayout.Button(
                                new GUIContent("刪除", "移除此指令"),
                                GUILayout.Width(52f),
                                GUILayout.Height(22f)))
                        {
                            removeIndex = index;
                        }
                    }

                    EditorGUILayout.Space(5f);
                    DrawCommandSelector(draft);

                    if (catalog.TryGetCommand(draft.Name, out EffectCommandDescriptor descriptor))
                    {
                        EnsureArgumentCount(draft, descriptor);
                        EditorGUILayout.Space(4f);
                        EditorGUI.indentLevel++;
                        for (int argumentIndex = 0;
                             argumentIndex < descriptor.Parameters.Count;
                             argumentIndex++)
                        {
                            DrawCommandArgument(draft, descriptor, argumentIndex);
                        }
                        EditorGUI.indentLevel--;
                    }
                    else if (string.IsNullOrWhiteSpace(draft.Name))
                    {
                        EditorGUILayout.HelpBox(
                            "請先選擇指令，再設定它的參數。",
                            MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            $"指令「{draft.Name}」尚未註冊，請選擇替代指令。",
                            MessageType.Error);
                    }
                }

                EditorGUILayout.Space(5f);
            }

            if (removeIndex >= 0)
            {
                commandDrafts.RemoveAt(removeIndex);
            }
            else if (moveFrom >= 0)
            {
                GameEventCommandDraft moving = commandDrafts[moveFrom];
                commandDrafts.RemoveAt(moveFrom);
                commandDrafts.Insert(moveTo, moving);
            }

            DrawAddCommand();
            session.Commands = GameEventCommandDraftCodec.Serialize(
                commandDrafts
                    .Where(draft => !string.IsNullOrWhiteSpace(draft.Name))
                    .ToList());
            EditorGUILayout.Space(6f);
            serializedPreviewFoldout = EditorGUILayout.Foldout(
                serializedPreviewFoldout,
                "進階：序列化內容預覽",
                true);
            if (serializedPreviewFoldout)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextArea(session.Commands, GUILayout.MinHeight(56f));
                }
            }
        }

        private void DrawCommandSelector(GameEventCommandDraft draft)
        {
            List<EffectCommandDescriptor> options = catalog.Commands.ToList();
            bool isKnown = catalog.TryGetCommand(draft.Name, out EffectCommandDescriptor current);
            string[] labels;
            int selectedIndex;
            bool isUnselected = string.IsNullOrWhiteSpace(draft.Name);
            if (isUnselected)
            {
                labels = new[] { "請選擇指令…" }
                    .Concat(options.Select(FormatCommandLabel))
                    .ToArray();
                selectedIndex = 0;
            }
            else if (isKnown)
            {
                labels = options.Select(FormatCommandLabel).ToArray();
                selectedIndex = options.IndexOf(current);
            }
            else
            {
                labels = new[] { $"遺失／{draft.Name}" }
                    .Concat(options.Select(FormatCommandLabel))
                    .ToArray();
                selectedIndex = 0;
            }

            int newIndex = EditorGUILayout.Popup("指令類型", selectedIndex, labels);
            if (newIndex == selectedIndex)
            {
                return;
            }

            EffectCommandDescriptor selected = isKnown
                ? options[newIndex]
                : options[newIndex - 1];
            SetCommandDescriptor(draft, selected);
        }

        private void DrawCommandArgument(
            GameEventCommandDraft draft,
            EffectCommandDescriptor descriptor,
            int argumentIndex)
        {
            EffectCommandParameterDefinition parameter = descriptor.Parameters[argumentIndex];
            string label = FormatCommandParameterLabel(parameter);
            if (parameter.Kind == EffectCommandParameterKind.ParameterKey)
            {
                draft.Arguments[argumentIndex] = DrawParameterKeyPopup(
                    label,
                    draft.Arguments[argumentIndex]);
                return;
            }

            draft.Arguments[argumentIndex] = EditorGUILayout.TextField(
                label,
                draft.Arguments[argumentIndex] ?? string.Empty);
        }

        private string DrawParameterKeyPopup(string label, string currentValue)
        {
            if (catalog.Parameters.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "選取的參數表中找不到任何參數鍵。",
                    MessageType.Error);
                return currentValue;
            }

            List<ParameterDefinition> options = catalog.Parameters.ToList();
            int selectedIndex = options.FindIndex(item => item.Key == currentValue);
            bool isKnown = selectedIndex >= 0;
            string[] labels;
            if (isKnown)
            {
                labels = options.Select(FormatParameterLabel).ToArray();
            }
            else
            {
                labels = new[] { $"遺失／{currentValue}" }
                    .Concat(options.Select(FormatParameterLabel))
                    .ToArray();
                selectedIndex = 0;
            }

            int newIndex = EditorGUILayout.Popup(label, selectedIndex, labels);
            if (!isKnown && newIndex == 0)
            {
                return currentValue;
            }

            return options[isKnown ? newIndex : newIndex - 1].Key;
        }

        private void DrawAddCommand()
        {
            if (GUILayout.Button("＋ 新增指令", GUILayout.Height(32f)))
            {
                commandDrafts.Add(new GameEventCommandDraft());
                GUI.FocusControl(null);
            }
        }

        private void SetCommandDescriptor(
            GameEventCommandDraft draft,
            EffectCommandDescriptor descriptor)
        {
            draft.Name = descriptor.Name;
            draft.Arguments.Clear();
            for (int index = 0; index < descriptor.Parameters.Count; index++)
            {
                EffectCommandParameterDefinition parameter = descriptor.Parameters[index];
                if (parameter.Kind == EffectCommandParameterKind.ParameterKey &&
                    catalog.Parameters.Count > 0)
                {
                    draft.Arguments.Add(catalog.Parameters[0].Key);
                    continue;
                }

                draft.Arguments.Add(string.Empty);
            }
        }

        private static void EnsureArgumentCount(
            GameEventCommandDraft draft,
            EffectCommandDescriptor descriptor)
        {
            while (draft.Arguments.Count < descriptor.Parameters.Count)
            {
                draft.Arguments.Add(string.Empty);
            }

            if (draft.Arguments.Count > descriptor.Parameters.Count)
            {
                draft.Arguments.RemoveRange(
                    descriptor.Parameters.Count,
                    draft.Arguments.Count - descriptor.Parameters.Count);
            }
        }

        private void LoadFromDialog()
        {
            string selectedPath = EditorUtility.OpenFilePanel(
                "開啟遊戲事件",
                Application.dataPath,
                "json");
            if (string.IsNullOrEmpty(selectedPath))
            {
                return;
            }

            bool loaded = RunCommand(
                () =>
                {
                    session.LoadDocument(selectedPath);
                    return session.ValidateDocument();
                },
                document => $"已開啟：{document.DisplayName}。");
            if (loaded)
            {
                RefreshCatalog(false);
                RefreshConditionDrafts();
                RefreshCommandDrafts();
            }
        }

        private bool SaveCurrentDocument()
        {
            if (!session.HasOpenFile)
            {
                SetStatus("目前沒有開啟的遊戲事件檔案。", MessageType.Error);
                return false;
            }

            bool saved = RunCommand(
                () =>
                {
                    ValidateEditorDocument();
                    session.SaveDocument(session.AssetPath);
                    return session.ValidateDocument();
                },
                document => $"已儲存：{document.DisplayName}。");
            if (saved)
            {
                EventCatalogEditor.Refresh();
            }

            return saved;
        }

        private bool SaveAsFromDialog()
        {
            string selectedPath = EditorUtility.SaveFilePanelInProject(
                "儲存遊戲事件",
                GetDefaultFileName(),
                "json",
                "將遊戲事件 JSON 儲存在 Assets/ 下。");
            if (string.IsNullOrEmpty(selectedPath))
            {
                return false;
            }

            selectedPath = EnsureGameEventExtension(selectedPath);

            bool saved = RunCommand(
                () =>
                {
                    ValidateEditorDocument();
                    session.SaveDocument(selectedPath);
                    return session.ValidateDocument();
                },
                document => $"已儲存：{document.DisplayName}。");
            if (saved)
            {
                EventCatalogEditor.Refresh();
            }

            return saved;
        }

        internal static string EnsureGameEventExtension(string assetPath)
        {
            if (assetPath.EndsWith(".gameevent.json", StringComparison.OrdinalIgnoreCase))
            {
                return assetPath;
            }

            return assetPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? assetPath.Substring(0, assetPath.Length - ".json".Length) +
                    ".gameevent.json"
                : assetPath + ".gameevent.json";
        }

        private string GetDefaultFileName()
        {
            string fileName = string.IsNullOrWhiteSpace(session.DisplayName)
                ? "NewGameEvent"
                : session.DisplayName;
            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidCharacter, '_');
            }

            return fileName + ".gameevent";
        }

        private bool ConfirmDiscardChanges()
        {
            return !session.IsDirty || EditorUtility.DisplayDialog(
                "遊戲事件尚未儲存",
                "要捨棄目前尚未儲存的變更嗎？",
                "捨棄變更",
                "取消");
        }

        private bool RunCommand(
            Func<GameEventDocument> command,
            Func<GameEventDocument, string> successMessage)
        {
            try
            {
                GameEventDocument document = command();
                SetStatus(successMessage(document), MessageType.Info);
                SyncUnsavedState();
                return true;
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
                return false;
            }
        }

        private GameEventDocument ValidateEditorDocument()
        {
            EnsureCatalog();
            for (int index = 0; index < commandDrafts.Count; index++)
            {
                GameEventCommandDraft draft = commandDrafts[index];
                if (string.IsNullOrWhiteSpace(draft.Name))
                {
                    throw new InvalidOperationException(
                        $"第 {index + 1} 筆指令尚未選擇指令類型。");
                }

                if (!catalog.TryGetCommand(draft.Name, out EffectCommandDescriptor descriptor))
                {
                    throw new InvalidOperationException(
                        $"第 {index + 1} 筆使用了尚未註冊的指令「{draft.Name}」。");
                }

                if (draft.Arguments.Count != descriptor.Parameters.Count)
                {
                    throw new InvalidOperationException(
                        $"指令「{draft.Name}」需要 {descriptor.Parameters.Count} 個參數。");
                }

                for (int argumentIndex = 0;
                     argumentIndex < descriptor.Parameters.Count;
                     argumentIndex++)
                {
                    if (string.IsNullOrWhiteSpace(draft.Arguments[argumentIndex]))
                    {
                        throw new InvalidOperationException(
                            $"指令「{draft.Name}」的參數「" +
                            $"{descriptor.Parameters[argumentIndex].Name}」不可留空。");
                    }

                    if (descriptor.Parameters[argumentIndex].Kind ==
                            EffectCommandParameterKind.ParameterKey &&
                        !catalog.TryGetParameter(
                            draft.Arguments[argumentIndex],
                            out ParameterDefinition _))
                    {
                        throw new InvalidOperationException(
                            $"指令「{draft.Name}」引用了不存在的參數「" +
                            $"{draft.Arguments[argumentIndex]}」。");
                    }
                }
            }

            if (catalog.Parameters.Count > 0)
            {
                var conditionResult = new ParameterStore(catalog.Parameters)
                    .EvaluateCondition(session.Condition);
                if (!conditionResult.IsSuccess)
                {
                    throw new InvalidOperationException(
                        "執行條件無效：" + conditionResult.Error);
                }
            }

            session.Commands = GameEventCommandDraftCodec.Serialize(commandDrafts);
            return session.ValidateDocument();
        }

        private void RefreshCatalog(bool showStatus)
        {
            if (selectedParameterTables == null)
            {
                LoadParameterTableSettings();
            }

            catalog = GameEventProjectAuthoringCatalog.Load(
                GetSelectedEventCatalog());
            if (!showStatus)
            {
                return;
            }

            string message =
                $"已重新整理選項：{catalog.TriggerTimings.Count} 個觸發時機、" +
                $"{catalog.Parameters.Count} 個參數、{catalog.Commands.Count} 個指令。";
            if (catalog.Warnings.Count > 0)
            {
                message += "\n" + string.Join("\n", catalog.Warnings);
            }

            SetStatus(
                message,
                catalog.Warnings.Count == 0 ? MessageType.Info : MessageType.Warning);
        }

        private void RefreshCommandDrafts()
        {
            try
            {
                commandDrafts = GameEventCommandDraftCodec.Parse(session.Commands);
            }
            catch (Exception exception)
            {
                commandDrafts = new List<GameEventCommandDraft>();
                SetStatus("無法在結構化編輯器中開啟指令：" + exception.Message, MessageType.Error);
            }
        }

        private void RefreshConditionDrafts()
        {
            try
            {
                conditionRoot = GameEventConditionDraftCodec.Parse(session.Condition);
                conditionEditorError = null;
            }
            catch (Exception exception)
            {
                conditionRoot = new GameEventConditionGroupDraft();
                conditionEditorError = exception.Message;
                SetStatus(
                    "無法在結構化編輯器中開啟條件：" + exception.Message,
                    MessageType.Error);
            }
        }

        private void EnsureCatalog()
        {
            if (catalog == null)
            {
                RefreshCatalog(false);
            }
        }

        private void SetStatus(string message, MessageType messageType)
        {
            statusMessage = message;
            statusType = messageType;
            Repaint();
        }

        private void SyncUnsavedState()
        {
            hasUnsavedChanges =
                (session != null && session.IsDirty) || ParameterEditor.IsDirty;
        }

        private static string FormatParameterLabel(ParameterDefinition parameter)
        {
            string displayName = string.IsNullOrWhiteSpace(parameter.DisplayName)
                ? parameter.Key
                : parameter.DisplayName;
            return $"{displayName}　（${parameter.Key}，{FormatParameterType(parameter.Type)}）";
        }

        private static string FormatCommandLabel(EffectCommandDescriptor descriptor)
        {
            return string.IsNullOrWhiteSpace(descriptor.Category)
                ? descriptor.DisplayName
                : descriptor.Category + " / " + descriptor.DisplayName;
        }

        private static string FormatCommandParameterLabel(
            EffectCommandParameterDefinition parameter)
        {
            string kind;
            switch (parameter.Kind)
            {
                case EffectCommandParameterKind.Literal: kind = "固定值"; break;
                case EffectCommandParameterKind.NumberExpression: kind = "數值／公式"; break;
                case EffectCommandParameterKind.ConditionExpression: kind = "條件公式"; break;
                case EffectCommandParameterKind.ParameterKey: kind = "參數鍵"; break;
                case EffectCommandParameterKind.TextKey: kind = "文字鍵"; break;
                case EffectCommandParameterKind.AssetKey: kind = "資源鍵"; break;
                default: kind = parameter.Kind.ToString(); break;
            }

            return $"{parameter.Name}　·　{kind}";
        }

        private static string FormatParameterType(ParameterType type)
        {
            switch (type)
            {
                case ParameterType.Int: return "整數";
                case ParameterType.Float: return "小數";
                case ParameterType.Bool: return "布林";
                case ParameterType.String: return "文字";
                default: return type.ToString();
            }
        }
    }
}
