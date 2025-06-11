using UnityEngine;
using UnityEditor;

namespace Assets.Scripts.StateMachine.Editor
{
    /// <summary>
    /// 管理檢查器面板的主要容器
    /// </summary>
    public class StateMachineInspector
    {
        private StateMachineEditorData editorData;
        private StateMachineAssetManager assetManager;
        private StateInspector stateInspector;
        private TransitionInspector transitionInspector;

        public StateMachineInspector(StateMachineEditorData data, StateMachineAssetManager manager)
        {
            editorData = data;
            assetManager = manager;

            // 創建子檢查器
            stateInspector = new StateInspector(editorData, assetManager);
            transitionInspector = new TransitionInspector(editorData, assetManager);
        }

        public void DrawInspector(Rect windowRect)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(windowRect.width * 0.3f));

            editorData.InspectorScrollPosition = EditorGUILayout.BeginScrollView(editorData.InspectorScrollPosition);

            if (editorData.CurrentStateMachine != null)
            {
                // 繪製狀態機基本信息
                DrawStateMachineInfo();

                EditorGUILayout.Space();

                // 根據選擇繪製相應的檢查器
                if (editorData.SelectedState != null)
                {
                    stateInspector.DrawStateInspector();
                }
                else if (editorData.SelectedTransition != null)
                {
                    transitionInspector.DrawTransitionInspector();
                }
                else
                {
                    DrawNoSelectionInfo();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawStateMachineInfo()
        {
            EditorGUILayout.LabelField("State Machine", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            editorData.CurrentStateMachine.stateMachineName = EditorGUILayout.TextField("Name", editorData.CurrentStateMachine.stateMachineName);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(editorData.CurrentStateMachine);
            }
        }

        private void DrawNoSelectionInfo()
        {
            EditorGUILayout.HelpBox("Select a state or transition to edit its properties.", MessageType.Info);

            // 顯示狀態機統計信息
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

            int stateCount = editorData.CurrentStateMachine.states?.Count ?? 0;
            EditorGUILayout.LabelField($"States: {stateCount}");

            bool hasAnyState = editorData.CurrentStateMachine.anyState != null;
            EditorGUILayout.LabelField($"Any State: {(hasAnyState ? "Yes" : "No")}");

            string defaultStateName = editorData.CurrentStateMachine.defaultState?.stateName ?? "None";
            EditorGUILayout.LabelField($"Default State: {defaultStateName}");

            // 計算總轉換數
            int totalTransitions = 0;
            if (editorData.CurrentStateMachine.states != null)
            {
                foreach (var state in editorData.CurrentStateMachine.states)
                {
                    if (state != null && state.transitions != null)
                    {
                        totalTransitions += state.transitions.Count;
                    }
                }
            }

            if (editorData.CurrentStateMachine.anyState != null && editorData.CurrentStateMachine.anyState.transitions != null)
            {
                totalTransitions += editorData.CurrentStateMachine.anyState.transitions.Count;
            }

            EditorGUILayout.LabelField($"Total Transitions: {totalTransitions}");
        }
    }
}
