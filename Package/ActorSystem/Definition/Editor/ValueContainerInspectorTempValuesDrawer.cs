using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Package.ActorSystem.Definition.Editor
{
    /// <summary>
    /// Handles drawing the temporary values section of the ValueContainer inspector
    /// </summary>
    public static class ValueContainerInspectorTempValuesDrawer
    {
        /// <summary>
        /// Draw the temporary values section
        /// </summary>
        public static void DrawTempValuesSection(
            Instance container,
            string containerKey,
            ValueContainerInspectorData.InspectorState state)
        {
            // Get temp values using reflection
            List<ValueContainerInspectorData.TempValueData> tempValues = ValueContainerInspectorUtility.GetTempValues(container);

            string sectionKey = $"{containerKey}_temp";
            if (!state.tempValuesFoldouts.ContainsKey(sectionKey))
            {
                state.tempValuesFoldouts[sectionKey] = true;
            }

            EditorGUILayout.BeginVertical(ValueContainerInspectorStyles.SectionStyle);

            state.tempValuesFoldouts[sectionKey] = EditorGUILayout.Foldout(
                state.tempValuesFoldouts[sectionKey],
                $"Temporary Values ({tempValues.Count})",
                true,
                ValueContainerInspectorStyles.SubHeaderStyle);

            if (state.tempValuesFoldouts[sectionKey])
            {
                if (tempValues.Count == 0)
                {
                    EditorGUILayout.LabelField("No temporary values", EditorStyles.boldLabel);
                }
                else
                {
                    DrawTempValuesTable(container, tempValues, containerKey, state);
                }

                EditorGUILayout.Space(5);

                DrawAddNewTempValueSection(container, state);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw the table of temporary values
        /// </summary>
        private static void DrawTempValuesTable(
            Instance container,
            List<ValueContainerInspectorData.TempValueData> tempValues,
            string containerKey,
            ValueContainerInspectorData.InspectorState state)
        {
            // Table header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tag", EditorStyles.boldLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField("Value", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("GUID", EditorStyles.boldLabel, GUILayout.Width(200));
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            // Draw each temp value
            List<Guid> guidsToRemove = new List<Guid>();
            foreach (var tempValue in tempValues)
            {
                EditorGUILayout.BeginHorizontal();

                // Tag (read-only)
                EditorGUILayout.LabelField(tempValue.tag, GUILayout.Width(120));

                // Value (editable)
                bool hasChanged = false;
                if (state.previousTempValues.ContainsKey(containerKey))
                {
                    var prevTempValue = state.previousTempValues[containerKey].FirstOrDefault(tv => tv.guid == tempValue.guid);
                    hasChanged = prevTempValue != null && prevTempValue.value != tempValue.value;
                }

                GUIStyle valueStyle = hasChanged ? ValueContainerInspectorStyles.ValueChangedStyle : EditorStyles.label;
                int newValue = EditorGUILayout.IntField(tempValue.value, valueStyle, GUILayout.Width(80));

                if (newValue != tempValue.value)
                {
                    container.SetTemp(tempValue.guid, newValue);
                }

                // GUID (read-only)
                EditorGUILayout.LabelField(tempValue.guid.ToString(), ValueContainerInspectorStyles.GuidStyle, GUILayout.Width(200));

                // Remove button
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    guidsToRemove.Add(tempValue.guid);
                }

                // Copy GUID button
                if (GUILayout.Button("Copy GUID", GUILayout.Width(80)))
                {
                    EditorGUIUtility.systemCopyBuffer = tempValue.guid.ToString();
                }

                EditorGUILayout.EndHorizontal();
            }

            // Remove any temp values marked for deletion
            foreach (Guid guid in guidsToRemove)
            {
                container.Remove(guid);
            }
        }

        /// <summary>
        /// Draw the section for adding a new temporary value
        /// </summary>
        private static void DrawAddNewTempValueSection(
            Instance container,
            ValueContainerInspectorData.InspectorState state)
        {
            EditorGUILayout.LabelField("Add New Temporary Value", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            state.newTempValueTag = EditorGUILayout.TextField("Tag", state.newTempValueTag);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            state.newTempValueAmount = EditorGUILayout.IntField("Value", state.newTempValueAmount);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !string.IsNullOrEmpty(state.newTempValueTag);
            if (GUILayout.Button("Add Temp Value", GUILayout.Width(120)))
            {
                container.Add(state.newTempValueTag, state.newTempValueAmount);
                state.newTempValueTag = "";
                state.newTempValueAmount = 0;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
    }
}
