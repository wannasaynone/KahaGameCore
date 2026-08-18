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

namespace KahaGameCore.GameEvents.Editor
{
    public sealed class GameEventDocumentEditorWindow : EditorWindow
    {
        private enum EditorTab
        {
            GameEvent,
            EventCatalog,
            ParameterTables,
            Commands
        }

        internal enum ConditionSetupState
        {
            SelectParameterTable,
            AddConditionParameter,
            Ready
        }

        private static readonly string[] NumericOperatorLabels =
            { "Equals", "Not Equal", "Greater", "Greater or Equal", "Less", "Less or Equal" };
        private static readonly string[] NumericOperatorSymbols =
            { "==", "!=", ">", ">=", "<", "<=" };
        private static readonly string[] BoolOperatorLabels = { "Is True", "Is False" };

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
        [SerializeField] private string conditionEditorError;
        [SerializeField] private bool parameterTablesFoldout = true;
        [SerializeField] private bool parameterEditorFoldout = true;
        [SerializeField] private ParameterTableEditorPanel parameterEditor =
            new ParameterTableEditorPanel();

        [NonSerialized] private GameEventProjectAuthoringCatalog catalog;
        [NonSerialized] private UnityEngine.Object selectedDataCatalog;
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

        [MenuItem("KahaGameCore/Game Events/Game Event Editor")]
        public static void OpenWindow()
        {
            GameEventDocumentEditorWindow window =
                GetWindow<GameEventDocumentEditorWindow>();
            window.titleContent = new GUIContent("Game Event");
            window.minSize = new Vector2(920f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Game Event");
            minSize = new Vector2(920f, 620f);
            saveChangesMessage =
                "Save changes to this Game Event document and its open Parameter Table?";
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

            LoadDataCatalogSettings();
            LoadParameterTableSettings();
            EventCatalogEditor.SetCatalog(GetSelectedEventCatalog());
            RefreshCatalog(false);

            if (eventWasDeleted)
            {
                RefreshConditionDrafts();
                RefreshCommandDrafts();
                SetStatus(
                    $"Closed deleted Game Event: {deletedEventPath}",
                    MessageType.Warning);
            }
            else if (parameterWasDeleted)
            {
                SetStatus(
                    $"Closed deleted Parameter Table: {deletedParameterPath}",
                    MessageType.Warning);
            }

            Repaint();
        }

        private void OnGUI()
        {
            GameEventEditorDataSource source =
                GameEventEditorCommandCatalog.GetDataSource();
            if (source == null)
            {
                EditorGUILayout.HelpBox(
                    "No Game Event Editor data source is registered.",
                    MessageType.Error);
                SyncUnsavedState();
                return;
            }

            if (!HasRequiredDataCatalog(source, selectedDataCatalog))
            {
                DrawDataCatalogSetup(source);
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
            DrawDataCatalogSelector(source);
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
                    DrawCommandCatalogSettings(source);
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
                ? "Game Event *"
                : "Game Event";
            string parameterLabel = ParameterEditor.IsDirty
                ? "Parameter Tables *"
                : "Parameter Tables";
            selectedTab = (EditorTab)GUILayout.Toolbar(
                (int)selectedTab,
                new[] { gameEventLabel, "Event Catalog", parameterLabel, "Commands" },
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
                if (GUILayout.Button("New", EditorStyles.toolbarButton) && ConfirmDiscardChanges())
                {
                    CreateNewDocumentFile();
                }

                if (GUILayout.Button("Open", EditorStyles.toolbarButton) && ConfirmDiscardChanges())
                {
                    LoadFromDialog();
                }

                if (GUILayout.Button("Refresh Choices", EditorStyles.toolbarButton))
                {
                    RefreshCatalog(true);
                }

                if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
                {
                    RunCommand(
                        ValidateEditorDocument,
                        document =>
                            $"Structure, Commands syntax, and GUID are valid: " +
                            $"{document.DisplayName} ({document.DocumentGuid:D}).");
                }

                if (GUILayout.Button("Save As", EditorStyles.toolbarButton))
                {
                    SaveAsFromDialog();
                }
            }
        }

        private void DrawDocumentFields()
        {
            EditorGUILayout.LabelField("Game Event Document", EditorStyles.boldLabel);
            DrawDocumentAssetHeader();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Schema Version", session.SchemaVersion);
            }

            DrawDocumentGuid();
            session.DisplayName = EditorGUILayout.TextField(
                "Display Name",
                session.DisplayName ?? string.Empty);
            DrawTriggerTiming();
            DrawDocumentSaveButton();
            EditorGUILayout.Space();
            DrawConditionEditor();
            EditorGUILayout.Space();
            DrawCommandsEditor();
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
                "Save *",
                "This Game Event has unsaved changes.");
            bool saveClicked = GUILayout.Button(saveLabel, GUILayout.Height(30f));
            GUI.backgroundColor = previousBackgroundColor;
            GUI.contentColor = previousContentColor;
            if (saveClicked)
            {
                SaveCurrentDocument();
            }
        }

