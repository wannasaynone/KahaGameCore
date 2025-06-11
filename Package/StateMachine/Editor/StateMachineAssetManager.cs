using UnityEngine;
using UnityEditor;
using System;

namespace Assets.Scripts.StateMachine.Editor
{
    /// <summary>
    /// 管理狀態機編輯器的資產創建、保存和載入
    /// </summary>
    public class StateMachineAssetManager
    {
        private StateMachineEditorData editorData;

        public StateMachineAssetManager(StateMachineEditorData data)
        {
            editorData = data;
        }

        public void CreateNewStateMachine()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create State Machine",
                "NewStateMachine",
                "asset",
                "Create a new state machine asset",
                editorData.GetLastDirectory()
            );

            if (string.IsNullOrEmpty(path)) return;

            editorData.SetLastDirectory(path);

            StateMachineDefinition newStateMachine = ScriptableObject.CreateInstance<StateMachineDefinition>();
            newStateMachine.stateMachineName = System.IO.Path.GetFileNameWithoutExtension(path);

            AssetDatabase.CreateAsset(newStateMachine, path);
            AssetDatabase.SaveAssets();

            editorData.SetCurrentStateMachine(newStateMachine);
        }

        public void LoadStateMachine()
        {
            string path = EditorUtility.OpenFilePanel(
                "Load State Machine",
                editorData.GetLastDirectory(),
                "asset"
            );

            if (string.IsNullOrEmpty(path)) return;

            editorData.SetLastDirectory(path);

            path = "Assets" + path.Substring(Application.dataPath.Length);

            StateMachineDefinition loadedStateMachine = AssetDatabase.LoadAssetAtPath<StateMachineDefinition>(path);

            if (loadedStateMachine != null)
            {
                loadedStateMachine.OnValidate();
                editorData.SetCurrentStateMachine(loadedStateMachine);
            }
        }

        public void SaveStateMachine()
        {
            if (editorData.CurrentStateMachine == null) return;

            EditorUtility.SetDirty(editorData.CurrentStateMachine);
            AssetDatabase.SaveAssets();
        }

        public StateDefinition CreateNewState(Vector2 position, string defaultName = "NewState")
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create State",
                defaultName,
                "asset",
                "Create a new state asset",
                editorData.GetLastDirectory()
            );

            if (string.IsNullOrEmpty(path)) return null;

            editorData.SetLastDirectory(path);

            StateDefinition newState = ScriptableObject.CreateInstance<StateDefinition>();
            newState.stateID = Guid.NewGuid().ToString();
            newState.stateName = System.IO.Path.GetFileNameWithoutExtension(path);
            newState.editorPosition = position;

            AssetDatabase.CreateAsset(newState, path);
            AssetDatabase.SaveAssets();

            editorData.CurrentStateMachine.states.Add(newState);
            editorData.CurrentStateMachine.statesByID[newState.stateID] = newState;

            if (editorData.CurrentStateMachine.defaultState == null)
            {
                editorData.CurrentStateMachine.defaultState = newState;
            }

            SaveStateMachine();
            return newState;
        }

        public StateDefinition CreateAnyState(Vector2 position)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Any State",
                "AnyState",
                "asset",
                "Create the Any State asset",
                editorData.GetLastDirectory()
            );

            if (string.IsNullOrEmpty(path)) return null;

            editorData.SetLastDirectory(path);

            StateDefinition anyState = ScriptableObject.CreateInstance<StateDefinition>();
            anyState.stateID = "AnyState";
            anyState.stateName = "AnyState";
            anyState.editorPosition = position;

            AssetDatabase.CreateAsset(anyState, path);
            AssetDatabase.SaveAssets();

            editorData.CurrentStateMachine.anyState = anyState;

            SaveStateMachine();
            return anyState;
        }

        public TransitionDefinition CreateTransition(StateDefinition sourceState, StateDefinition targetState)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Transition",
                $"Transition_{sourceState.stateName}_to_{targetState.stateName}",
                "asset",
                "Create a new transition asset",
                editorData.GetLastDirectory()
            );

            if (string.IsNullOrEmpty(path)) return null;

            editorData.SetLastDirectory(path);

            TransitionDefinition newTransition = ScriptableObject.CreateInstance<TransitionDefinition>();
            newTransition.transitionID = Guid.NewGuid().ToString();
            newTransition.targetStateID = targetState.stateID;
            newTransition.lineColor = new Color(0.8f, 0.8f, 0.8f, 1f);

            AssetDatabase.CreateAsset(newTransition, path);
            AssetDatabase.SaveAssets();

            sourceState.transitions.Add(newTransition);

            SaveStateMachine();
            return newTransition;
        }

        public StateBehaviourDefinition CreateBehaviour(StateDefinition state, string behaviourType)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Behaviour",
                $"{state.stateName}_{behaviourType}Behaviour",
                "asset",
                $"Create a new {behaviourType} behaviour asset",
                editorData.GetLastDirectory()
            );

            if (string.IsNullOrEmpty(path)) return null;

            editorData.SetLastDirectory(path);

            StateBehaviourDefinition newBehaviour = ScriptableObject.CreateInstance<StateBehaviourDefinition>();
            newBehaviour.behaviourID = Guid.NewGuid().ToString();
            newBehaviour.behaviourName = $"{state.stateName} {behaviourType}";

            AssetDatabase.CreateAsset(newBehaviour, path);
            AssetDatabase.SaveAssets();

            switch (behaviourType)
            {
                case "Enter":
                    state.enterBehaviour = newBehaviour;
                    break;
                case "Exit":
                    state.exitBehaviour = newBehaviour;
                    break;
                case "Update":
                    state.updateBehaviour = newBehaviour;
                    break;
            }

            SaveStateMachine();
            return newBehaviour;
        }

        public ConditionDefinition CreateCondition(TransitionDefinition transition)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Condition",
                $"Condition_{transition.transitionID}",
                "asset",
                "Create a new condition asset",
                editorData.GetLastDirectory()
            );

            if (string.IsNullOrEmpty(path)) return null;

            editorData.SetLastDirectory(path);

            ConditionDefinition newCondition = ScriptableObject.CreateInstance<ConditionDefinition>();
            newCondition.conditionID = Guid.NewGuid().ToString();

            AssetDatabase.CreateAsset(newCondition, path);
            AssetDatabase.SaveAssets();

            transition.conditions.Add(newCondition);
            EditorUtility.SetDirty(transition);
            AssetDatabase.SaveAssets();

            return newCondition;
        }

        public void DeleteState(StateDefinition state)
        {
            if (state == editorData.CurrentStateMachine.anyState)
            {
                // Delete all transitions from AnyState
                for (int i = editorData.CurrentStateMachine.anyState.transitions.Count - 1; i >= 0; i--)
                {
                    DeleteTransition(editorData.CurrentStateMachine.anyState.transitions[i]);
                }

                editorData.CurrentStateMachine.anyState = null;
            }
            else
            {
                // Delete all transitions from this state
                for (int i = state.transitions.Count - 1; i >= 0; i--)
                {
                    DeleteTransition(state.transitions[i]);
                }

                editorData.CurrentStateMachine.states.Remove(state);
                editorData.CurrentStateMachine.statesByID.Remove(state.stateID);

                if (editorData.CurrentStateMachine.defaultState == state)
                {
                    editorData.CurrentStateMachine.defaultState = editorData.CurrentStateMachine.states.Count > 0 ?
                        editorData.CurrentStateMachine.states[0] : null;
                }

                // Remove transitions from other states that point to this state
                foreach (var otherState in editorData.CurrentStateMachine.states)
                {
                    if (otherState != null)
                    {
                        for (int i = otherState.transitions.Count - 1; i >= 0; i--)
                        {
                            if (otherState.transitions[i].targetStateID == state.stateID)
                            {
                                DeleteTransition(otherState.transitions[i]);
                            }
                        }
                    }
                }

                // Remove transitions from AnyState that point to this state
                if (editorData.CurrentStateMachine.anyState != null)
                {
                    for (int i = editorData.CurrentStateMachine.anyState.transitions.Count - 1; i >= 0; i--)
                    {
                        if (editorData.CurrentStateMachine.anyState.transitions[i].targetStateID == state.stateID)
                        {
                            DeleteTransition(editorData.CurrentStateMachine.anyState.transitions[i]);
                        }
                    }
                }
            }

            if (editorData.SelectedState == state)
            {
                editorData.ClearSelection();
            }

            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(state));
            SaveStateMachine();
        }

        public void DeleteTransition(TransitionDefinition transition)
        {
            // Delete all conditions associated with this transition
            for (int i = transition.conditions.Count - 1; i >= 0; i--)
            {
                ConditionDefinition condition = transition.conditions[i];
                if (condition != null)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(condition));
                }
            }

            transition.conditions.Clear();

            // Remove the transition from all states
            foreach (var state in editorData.CurrentStateMachine.states)
            {
                if (state != null)
                {
                    state.transitions.Remove(transition);
                }
            }

            if (editorData.CurrentStateMachine.anyState != null)
            {
                editorData.CurrentStateMachine.anyState.transitions.Remove(transition);
            }

            if (editorData.SelectedTransition == transition)
            {
                editorData.ClearSelection();
            }

            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(transition));
            SaveStateMachine();
        }

        public void RemoveTransitionFromState(StateDefinition state, int index)
        {
            if (index >= 0 && index < state.transitions.Count)
            {
                TransitionDefinition transition = state.transitions[index];
                state.transitions.RemoveAt(index);

                if (editorData.SelectedTransition == transition)
                {
                    editorData.ClearSelection();
                }

                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(transition));
                SaveStateMachine();
            }
        }

        public void RemoveConditionFromTransition(TransitionDefinition transition, int index)
        {
            if (index >= 0 && index < transition.conditions.Count)
            {
                ConditionDefinition condition = transition.conditions[index];
                transition.conditions.RemoveAt(index);

                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(condition));
                EditorUtility.SetDirty(transition);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
