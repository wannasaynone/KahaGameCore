using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Package.ActorSystem.Definition.Editor
{
    /// <summary>
    /// Handles drawing the base values section of the ValueContainer inspector
    /// </summary>
    public static class ValueContainerInspectorBaseValuesDrawer
    {
        /// <summary>
        /// Draw the base values section
        /// </summary>
        public static void DrawBaseValuesSection(
            Instance container,
            string containerKey,
            ValueContainerInspectorData.InspectorState state)
        {
            // Get base values using reflection
            Dictionary<string, int> baseValues = ValueContainerInspectorUtility.GetBaseValues(container);

            string sectionKey = $"{containerKey}_base";
            if (!state.baseValuesFoldouts.ContainsKey(sectionKey))
            {
                state.baseValuesFoldouts[sectionKey] = true;
            }

            EditorGUILayout.BeginVertical(ValueContainerInspectorStyles.SectionStyle);

            state.baseValuesFoldouts[sectionKey] = EditorGUILayout.Foldout(
                state.baseValuesFoldouts[sectionKey],
                $"Base Values ({baseValues.Count})",
                true,
                ValueContainerInspectorStyles.SubHeaderStyle);

            if (state.baseValuesFoldouts[sectionKey])
            {
                if (baseValues.Count == 0)
                {
                    EditorGUILayout.LabelField("No base values", EditorStyles.boldLabel);
                }
                else
                {
                    DrawBaseValuesTable(container, baseValues, containerKey, state);
                }

                EditorGUILayout.Space(5);

                DrawAddNewBaseValueSection(container, state);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw the table of base values
        /// </summary>
        private static void DrawBaseValuesTable(
            Instance container,
            Dictionary<string, int> baseValues,
            string containerKey,
            ValueContainerInspectorData.InspectorState state)
        {
            // Table header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tag", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("Value", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            // Draw each base value
            List<string> keysToRemove = new List<string>();
            foreach (var kvp in baseValues)
            {
                EditorGUILayout.BeginHorizontal();

                // Tag (read-only)
                EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(150));

                // Value (editable)
                bool hasChanged = state.previousBaseValues.ContainsKey(containerKey) &&
                                 state.previousBaseValues[containerKey].ContainsKey(kvp.Key) &&
                                 state.previousBaseValues[containerKey][kvp.Key] != kvp.Value;

                GUIStyle valueStyle = hasChanged ? ValueContainerInspectorStyles.ValueChangedStyle : EditorStyles.label;
                int newValue = EditorGUILayout.IntField(kvp.Value, valueStyle, GUILayout.Width(100));

                if (newValue != kvp.Value)
                {
                    container.SetBase(kvp.Key, newValue);
                }

                // Remove button
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    keysToRemove.Add(kvp.Key);
                }

                EditorGUILayout.EndHorizontal();
            }

            // Remove any base values marked for deletion
            foreach (string key in keysToRemove)
            {
                container.SetBase(key, 0);
            }
        }

        /// <summary>
        /// Draw the section for adding a new base value
        /// </summary>
        private static void DrawAddNewBaseValueSection(
            Instance container,
            ValueContainerInspectorData.InspectorState state)
        {
            EditorGUILayout.LabelField("Add New Base Value", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            state.newBaseValueTag = EditorGUILayout.TextField("Tag", state.newBaseValueTag);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            state.newBaseValueAmount = EditorGUILayout.IntField("Value", state.newBaseValueAmount);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !string.IsNullOrEmpty(state.newBaseValueTag);
            if (GUILayout.Button("Add Base Value", GUILayout.Width(120)))
            {
                container.SetBase(state.newBaseValueTag, state.newBaseValueAmount);
                state.newBaseValueTag = "";
                state.newBaseValueAmount = 0;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
    }
}
