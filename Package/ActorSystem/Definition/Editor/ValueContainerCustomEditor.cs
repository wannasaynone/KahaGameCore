using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Package.ActorSystem.Definition.Editor
{
    /// <summary>
    /// Custom inspector for ValueContainer that allows runtime editing
    /// </summary>
    [CustomEditor(typeof(Instance))]
    public class ValueContainerCustomEditor : UnityEditor.Editor
    {
        private ValueContainerInspectorData.InspectorState state = new ValueContainerInspectorData.InspectorState();
        private Vector2 scrollPosition;
        private bool initialized = false;

        private void OnEnable()
        {
            // Initialize styles
            if (!initialized)
            {
                ValueContainerInspectorStyles.InitStyles();
                initialized = true;
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Draw the default inspector in edit mode
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to edit ValueContainer values.", MessageType.Info);
                return;
            }

            Instance container = (Instance)target;
            string containerKey = container.GetInstanceID().ToString();

            // Draw the toolbar (simplified for inspector)
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Auto-refresh toggle
            state.autoRefresh = EditorGUILayout.ToggleLeft("Auto Refresh", state.autoRefresh, GUILayout.Width(100));

            // Manual refresh button
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            // Begin scrollable area
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Draw the three main sections
            ValueContainerInspectorBaseValuesDrawer.DrawBaseValuesSection(container, containerKey, state);
            ValueContainerInspectorTempValuesDrawer.DrawTempValuesSection(container, containerKey, state);
            ValueContainerInspectorStringValuesDrawer.DrawStringValuesSection(container, containerKey, state);

            EditorGUILayout.EndScrollView();

            // Auto-repaint if auto-refresh is enabled
            if (state.autoRefresh)
            {
                Repaint();
            }
        }
    }
}