        private void DrawDocumentAssetHeader()
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
                    if (GUILayout.Button("Ping", GUILayout.Width(56f), GUILayout.Height(28f)))
                    {
                        EditorGUIUtility.PingObject(documentAsset);
                    }
                }
            }
        }

        private void DrawEventCatalogSettings()
        {
            EditorGUILayout.LabelField("Runtime Event Catalog", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "This reference is stored in the selected Data Catalog. Its event order is the runtime order for events sharing a TriggerTiming.",
                EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GameEventCatalogAsset selected =
                    (GameEventCatalogAsset)EditorGUILayout.ObjectField(
                        "Catalog",
                        EventCatalogEditor.Catalog,
                        typeof(GameEventCatalogAsset),
                        false);
                if (selected != EventCatalogEditor.Catalog)
                {
                    SetEventCatalog(selected);
                }

                using (new EditorGUI.DisabledScope(selectedDataCatalog == null))
                {
                    if (GUILayout.Button("Create New", GUILayout.Width(92f)))
                    {
                        CreateEventCatalog();
                        GUIUtility.ExitGUI();
                    }
                }

                using (new EditorGUI.DisabledScope(EventCatalogEditor.Catalog == null))
                {
                    if (GUILayout.Button("Ping", GUILayout.Width(48f)))
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

        private void DrawCommandCatalogSettings(GameEventEditorDataSource source)
        {
            EditorGUILayout.LabelField("Available Commands", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Choose which registered Commands belong to the selected Data Catalog. " +
                "Only checked Commands appear in Game Event rows.",
                EditorStyles.wordWrappedMiniLabel);

            List<EffectCommandDescriptor> registeredCommands =
                GameEventEditorCommandCatalog.GetDescriptors()
                    .OrderBy(command => command.Category, StringComparer.Ordinal)
                    .ThenBy(command => command.DisplayName, StringComparer.Ordinal)
                    .ToList();
            if (registeredCommands.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No Commands are registered by the project.",
                    MessageType.Error);
                return;
            }

            List<string> selectedNames = source.GetCommandNames(selectedDataCatalog)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            HashSet<string> selected =
                new HashSet<string>(selectedNames, StringComparer.Ordinal);
            bool changed = false;

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Enable All", GUILayout.Width(96f)))
                {
                    selectedNames = registeredCommands
                        .Select(command => command.Name)
                        .ToList();
                    selected = new HashSet<string>(selectedNames, StringComparer.Ordinal);
                    changed = true;
                }

                if (GUILayout.Button("Clear All", GUILayout.Width(96f)))
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
                    string.IsNullOrWhiteSpace(group.Key) ? "Uncategorized" : group.Key,
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
                EditorGUILayout.LabelField("Missing Registrations", EditorStyles.miniBoldLabel);
                foreach (string missingName in missingNames)
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField($"Missing / {missingName}");
                        if (GUILayout.Button("Remove", GUILayout.Width(72f)))
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

            source.SetCommandNames(selectedDataCatalog, selectedNames);
            RefreshCatalog(false);
            SetStatus(
                $"Available Commands updated: {selectedNames.Count} selected.",
                MessageType.Info);
        }

        private void SetEventCatalog(GameEventCatalogAsset selected)
        {
            try
            {
                GameEventEditorDataSource source = RequireDataSourceAndCatalog();
                source.SetEventCatalog(selectedDataCatalog, selected);
                EventCatalogEditor.SetCatalog(selected);
                SetStatus(
                    selected == null
                        ? "Event Catalog selection cleared."
                        : $"Event Catalog selected: {AssetDatabase.GetAssetPath(selected)}",
                    MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
            }
        }

        internal static bool HasRequiredDataCatalog(
            GameEventEditorDataSource source,
            UnityEngine.Object dataCatalog)
        {
            return source != null && source.IsValidAsset(dataCatalog);
        }

        private void DrawDataCatalogSetup(GameEventEditorDataSource source)
        {
            EditorGUILayout.Space(24f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    $"{source.DisplayName} Required",
                    EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    $"The Game Event Editor needs one shared {source.DisplayName} before " +
                    "events, event order, timings, or Parameter Tables can be edited. " +
                    "Create the project catalog now, or assign an existing one.",
                    MessageType.Warning);

                UnityEngine.Object existing = EditorGUILayout.ObjectField(
                    "Use Existing",
                    null,
                    source.AssetType,
                    false);
                if (existing != null)
                {
                    SetDataCatalog(existing);
                    if (HasRequiredDataCatalog(source, selectedDataCatalog))
                    {
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.Space(8f);
                if (GUILayout.Button(
                        $"Create {source.DisplayName}",
                        GUILayout.Height(34f)))
                {
                    CreateDataCatalog(source);
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "After creation, the new catalog is selected automatically and opened " +
                "in the Inspector so its GameFlow tables can be assigned.",
                EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawGameEventDocumentSetup()
        {
            EditorGUILayout.Space(24f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "Game Event Document Required",
                    EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Create a new Game Event document before editing its timing, " +
                    "conditions, or commands. You can also open an existing " +
                    ".gameevent.json file.",
                    MessageType.Info);

                if (GUILayout.Button("Create New", GUILayout.Height(34f)))
                {
                    if (CreateNewDocumentFile())
                    {
                        GUIUtility.ExitGUI();
                    }
                }

                if (GUILayout.Button("Open Existing", GUILayout.Height(26f)))
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
                "Create Game Event",
                "NewGameEvent.gameevent",
                "json",
                "Create and open a canonical Game Event file under Assets/.");
            if (string.IsNullOrEmpty(selectedPath))
            {
                return false;
            }

            selectedPath = EnsureGameEventExtension(selectedPath);
            if (AssetDatabase.LoadMainAssetAtPath(selectedPath) != null)
            {
                SetStatus(
                    $"A Game Event file already exists at '{selectedPath}'. " +
                    "Choose a new file name.",
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
                        $"Created Game Event could not be loaded: {selectedPath}");
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
                    $"Created, cataloged, and opened: {selectedPath}.",
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
                    $"Cannot create Game Event Catalog because '{catalogPath}' " +
                    "is already used by another asset type.");
            }

            eventCatalog = existingAsset as GameEventCatalogAsset;
            if (eventCatalog == null)
            {
                eventCatalog = CreateInstance<GameEventCatalogAsset>();
                AssetDatabase.CreateAsset(eventCatalog, catalogPath);
                AssetDatabase.SaveAssets();
            }

            GameEventEditorDataSource source = RequireDataSourceAndCatalog();
            source.SetEventCatalog(selectedDataCatalog, eventCatalog);
            return eventCatalog;
        }

        internal static string GetDefaultEventCatalogPath(string eventAssetPath)
        {
            string directory = Path.GetDirectoryName(eventAssetPath)
                ?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException(
                    "Game Event asset must have a parent directory.",
                    nameof(eventAssetPath));
            }

            return directory + "/GameEventCatalog.asset";
        }

        private void DrawDataCatalogSelector(GameEventEditorDataSource source)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                UnityEngine.Object selected = EditorGUILayout.ObjectField(
                    source.DisplayName,
                    selectedDataCatalog,
                    source.AssetType,
                    false);
                if (selected != selectedDataCatalog)
                {
                    SetDataCatalog(selected);
                }

                if (GUILayout.Button("Create New", GUILayout.Width(92f)))
                {
                    CreateDataCatalog(source);
                    GUIUtility.ExitGUI();
                }

                using (new EditorGUI.DisabledScope(selectedDataCatalog == null))
                {
                    if (GUILayout.Button("Ping", GUILayout.Width(48f)))
                    {
                        EditorGUIUtility.PingObject(selectedDataCatalog);
                    }
                }
            }
        }

        private void SetDataCatalog(UnityEngine.Object selected)
        {
            try
            {
                GameEventEditorDataSource source =
                    GameEventEditorCommandCatalog.GetDataSource();
                if (selected != null && (source == null || !source.IsValidAsset(selected)))
                {
                    throw new ArgumentException(
                        $"Data Catalog must be a {source?.AssetType.Name ?? "registered asset type"}.");
                }

                GameEventEditorProjectSettings.instance.SetDataCatalog(selected);
                selectedDataCatalog = selected;
                LoadParameterTableSettings();
                EventCatalogEditor.SetCatalog(GetSelectedEventCatalog());
                RefreshCatalog(false);
                SetStatus(
                    selected == null
                        ? "Data Catalog selection cleared."
                        : $"Data Catalog selected: {AssetDatabase.GetAssetPath(selected)}",
                    MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
            }
        }

        private void CreateDataCatalog(GameEventEditorDataSource source)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                $"Create {source.DisplayName}",
                "GameFlowDataCatalog",
                "asset",
                "Create the shared data catalog under Assets/.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                ScriptableObject created = CreateInstance(source.AssetType);
                AssetDatabase.CreateAsset(created, path);
                AssetDatabase.SaveAssets();
                SetDataCatalog(created);
                Selection.activeObject = created;
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
            }
        }

        private void CreateEventCatalog()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Game Event Catalog",
                "GameEventCatalog",
                "asset",
                "Create the runtime Game Event Catalog under Assets/.");
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
                document => $"Loaded from catalog: {document.DisplayName}.");
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
                    EditorGUILayout.TextField("Document GUID", session.DocumentGuid ?? string.Empty);
                }

                if (GUILayout.Button("Copy", GUILayout.Width(52f)))
                {
                    EditorGUIUtility.systemCopyBuffer = session.DocumentGuid ?? string.Empty;
                    SetStatus("Document GUID copied.", MessageType.Info);
                }

                if (GUILayout.Button("Regenerate", GUILayout.Width(82f)) &&
                    EditorUtility.DisplayDialog(
                        "Regenerate Document GUID?",
                        "References to the current GUID will no longer identify this document.",
                        "Regenerate",
                        "Cancel"))
                {
                    session.RegenerateDocumentGuid();
                    SetStatus("Document GUID regenerated.", MessageType.Warning);
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
                $"Authoring Parameter Tables ({selectedCount})",
                true);
            if (parameterTablesFoldout)
            {
                EditorGUILayout.HelpBox(
                    "Only the tables selected here are used for Condition and ParameterKey choices. " +
                    "Create a new table here or add an existing one. Click Edit to modify it; " +
                    "saving refreshes those choices immediately. " +
                    "The selection is stored in the shared Data Catalog.",
                    MessageType.Info);

                int removeIndex = -1;
                for (int index = 0; index < selectedParameterTables.Count; index++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        TextAsset current = selectedParameterTables[index];
                        TextAsset selected = (TextAsset)EditorGUILayout.ObjectField(
                            $"Table {index + 1}",
                            current,
                            typeof(TextAsset),
                            false);
                        if (selected != current)
                        {
                            TrySetParameterTable(index, selected);
                        }

                        using (new EditorGUI.DisabledScope(current == null))
                        {
                            if (GUILayout.Button("Edit", GUILayout.Width(48f)))
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

                using (new EditorGUI.DisabledScope(selectedDataCatalog == null))
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ Create New Table", GUILayout.Width(160f)))
                    {
                        CreateParameterTableFromDialog();
                    }

                    if (GUILayout.Button("+ Add Existing Table", GUILayout.Width(170f)))
                    {
                        selectedParameterTables.Add(null);
                    }
                }

                if (selectedCount == 0)
                {
                    EditorGUILayout.HelpBox(
                        selectedDataCatalog == null
                            ? "Select or create a Data Catalog before adding Parameter Tables."
                            : "No Parameter Tables selected. Project and sample tables are not loaded automatically.",
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
                "Create Parameter Table",
                "NewParameterTable.parameters",
                "json",
                "Create the Parameter Table JSON under Assets/.");
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
                        $"Created Parameter Table could not be loaded: {ParameterEditor.AssetPath}");
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
                    $"Created and opened Parameter Table: {ParameterEditor.AssetPath}",
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
                        $"'{path}' is not a .parameters.json asset.",
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
                            $"Parameter Table '{path}' is already selected.",
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
                SetStatus($"Editing Parameter Table: {path}", MessageType.Info);
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
                ? "Parameter Table Editor"
                : "Parameter Table Editor — " + ParameterEditor.TableDisplayName;
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
                        isDirty ? "Save Table *" : "Save Table",
                        isDirty
                            ? "This Parameter Table has unsaved changes."
                            : "Save this Parameter Table.");
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

                    if (GUILayout.Button("Reload Table", EditorStyles.toolbarButton) &&
                        ConfirmDiscardParameterTableChanges())
                    {
                        ReloadEmbeddedParameterTable();
                    }

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Close", EditorStyles.toolbarButton) &&
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
                    $"Saved Parameter Table and refreshed event choices: {path}",
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
                SetStatus("Parameter Table reloaded from disk.", MessageType.Info);
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
                "Unsaved Parameter Table",
                $"Discard unsaved changes to '{ParameterEditor.TableDisplayName}'?",
                "Discard",
                "Cancel");
        }

        private bool IsEditingParameterTable(TextAsset tableAsset)
        {
            return tableAsset != null &&
                string.Equals(
                    AssetDatabase.GetAssetPath(tableAsset),
                    ParameterEditor.AssetPath,
                    StringComparison.OrdinalIgnoreCase);
        }

        private void LoadDataCatalogSettings()
        {
            selectedDataCatalog =
                GameEventEditorProjectSettings.instance.LoadDataCatalog();
        }

        private GameEventCatalogAsset GetSelectedEventCatalog()
        {
            GameEventEditorDataSource source =
                GameEventEditorCommandCatalog.GetDataSource();
            return source != null && selectedDataCatalog != null
                ? source.GetEventCatalog(selectedDataCatalog)
                : null;
        }

        private GameEventEditorDataSource RequireDataSourceAndCatalog()
        {
            GameEventEditorDataSource source =
                GameEventEditorCommandCatalog.GetDataSource();
            if (source == null)
            {
                throw new InvalidOperationException(
                    "No Game Event Editor data source is registered.");
            }

            if (selectedDataCatalog == null)
            {
                throw new InvalidOperationException(
                    $"Select or create a {source.DisplayName} first.");
            }

            return source;
        }

        private void LoadParameterTableSettings()
        {
            GameEventEditorDataSource source =
                GameEventEditorCommandCatalog.GetDataSource();
            selectedParameterTables = source != null && selectedDataCatalog != null
                ? new List<TextAsset>(source.GetParameterTables(selectedDataCatalog))
                : new List<TextAsset>();
        }

        private void SaveParameterTableSettings()
        {
            try
            {
                GameEventEditorDataSource source = RequireDataSourceAndCatalog();
                source.SetParameterTables(
                    selectedDataCatalog,
                    selectedParameterTables.Where(table => table != null).ToArray());
                RefreshCatalog(true);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
            }
        }

        private void DrawTriggerTiming()
        {
            EnsureCatalog();
            List<string> values = new List<string> { string.Empty };
            values.AddRange(catalog.TriggerTimings.Where(value => !string.IsNullOrWhiteSpace(value)));
            if (!string.IsNullOrWhiteSpace(session.TriggerTiming) &&
                !values.Contains(session.TriggerTiming))
            {
                values.Add(session.TriggerTiming);
            }

            string[] labels = values
                .Select(value => string.IsNullOrEmpty(value)
                    ? "Direct Scene Trigger (no timing)"
                    : value)
                .ToArray();
            int selectedIndex = Math.Max(0, values.IndexOf(session.TriggerTiming ?? string.Empty));
            int newIndex = EditorGUILayout.Popup("Trigger Timing", selectedIndex, labels);
            session.TriggerTiming = values[newIndex];
        }

        private void DrawConditionEditor()
        {
            EnsureCatalog();
            if (conditionRoot == null)
            {
                RefreshConditionDrafts();
            }

            EditorGUILayout.LabelField("Condition", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(conditionEditorError))
            {
                EditorGUILayout.HelpBox(
                    "This event uses condition syntax that is not supported by the " +
                    "structured editor. The saved condition is preserved. Clear it to " +
                    "rebuild the condition as rows.",
                    MessageType.Error);
                if (GUILayout.Button("Clear and Rebuild", GUILayout.Width(130f)))
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
                    EditorGUILayout.LabelField("Always.", EditorStyles.miniLabel);
                    if (GUILayout.Button(
                            setupState == ConditionSetupState.SelectParameterTable
                                ? "Select a Parameter Table to Add Conditions"
                                : "Add a Bool or Number Parameter to Build Conditions",
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
                        "The condition references Parameters that are not available from " +
                        "the selected Parameter Tables.",
                        MessageType.Error);
                    if (GUILayout.Button("Clear Conditions", GUILayout.Width(130f)))
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
                        isRoot ? "Root Group" : "Condition Group",
                        EditorStyles.boldLabel,
                        GUILayout.Width(110f));
                    EditorGUI.BeginChangeCheck();
                    group.Mode = (GameEventConditionGroupMode)EditorGUILayout.Popup(
                        (int)group.Mode,
                        new[] { "Match All (AND)", "Match Any (OR)" });
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
                        EditorGUILayout.HelpBox("Always — no conditions.", MessageType.Info);
                    }
                    else
                    {
                        removeRequested = true;
                        return true;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ Condition", GUILayout.Width(100f)))
                    {
                        group.Children.Add(CreateDefaultCondition(parameters[0]));
                        changed = true;
                    }

                    if (GUILayout.Button("+ Group", GUILayout.Width(90f)))
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
                        $"Parameter '{draft.ParameterKey}' is not available.",
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
                labels = new[] { $"Missing / {currentValue}" }
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
                    "State",
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
                "Comparison",
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
                value = EditorGUILayout.IntField("Value", value);
                draft.Value = value.ToString(CultureInfo.InvariantCulture);
                return;
            }

            float.TryParse(
                draft.Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float floatValue);
            floatValue = EditorGUILayout.FloatField("Value", floatValue);
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
            EditorGUILayout.LabelField("Commands", EditorStyles.boldLabel);
            if (catalog.Commands.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No Commands are enabled in the selected Data Catalog.",
                    MessageType.Warning);
                if (GUILayout.Button("Configure Commands"))
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
                        EditorGUILayout.LabelField($"#{index + 1}", GUILayout.Width(28f));
                        DrawCommandSelector(draft);
                        using (new EditorGUI.DisabledScope(index == 0))
                        {
                            if (GUILayout.Button("↑", GUILayout.Width(28f)))
                            {
                                moveFrom = index;
                                moveTo = index - 1;
                            }
                        }

                        using (new EditorGUI.DisabledScope(index == commandDrafts.Count - 1))
                        {
                            if (GUILayout.Button("↓", GUILayout.Width(28f)))
                            {
                                moveFrom = index;
                                moveTo = index + 1;
                            }
                        }

                        if (GUILayout.Button("×", GUILayout.Width(28f)))
                        {
                            removeIndex = index;
                        }
                    }

                    if (catalog.TryGetCommand(draft.Name, out EffectCommandDescriptor descriptor))
                    {
                        EnsureArgumentCount(draft, descriptor);
                        for (int argumentIndex = 0;
                             argumentIndex < descriptor.Parameters.Count;
                             argumentIndex++)
                        {
                            DrawCommandArgument(draft, descriptor, argumentIndex);
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(draft.Name))
                    {
                        EditorGUILayout.HelpBox(
                            "Select a command to configure its parameters.",
                            MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            $"Command '{draft.Name}' is not registered. Select a replacement.",
                            MessageType.Error);
                    }
                }
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
            EditorGUILayout.LabelField("Serialized Preview", EditorStyles.miniBoldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextArea(session.Commands, GUILayout.MinHeight(56f));
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
                labels = new[] { "Select Command…" }
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
                labels = new[] { $"Missing / {draft.Name}" }
                    .Concat(options.Select(FormatCommandLabel))
                    .ToArray();
                selectedIndex = 0;
            }

            int newIndex = EditorGUILayout.Popup(selectedIndex, labels);
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
            string label = $"{parameter.Name} ({parameter.Kind})";
            if (parameter.Kind == EffectCommandParameterKind.ParameterKey)
            {
                draft.Arguments[argumentIndex] = DrawParameterKeyPopup(
                    label,
                    draft.Arguments[argumentIndex]);
                return;
            }

            if (descriptor.Name == "SetParameter" && argumentIndex == 1 &&
                draft.Arguments.Count > 0 &&
                catalog.TryGetParameter(draft.Arguments[0], out ParameterDefinition target) &&
                target.Type == ParameterType.Bool)
            {
                string[] values = { "true", "false" };
                int selected = string.Equals(
                    draft.Arguments[argumentIndex],
                    "false",
                    StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                draft.Arguments[argumentIndex] = values[
                    EditorGUILayout.Popup(label, selected, new[] { "True", "False" })];
                return;
            }

            draft.Arguments[argumentIndex] = DrawKnownValuePopup(
                label,
                draft.Arguments[argumentIndex],
                catalog.GetArgumentOptions(descriptor.Name, argumentIndex));
        }

        private string DrawParameterKeyPopup(string label, string currentValue)
        {
            if (catalog.Parameters.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No Parameter keys were found in the selected Parameter Tables.",
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
                labels = new[] { $"Missing / {currentValue}" }
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

        private static string DrawKnownValuePopup(
            string label,
            string currentValue,
            IReadOnlyList<string> knownValues)
        {
            List<string> values = knownValues
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            int knownIndex = values.IndexOf(currentValue ?? string.Empty);
            string[] labels = values.Concat(new[] { "<Custom value…>" }).ToArray();
            int customIndex = values.Count;
            int selectedIndex = knownIndex >= 0 ? knownIndex : customIndex;
            int newIndex = EditorGUILayout.Popup(label, selectedIndex, labels);
            if (newIndex < customIndex)
            {
                return values[newIndex];
            }

            return EditorGUILayout.TextField("Custom", currentValue ?? string.Empty);
        }

        private void DrawAddCommand()
        {
            if (GUILayout.Button("Add Command"))
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

                IReadOnlyList<string> knownValues = catalog.GetArgumentOptions(
                    descriptor.Name,
                    index);
                draft.Arguments.Add(knownValues.Count > 0 ? knownValues[0] : string.Empty);
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
                "Load Game Event",
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
                document => $"Loaded: {document.DisplayName}.");
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
                SetStatus("No Game Event file is open.", MessageType.Error);
                return false;
            }

            bool saved = RunCommand(
                () =>
                {
                    ValidateEditorDocument();
                    session.SaveDocument(session.AssetPath);
                    return session.ValidateDocument();
                },
                document => $"Saved: {document.DisplayName}.");
            if (saved)
            {
                EventCatalogEditor.Refresh();
            }

            return saved;
        }

        private bool SaveAsFromDialog()
        {
            string selectedPath = EditorUtility.SaveFilePanelInProject(
                "Save Game Event",
                GetDefaultFileName(),
                "json",
                "Save the canonical Game Event JSON under Assets/.");
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
                document => $"Saved: {document.DisplayName}.");
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
                "Unsaved Game Event",
                "Discard the current unsaved changes?",
                "Discard",
                "Cancel");
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
                        $"Command row {index + 1} has no command selected.");
                }

                if (!catalog.TryGetCommand(draft.Name, out EffectCommandDescriptor descriptor))
                {
                    throw new InvalidOperationException(
                        $"Command row {index + 1} uses unregistered command '{draft.Name}'.");
                }

                if (draft.Arguments.Count != descriptor.Parameters.Count)
                {
                    throw new InvalidOperationException(
                        $"Command '{draft.Name}' expects {descriptor.Parameters.Count} arguments.");
                }

                for (int argumentIndex = 0;
                     argumentIndex < descriptor.Parameters.Count;
                     argumentIndex++)
                {
                    if (string.IsNullOrWhiteSpace(draft.Arguments[argumentIndex]))
                    {
                        throw new InvalidOperationException(
                            $"Command '{draft.Name}' parameter " +
                            $"'{descriptor.Parameters[argumentIndex].Name}' requires a value.");
                    }

                    if (descriptor.Parameters[argumentIndex].Kind ==
                            EffectCommandParameterKind.ParameterKey &&
                        !catalog.TryGetParameter(
                            draft.Arguments[argumentIndex],
                            out ParameterDefinition _))
                    {
                        throw new InvalidOperationException(
                            $"Command '{draft.Name}' references unknown Parameter " +
                            $"'{draft.Arguments[argumentIndex]}'.");
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
                        "Condition is invalid: " + conditionResult.Error);
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
                selectedParameterTables.Where(table => table != null).ToArray(),
                GetSelectedEventCatalog(),
                selectedDataCatalog);
            if (!showStatus)
            {
                return;
            }

            string message =
                $"Choices refreshed: {catalog.TriggerTimings.Count} timings, " +
                $"{catalog.Parameters.Count} parameters, {catalog.Commands.Count} commands.";
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
                SetStatus("Cannot open Commands in structured editor: " + exception.Message, MessageType.Error);
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
                    "Cannot open Condition in structured editor: " + exception.Message,
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
            return $"{displayName}  (${parameter.Key}, {parameter.Type})";
        }

        private static string FormatCommandLabel(EffectCommandDescriptor descriptor)
        {
            return string.IsNullOrWhiteSpace(descriptor.Category)
                ? descriptor.DisplayName
                : descriptor.Category + " / " + descriptor.DisplayName;
        }
    }
}
