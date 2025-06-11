using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Assets.Scripts.StateMachine.Editor
{
    /// <summary>
    /// 負責繪製轉換檢查器面板
    /// </summary>
    public class TransitionInspector
    {
        private StateMachineEditorData editorData;
        private StateMachineAssetManager assetManager;

        public TransitionInspector(StateMachineEditorData data, StateMachineAssetManager manager)
        {
            editorData = data;
            assetManager = manager;
        }

        public void DrawTransitionInspector()
        {
            if (editorData.SelectedTransition == null) return;

            TransitionDefinition selectedTransition = editorData.SelectedTransition;

            EditorGUILayout.LabelField("Selected Transition", EditorStyles.boldLabel);

            // 目標狀態選擇
            DrawTargetStateSelection(selectedTransition);

            // 線條顏色
            selectedTransition.lineColor = EditorGUILayout.ColorField("Line Color", selectedTransition.lineColor);

            EditorGUILayout.Space();

            // 條件列表
            DrawConditionsSection(selectedTransition);
        }

        private void DrawTargetStateSelection(TransitionDefinition selectedTransition)
        {
            List<string> stateNames = new List<string>();
            List<string> stateIDs = new List<string>();

            foreach (var state in editorData.CurrentStateMachine.states)
            {
                if (state != null)
                {
                    stateNames.Add(state.stateName);
                    stateIDs.Add(state.stateID);
                }
            }

            int selectedIndex = stateIDs.IndexOf(selectedTransition.targetStateID);
            if (selectedIndex < 0) selectedIndex = 0;

            selectedIndex = EditorGUILayout.Popup("Target State", selectedIndex, stateNames.ToArray());
            if (selectedIndex >= 0 && selectedIndex < stateIDs.Count)
            {
                selectedTransition.targetStateID = stateIDs[selectedIndex];
            }
        }

        private void DrawConditionsSection(TransitionDefinition selectedTransition)
        {
            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);

            if (selectedTransition.conditions.Count > 0)
            {
                for (int i = 0; i < selectedTransition.conditions.Count; i++)
                {
                    ConditionDefinition condition = selectedTransition.conditions[i];

                    if (condition != null)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        // Condition header with name and buttons
                        EditorGUILayout.BeginHorizontal();

                        // Edit condition name directly with focus control
                        GUI.SetNextControlName("ConditionName_" + i);
                        EditorGUI.BeginChangeCheck();
                        // Note: ConditionDefinition doesn't have a name field, so we skip this
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(condition);
                        }

                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeObject = condition;
                        }

                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            assetManager.RemoveConditionFromTransition(selectedTransition, i);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                            break;
                        }

                        EditorGUILayout.EndHorizontal();

                        // Edit condition content directly with focus control
                        EditorGUILayout.LabelField("Condition Content", EditorStyles.boldLabel);
                        GUI.SetNextControlName("ConditionContent_" + i);
                        EditorGUI.BeginChangeCheck();
                        condition.conditionContent = EditorGUILayout.TextArea(condition.conditionContent, GUILayout.Height(60));
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(condition);
                        }

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space();
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Invalid Condition");

                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            assetManager.RemoveConditionFromTransition(selectedTransition, i);
                            EditorGUILayout.EndHorizontal();
                            break;
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            // 檢查條件數量和顯示相應的提示
            DrawConditionWarnings(selectedTransition);

            // 添加條件按鈕
            if (GUILayout.Button("Add Condition"))
            {
                ConditionDefinition newCondition = assetManager.CreateCondition(selectedTransition);
                if (newCondition != null)
                {
                    Selection.activeObject = newCondition;
                }
            }
        }

        private void DrawConditionWarnings(TransitionDefinition selectedTransition)
        {
            // 檢查源狀態類型
            string sourceStateKey = GetSourceStateKey(selectedTransition);

            if (selectedTransition.conditions.Count == 0)
            {
                if (sourceStateKey.Contains("AnyState"))
                {
                    EditorGUILayout.HelpBox("Required conditions for AnyState transitions.", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("This transition has no conditions. It will always be taken.", MessageType.Warning);
                }
            }
        }

        private string GetSourceStateKey(TransitionDefinition transition)
        {
            // 查找包含此轉換的源狀態
            foreach (var entry in editorData.ConnectionCache)
            {
                if (entry.Value.Contains(transition))
                {
                    return entry.Key;
                }
            }

            // 如果在緩存中找不到，直接搜索狀態
            foreach (var state in editorData.CurrentStateMachine.states)
            {
                if (state != null && state.transitions.Contains(transition))
                {
                    return state.stateID;
                }
            }

            if (editorData.CurrentStateMachine.anyState != null &&
                editorData.CurrentStateMachine.anyState.transitions.Contains(transition))
            {
                return "AnyState";
            }

            return string.Empty;
        }
    }
}
