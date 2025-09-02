using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Package.ActorSystem.Definition.Editor
{
    /// <summary>
    /// Handles drawing the toolbar for the ValueContainer inspector
    /// </summary>
    public static class ValueContainerInspectorToolbar
    {
        /// <summary>
        /// Draw the toolbar for the inspector
        /// </summary>
        public static void DrawToolbar(ValueContainerInspectorData.InspectorState state)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            DrawAutoRefreshControls(state);

            GUILayout.FlexibleSpace();

            DrawSearchControls(state);

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw the auto-refresh controls
        /// </summary>
        private static void DrawAutoRefreshControls(ValueContainerInspectorData.InspectorState state)
        {
            // Auto-refresh toggle
            bool newAutoRefresh = EditorGUILayout.ToggleLeft("Auto Refresh", state.autoRefresh, GUILayout.Width(100));
            if (newAutoRefresh != state.autoRefresh)
            {
                state.autoRefresh = newAutoRefresh;
                if (state.autoRefresh)
                {
                    state.lastRefreshTime = Time.realtimeSinceStartup - state.refreshInterval; // Force immediate refresh
                }
            }

            // Refresh interval slider (only show if auto-refresh is enabled)
            if (state.autoRefresh)
            {
                EditorGUILayout.LabelField("Interval:", GUILayout.Width(60));
                state.refreshInterval = EditorGUILayout.Slider(state.refreshInterval, 0.1f, 2f, GUILayout.Width(150));
            }

            // Manual refresh button
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                // The window will be repainted after this
            }
        }

        /// <summary>
        /// Draw the search controls
        /// </summary>
        private static void DrawSearchControls(ValueContainerInspectorData.InspectorState state)
        {
            // Search field
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            state.searchFilter = EditorGUILayout.TextField(state.searchFilter, GUILayout.Width(150));

            // Clear search button
            if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)) && !string.IsNullOrEmpty(state.searchFilter))
            {
                state.searchFilter = "";
                GUI.FocusControl(null);
            }
        }
    }
}
