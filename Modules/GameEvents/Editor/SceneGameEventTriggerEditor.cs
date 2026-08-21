using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    internal readonly struct GameEventTriggerOption
    {
        public GameEventTriggerOption(TextAsset asset, string label)
        {
            Asset = asset;
            Label = label ?? throw new ArgumentNullException(nameof(label));
        }

        public TextAsset Asset { get; }
        public string Label { get; }
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

        private static void DrawGameEventSelector(
            SerializedProperty gameEventFile,
            GameEventCatalogAsset catalog)
        {
            TextAsset current = gameEventFile.hasMultipleDifferentValues
                ? null
                : gameEventFile.objectReferenceValue as TextAsset;
            IReadOnlyList<GameEventTriggerOption> options =
                BuildGameEventOptions(catalog, current);
            string[] labels = new string[options.Count];
            int currentIndex = 0;
            for (int index = 0; index < options.Count; index++)
            {
                labels[index] = options[index].Label;
                if (options[index].Asset == current)
                {
                    currentIndex = index;
                }
            }

            bool previousShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = gameEventFile.hasMultipleDifferentValues;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                int selectedIndex = EditorGUILayout.Popup(
                    new GUIContent(
                        "Game Event File",
                        "Select from the event list in Game Event Editor."),
                    currentIndex,
                    labels);
                bool changed = EditorGUI.EndChangeCheck();

                if (changed && selectedIndex >= 0 && selectedIndex < options.Count)
                {
                    current = options[selectedIndex].Asset;
                    gameEventFile.objectReferenceValue = current;
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
                            FormatEventLabel(asset)));
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

        private static string FormatEventLabel(TextAsset asset)
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
}
