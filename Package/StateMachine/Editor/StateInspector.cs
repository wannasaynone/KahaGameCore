using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace Assets.Scripts.StateMachine.Editor
{
    /// <summary>
    /// 負責繪製狀態檢查器面板
    /// </summary>
    public class StateInspector
    {
        private StateMachineEditorData editorData;
        private StateMachineAssetManager assetManager;

        public StateInspector(StateMachineEditorData data, StateMachineAssetManager manager)
        {
            editorData = data;
            assetManager = manager;
        }

        public void DrawStateInspector()
        {
            if (editorData.SelectedState == null) return;

            StateDefinition selectedState = editorData.SelectedState;

            EditorGUILayout.LabelField("Selected State", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            selectedState.stateName = EditorGUILayout.TextField("Name", selectedState.stateName);
            if (EditorGUI.EndChangeCheck() && string.IsNullOrEmpty(selectedState.stateID))
            {
                selectedState.stateID = Guid.NewGuid().ToString();
            }

            EditorGUILayout.Space();

            // 繪製Enter Behaviour
            DrawBehaviourSection("Enter Behaviour", selectedState.enterBehaviour,
                () => assetManager.CreateBehaviour(selectedState, "Enter"),
                (behaviour) => selectedState.enterBehaviour = behaviour);

            // 繪製Exit Behaviour
            DrawBehaviourSection("Exit Behaviour", selectedState.exitBehaviour,
                () => assetManager.CreateBehaviour(selectedState, "Exit"),
                (behaviour) => selectedState.exitBehaviour = behaviour);

            // 繪製Update Behaviour
            DrawBehaviourSection("Update Behaviour", selectedState.updateBehaviour,
                () => assetManager.CreateBehaviour(selectedState, "Update"),
                (behaviour) => selectedState.updateBehaviour = behaviour);

            EditorGUILayout.Space();

            // 繪製轉換列表
            DrawTransitionsSection(selectedState);

            EditorGUILayout.Space();

            // 繪製默認狀態設置
            DrawDefaultStateSection(selectedState);
        }

        private void DrawBehaviourSection(string title, StateBehaviourDefinition behaviour,
            System.Func<StateBehaviourDefinition> createCallback, System.Action<StateBehaviourDefinition> setCallback)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            StateBehaviourDefinition newBehaviour = (StateBehaviourDefinition)EditorGUILayout.ObjectField(
                "Behaviour Asset", behaviour, typeof(StateBehaviourDefinition), false);

            if (newBehaviour != behaviour)
            {
                setCallback(newBehaviour);
            }

            if (behaviour == null)
            {
                if (GUILayout.Button($"Create {title}"))
                {
                    StateBehaviourDefinition createdBehaviour = createCallback();
                    if (createdBehaviour != null)
                    {
                        setCallback(createdBehaviour);
                    }
                }
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Behaviour header with name and buttons
                EditorGUILayout.BeginHorizontal();

                // Edit behaviour name directly with focus control
                GUI.SetNextControlName($"{title}BehaviourName");
                EditorGUI.BeginChangeCheck();
                behaviour.behaviourName = EditorGUILayout.TextField(behaviour.behaviourName);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(behaviour);
                }

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = behaviour;
                }

                EditorGUILayout.EndHorizontal();

                // Edit behaviour content directly with focus control
                EditorGUILayout.LabelField("Behaviour Content", EditorStyles.boldLabel);
                GUI.SetNextControlName($"{title}BehaviourContent");
                EditorGUI.BeginChangeCheck();
                behaviour.behaviourContent = EditorGUILayout.TextArea(
                    behaviour.behaviourContent, GUILayout.Height(60));
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(behaviour);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawTransitionsSection(StateDefinition selectedState)
        {
            EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);

            // 轉換排序設置
            EditorGUI.BeginChangeCheck();
            selectedState.useOrderedEvaluation = EditorGUILayout.Toggle("Use Ordered Evaluation", selectedState.useOrderedEvaluation);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(selectedState);
            }

            if (selectedState.useOrderedEvaluation)
            {
                EditorGUI.BeginChangeCheck();
                selectedState.stopOnFirstSuccess = EditorGUILayout.Toggle("Stop On First Success", selectedState.stopOnFirstSuccess);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(selectedState);
                }
            }

            EditorGUILayout.Space();

            if (selectedState.transitions != null && selectedState.transitions.Count > 0)
            {
                // 同步排序列表
                selectedState.SyncOrderedTransitions();

                if (selectedState.useOrderedEvaluation)
                {
                    DrawOrderedTransitions(selectedState);
                }
                else
                {
                    DrawLegacyTransitions(selectedState);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No transitions. Right click on state to add a transition to connect to another state.", MessageType.Info);
            }
        }

        private void DrawOrderedTransitions(StateDefinition selectedState)
        {
            EditorGUILayout.LabelField("Ordered Transitions (by priority):", EditorStyles.miniLabel);

            // 按優先級排序顯示
            var orderedTransitions = selectedState.orderedTransitions.OrderBy(ot => ot.priority).ToList();

            for (int i = 0; i < orderedTransitions.Count; i++)
            {
                var orderedTransition = orderedTransitions[i];
                var transition = orderedTransition.transition;

                if (transition != null)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    // 轉換標題行
                    EditorGUILayout.BeginHorizontal();

                    // 啟用/禁用切換
                    EditorGUI.BeginChangeCheck();
                    orderedTransition.enabled = EditorGUILayout.Toggle(orderedTransition.enabled, GUILayout.Width(20));
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(selectedState);
                    }

                    // 優先級顯示
                    EditorGUILayout.LabelField($"Priority: {orderedTransition.priority}", GUILayout.Width(80));

                    // 目標狀態名稱
                    if (!string.IsNullOrEmpty(transition.targetStateID) &&
                        editorData.CurrentStateMachine.statesByID.ContainsKey(transition.targetStateID))
                    {
                        string targetStateName = editorData.CurrentStateMachine.statesByID[transition.targetStateID].stateName;
                        EditorGUILayout.LabelField($"To: {targetStateName}");
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Invalid Transition");
                    }

                    EditorGUILayout.EndHorizontal();

                    // 控制按鈕行
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("↑", GUILayout.Width(30)) && i > 0)
                    {
                        selectedState.MoveTransitionUp(transition);
                        EditorUtility.SetDirty(selectedState);
                    }

                    if (GUILayout.Button("↓", GUILayout.Width(30)) && i < orderedTransitions.Count - 1)
                    {
                        selectedState.MoveTransitionDown(transition);
                        EditorUtility.SetDirty(selectedState);
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        editorData.SelectTransition(transition);
                    }

                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        int transitionIndex = selectedState.transitions.IndexOf(transition);
                        if (transitionIndex >= 0)
                        {
                            assetManager.RemoveTransitionFromState(selectedState, transitionIndex);
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void DrawLegacyTransitions(StateDefinition selectedState)
        {
            EditorGUILayout.LabelField("Transitions (legacy order):", EditorStyles.miniLabel);

            for (int i = 0; i < selectedState.transitions.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                TransitionDefinition transition = selectedState.transitions[i];

                if (transition != null && !string.IsNullOrEmpty(transition.targetStateID) &&
                    editorData.CurrentStateMachine.statesByID.ContainsKey(transition.targetStateID))
                {
                    string targetStateName = editorData.CurrentStateMachine.statesByID[transition.targetStateID].stateName;
                    EditorGUILayout.LabelField($"To: {targetStateName}");

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        editorData.SelectTransition(transition);
                    }

                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        assetManager.RemoveTransitionFromState(selectedState, i);
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Invalid Transition");

                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        assetManager.RemoveTransitionFromState(selectedState, i);
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawDefaultStateSection(StateDefinition selectedState)
        {
            if (editorData.CurrentStateMachine.defaultState != selectedState)
            {
                if (GUILayout.Button("Set as Default State"))
                {
                    editorData.CurrentStateMachine.defaultState = selectedState;
                    assetManager.SaveStateMachine();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("This is the default state.", MessageType.Info);
            }
        }
    }
}
