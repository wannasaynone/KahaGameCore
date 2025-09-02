using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Package.ActorSystem.Definition.Editor
{
    /// <summary>
    /// Handles drawing the string values section of the ValueContainer inspector
    /// </summary>
    public static class ValueContainerInspectorStringValuesDrawer
    {
        /// <summary>
        /// Draw the string values section
        /// </summary>
        public static void DrawStringValuesSection(
            Instance container,
            string containerKey,
            ValueContainerInspectorData.InspectorState state)
        {
            // Get string values using reflection
            Dictionary<string, string> stringValues = ValueContainerInspectorUtility.GetStringKeyValues(container);

            string sectionKey = $"{containerKey}_string";
            if (!state.stringValuesFoldouts.ContainsKey(sectionKey))
            {
                state.stringValuesFoldouts[sectionKey] = true;
            }

            EditorGUILayout.BeginVertical(ValueContainerInspectorStyles.SectionStyle);

            state.stringValuesFoldouts[sectionKey] = EditorGUILayout.Foldout(
                state.stringValuesFoldouts[sectionKey],
                $"String Key-Value Pairs ({stringValues.Count})",
                true,
                ValueContainerInspectorStyles.SubHeaderStyle);

            if (state.stringValuesFoldouts[sectionKey])
            {
                if (stringValues.Count == 0)
                {
                    EditorGUILayout.LabelField("No string values", EditorStyles.boldLabel);
                }
                else
                {
                    DrawStringValuesTable(container, stringValues, containerKey, state);
                }

                EditorGUILayout.Space(5);

                DrawAddNewStringValueSection(container, state);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw the table of string values
        /// </summary>
        private static void DrawStringValuesTable(
            Instance container,
            Dictionary<string, string> stringValues,
            string containerKey,
            ValueContainerInspectorData.InspectorState state)
        {
            // Table header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Key", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("Value", EditorStyles.boldLabel, GUILayout.Width(200));
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            // Draw each string value
            List<string> keysToRemove = new List<string>();
            foreach (var kvp in stringValues)
            {
                EditorGUILayout.BeginHorizontal();

                // Key (read-only)
                EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(150));

                // Value (editable)
                bool hasChanged = state.previousStringValues.ContainsKey(containerKey) &&
                                 state.previousStringValues[containerKey].ContainsKey(kvp.Key) &&
                                 state.previousStringValues[containerKey][kvp.Key] != kvp.Value;

                GUIStyle valueStyle = hasChanged ? ValueContainerInspectorStyles.ValueChangedStyle : EditorStyles.label;
                string newValue = EditorGUILayout.TextField(kvp.Value ?? "", valueStyle, GUILayout.Width(200));

                if (newValue != (kvp.Value ?? ""))
                {
                    container.SetStringKeyValue(kvp.Key, newValue);
                }

                // Remove button
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    keysToRemove.Add(kvp.Key);
                }

                EditorGUILayout.EndHorizontal();
            }

            // Remove any string values marked for deletion
            foreach (string key in keysToRemove)
            {
                container.RemoveStringKeyValue(key);
            }
        }

        /// <summary>
        /// Draw the section for adding a new string value
        /// </summary>
        private static void DrawAddNewStringValueSection(
            Instance container,
            ValueContainerInspectorData.InspectorState state)
        {
            EditorGUILayout.LabelField("Add New String Value", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            state.newStringKey = EditorGUILayout.TextField("Key", state.newStringKey);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            state.newStringValue = EditorGUILayout.TextField("Value", state.newStringValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !string.IsNullOrEmpty(state.newStringKey);
            if (GUILayout.Button("Add String Value", GUILayout.Width(120)))
            {
                container.SetStringKeyValue(state.newStringKey, state.newStringValue);
                state.newStringKey = "";
                state.newStringValue = "";
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
    }
}
