using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    internal readonly struct GameEventTriggerOption
    {
        public GameEventTriggerOption(TextAsset asset, string label, string category = null)
        {
            Asset = asset;
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Category = category?.Trim() ?? string.Empty;
        }

        public TextAsset Asset { get; }
        public string Label { get; }
        public string Category { get; }
    }

    internal sealed class GameEventAssetAdvancedDropdown : AdvancedDropdown
    {
        private sealed class OptionItem : AdvancedDropdownItem
        {
            public OptionItem(GameEventTriggerOption option) : base(option.Label)
            {
                Option = option;
            }

            public GameEventTriggerOption Option { get; }
        }

        private readonly IReadOnlyList<GameEventTriggerOption> options;
        private readonly Action<TextAsset> onSelected;

        public GameEventAssetAdvancedDropdown(
            AdvancedDropdownState state,
            IReadOnlyList<GameEventTriggerOption> options,
            Action<TextAsset> onSelected)
            : base(state)
        {
            this.options = options ?? Array.Empty<GameEventTriggerOption>();
            this.onSelected = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
            minimumSize = new Vector2(420f, 320f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("選擇 Game Event");
            var groups = new Dictionary<string, AdvancedDropdownItem>(
                StringComparer.Ordinal);
            foreach (GameEventTriggerOption option in options
                         .OrderBy(item => item.Category, StringComparer.Ordinal)
                         .ThenBy(item => item.Label, StringComparer.Ordinal))
            {
                AdvancedDropdownItem parent = root;
                if (!string.IsNullOrWhiteSpace(option.Category))
                {
                    if (!groups.TryGetValue(
                            option.Category,
                            out AdvancedDropdownItem group))
                    {
                        group = new AdvancedDropdownItem(option.Category);
                        groups.Add(option.Category, group);
                        root.AddChild(group);
                    }

                    parent = group;
                }

                parent.AddChild(new OptionItem(option));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is OptionItem optionItem)
            {
                onSelected(optionItem.Option.Asset);
            }
        }
    }

    internal abstract class SceneGameEventTriggerEditorBase : UnityEditor.Editor
    {
        internal const string EmptyTriggerLayerMessage =
            "現在沒有任何物件可以觸發本trigger";
        internal const string EmptyGameEventMessage =
            "這個觸發器還未選擇任何事件觸發";
        internal const string MissingCatalogMessage =
            "請先在 Game Event Editor 選擇事件目錄。";
        internal const string EventOutsideCatalogMessage =
            "目前指定的 Game Event File 不在 Game Event Editor 的事件清單中。";

        private static readonly GameEventDocumentJsonCodec EventCodec =
            new GameEventDocumentJsonCodec();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("m_Script"));
            }

            SerializedProperty gameEventFile =
                serializedObject.FindProperty("gameEventFile");
            GameEventCatalogAsset catalog =
                GameEventEditorProjectSettings.instance.LoadEventCatalog();
            DrawGameEventSelector(gameEventFile, catalog);

            SerializedProperty triggeringLayers =
                serializedObject.FindProperty("triggeringLayers");
            EditorGUILayout.PropertyField(triggeringLayers);
            if (ShouldShowEmptyTriggerLayerError(triggeringLayers))
            {
                EditorGUILayout.HelpBox(EmptyTriggerLayerMessage, MessageType.Error);
            }

            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "gameEventFile",
                "triggeringLayers");
            serializedObject.ApplyModifiedProperties();
        }

        internal static void DrawGameEventSelector(
            SerializedProperty gameEventFile,
            GameEventCatalogAsset catalog)
        {
            TextAsset current = gameEventFile.hasMultipleDifferentValues
                ? null
                : gameEventFile.objectReferenceValue as TextAsset;
            IReadOnlyList<GameEventTriggerOption> options =
                BuildGameEventOptions(catalog, current);
            string currentLabel = "未選擇";
            for (int index = 0; index < options.Count; index++)
            {
                if (options[index].Asset == current)
                {
                    currentLabel = options[index].Label;
                    break;
                }
            }

            bool previousShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = gameEventFile.hasMultipleDifferentValues;
            using (new EditorGUILayout.HorizontalScope())
            {
                Rect controlRect = EditorGUILayout.GetControlRect();
                Rect buttonRect = EditorGUI.PrefixLabel(
                    controlRect,
                    new GUIContent(
                        "Game Event File",
                        "Select from the event list in Game Event Editor."));
                if (EditorGUI.DropdownButton(
                        buttonRect,
                        new GUIContent(currentLabel),
                        FocusType.Keyboard))
                {
                    var dropdown = new GameEventAssetAdvancedDropdown(
                        new AdvancedDropdownState(),
                        options,
                        selected =>
                        {
                            gameEventFile.objectReferenceValue = selected;
                            gameEventFile.serializedObject.ApplyModifiedProperties();
                        });
                    dropdown.Show(buttonRect);
                }

                using (new EditorGUI.DisabledScope(
                           gameEventFile.hasMultipleDifferentValues || current == null))
                {
                    if (GUILayout.Button(
                            new GUIContent(
                                "前往",
                                "在遊戲事件編輯器中開啟此事件。"),
                            GUILayout.Width(48f)))
                    {
                        GameEventDocumentEditorWindow.OpenWindow(current);
                    }
                }
            }
            EditorGUI.showMixedValue = previousShowMixedValue;

            if (ShouldShowEmptyGameEventError(gameEventFile))
            {
                EditorGUILayout.HelpBox(EmptyGameEventMessage, MessageType.Error);
            }

            if (catalog == null)
            {
                EditorGUILayout.HelpBox(MissingCatalogMessage, MessageType.Error);
            }
            else if (current != null && !ContainsEvent(catalog, current))
            {
                EditorGUILayout.HelpBox(EventOutsideCatalogMessage, MessageType.Error);
            }
            else if (catalog.Files.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Game Event Editor 的事件清單目前是空的。",
                    MessageType.Info);
            }
        }

        internal static IReadOnlyList<GameEventTriggerOption> BuildGameEventOptions(
            GameEventCatalogAsset catalog,
            TextAsset current)
        {
            List<GameEventTriggerOption> options =
                new List<GameEventTriggerOption>
                {
                    new GameEventTriggerOption(null, "未選擇")
                };

            if (catalog != null)
            {
                for (int index = 0; index < catalog.Files.Count; index++)
                {
                    TextAsset asset = catalog.Files[index];
                    if (asset != null)
                    {
                        options.Add(new GameEventTriggerOption(
                            asset,
                            FormatEventLabel(asset),
                            GameEventEditorAssetUtility.GetCategory(asset)));
                    }
                }
            }

            if (current != null && !ContainsEvent(catalog, current))
            {
                options.Add(new GameEventTriggerOption(
                    current,
                    $"{current.name}（不在目前事件清單）"));
            }

            return options;
        }

        internal static string FormatEventLabel(TextAsset asset)
        {
            try
            {
                GameEventDocument document = EventCodec.Read(asset.text);
                return $"{document.DisplayName} ({asset.name})";
            }
            catch (Exception)
            {
                return $"{asset.name}（事件格式無效）";
            }
        }

        private static bool ContainsEvent(
            GameEventCatalogAsset catalog,
            TextAsset asset)
        {
            if (catalog == null || asset == null)
            {
                return false;
            }

            for (int index = 0; index < catalog.Files.Count; index++)
            {
                if (catalog.Files[index] == asset)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool ShouldShowEmptyTriggerLayerError(
            SerializedProperty layerProperty)
        {
            return layerProperty != null &&
                   !layerProperty.hasMultipleDifferentValues &&
                   layerProperty.intValue == 0;
        }

        internal static bool ShouldShowEmptyGameEventError(
            SerializedProperty gameEventProperty)
        {
            return gameEventProperty != null &&
                   !gameEventProperty.hasMultipleDifferentValues &&
                   gameEventProperty.objectReferenceValue == null;
        }
    }

    [CustomEditor(typeof(SceneGameEventTrigger))]
    [CanEditMultipleObjects]
    internal sealed class SceneGameEventTriggerEditor :
        SceneGameEventTriggerEditorBase
    {
    }

    [CustomEditor(typeof(SceneGameEventTrigger2D))]
    [CanEditMultipleObjects]
    internal sealed class SceneGameEventTrigger2DEditor :
        SceneGameEventTriggerEditorBase
    {
    }

    [CustomEditor(typeof(StartGameEventTrigger))]
    [CanEditMultipleObjects]
    internal sealed class StartGameEventTriggerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("m_Script"));
            }

            SceneGameEventTriggerEditorBase.DrawGameEventSelector(
                serializedObject.FindProperty("gameEventFile"),
                GameEventEditorProjectSettings.instance.LoadEventCatalog());
            EditorGUILayout.HelpBox(
                "Parameter Binder 初始化後，只有仍 active 的物件會在場景開始時執行此事件。",
                MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
