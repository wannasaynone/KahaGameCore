using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Parameters.Editor
{
    public sealed class RuntimeParameterMonitorWindow : EditorWindow
    {
        private const string ParametersDocumentationPath =
            "Assets/KahaGameCore/Modules/Parameters/README.md";

        [SerializeField] private ParameterRuntimeSource source;
        [SerializeField] private string searchText = string.Empty;
        [SerializeField] private Vector2 scrollPosition;

        [MenuItem("KahaGameCore/Parameters/Runtime Parameter Monitor")]
        public static void OpenWindow()
        {
            RuntimeParameterMonitorWindow window =
                GetWindow<RuntimeParameterMonitorWindow>();
            window.titleContent = new GUIContent("Runtime Parameters");
            window.minSize = new Vector2(560f, 320f);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Runtime Parameters");
            minSize = new Vector2(560f, 320f);
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Runtime Parameters", EditorStyles.boldLabel);
            if (source == null)
            {
                source = UnityEngine.Object.FindFirstObjectByType<ParameterRuntimeSource>(
                    FindObjectsInactive.Include);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                source = (ParameterRuntimeSource)EditorGUILayout.ObjectField(
                    "Source",
                    source,
                    typeof(ParameterRuntimeSource),
                    true);
                using (new EditorGUI.DisabledScope(source == null))
                {
                    if (GUILayout.Button("Ping", GUILayout.Width(52f)))
                    {
                        EditorGUIUtility.PingObject(source);
                    }
                }
            }

            if (source == null)
            {
                DrawMissingSourceHelp();
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to inspect the live ParameterStore exposed by this source.",
                    MessageType.Info);
                DrawDocumentationButton();
                return;
            }

            if (!source.IsInitialized)
            {
                EditorGUILayout.HelpBox(
                    "This ParameterRuntimeSource is not initialized. After creating the live " +
                    "ParameterStore, call source.Initialize(parameterStore) from the composition root.",
                    MessageType.Warning);
                DrawDocumentationButton();
                return;
            }

            IReadOnlyList<ParameterRuntimeValue> allValues =
                source.CaptureCurrentValues();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                searchText = GUILayout.TextField(
                    searchText ?? string.Empty,
                    EditorStyles.toolbarSearchField);
                GUILayout.Label("Count", GUILayout.Width(36f));
                GUILayout.Label(
                    allValues.Count.ToString(CultureInfo.InvariantCulture),
                    GUILayout.Width(40f));
            }

            IEnumerable<ParameterRuntimeValue> visibleValues = allValues;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                visibleValues = visibleValues.Where(item =>
                    ContainsIgnoreCase(item.Definition.Key, searchText) ||
                    ContainsIgnoreCase(item.Definition.DisplayName, searchText));
            }

            DrawHeader();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (ParameterRuntimeValue item in visibleValues)
            {
                DrawRow(item);
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawMissingSourceHelp()
        {
            EditorGUILayout.HelpBox(
                "No ParameterRuntimeSource was found in the loaded scenes.\n\n" +
                "Setup:\n" +
                "1. Create a concrete MonoBehaviour derived from ParameterRuntimeSource.\n" +
                "2. Attach it to a scene GameObject.\n" +
                "3. After creating the live ParameterStore, call " +
                "source.Initialize(parameterStore) from the composition root.",
                MessageType.Warning);
            DrawDocumentationButton();
        }

        private static void DrawDocumentationButton()
        {
            if (!GUILayout.Button("Open Parameters Setup Documentation"))
            {
                return;
            }

            UnityEngine.Object documentation = LoadDocumentationAsset();
            if (documentation == null)
            {
                Debug.LogError(
                    $"Parameters documentation was not found at '{ParametersDocumentationPath}'.");
                return;
            }

            EditorGUIUtility.PingObject(documentation);
            AssetDatabase.OpenAsset(documentation);
        }

        private static UnityEngine.Object LoadDocumentationAsset()
        {
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                ParametersDocumentationPath);
        }

        private static void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Key", EditorStyles.miniBoldLabel, GUILayout.Width(180f));
                GUILayout.Label("Display Name", EditorStyles.miniBoldLabel, GUILayout.Width(160f));
                GUILayout.Label("Type", EditorStyles.miniBoldLabel, GUILayout.Width(70f));
                GUILayout.Label("Current Value", EditorStyles.miniBoldLabel);
            }
        }

        private static void DrawRow(ParameterRuntimeValue item)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.SelectableLabel(
                    item.Definition.Key,
                    EditorStyles.label,
                    GUILayout.Width(180f),
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.LabelField(
                    item.Definition.DisplayName ?? string.Empty,
                    GUILayout.Width(160f));
                EditorGUILayout.LabelField(
                    item.Definition.Type.ToString(),
                    GUILayout.Width(70f));
                EditorGUILayout.SelectableLabel(
                    FormatValue(item.Value),
                    EditorStyles.label,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }

        internal static string FormatValue(ParameterValue value)
        {
            switch (value.Type)
            {
                case ParameterType.Int:
                    return value.AsInt().ToString(CultureInfo.InvariantCulture);
                case ParameterType.Float:
                    return value.AsFloat().ToString("0.######", CultureInfo.InvariantCulture);
                case ParameterType.Bool:
                    return value.AsBool() ? "True" : "False";
                case ParameterType.String:
                    return value.AsString() ?? string.Empty;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            return !string.IsNullOrEmpty(source) &&
                   source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
