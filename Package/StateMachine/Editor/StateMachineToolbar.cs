using UnityEngine;
using UnityEditor;

namespace Assets.Scripts.StateMachine.Editor
{
    /// <summary>
    /// 管理狀態機編輯器的工具欄
    /// </summary>
    public class StateMachineToolbar
    {
        private StateMachineEditorData editorData;
        private StateMachineAssetManager assetManager;

        public StateMachineToolbar(StateMachineEditorData data, StateMachineAssetManager manager)
        {
            editorData = data;
            assetManager = manager;
        }

        public void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("New", EditorStyles.toolbarButton))
            {
                CreateNewStateMachine();
            }

            if (GUILayout.Button("Load", EditorStyles.toolbarButton))
            {
                LoadStateMachine();
            }

            if (editorData.CurrentStateMachine != null)
            {
                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                {
                    SaveStateMachine();
                }

                if (GUILayout.Button("Add State", EditorStyles.toolbarButton))
                {
                    AddNewState();
                }

                if (editorData.CurrentStateMachine.anyState == null &&
                    GUILayout.Button("Add AnyState", EditorStyles.toolbarButton))
                {
                    AddAnyState();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CreateNewStateMachine()
        {
            assetManager.CreateNewStateMachine();
        }

        private void LoadStateMachine()
        {
            assetManager.LoadStateMachine();
        }

        private void SaveStateMachine()
        {
            assetManager.SaveStateMachine();
        }

        private void AddNewState()
        {
            StateDefinition newState = assetManager.CreateNewState(new Vector2(200, 200));
            if (newState != null)
            {
                editorData.SelectState(newState);
            }
        }

        private void AddAnyState()
        {
            StateDefinition anyState = assetManager.CreateAnyState(new Vector2(50, 50));
            if (anyState != null)
            {
                editorData.SelectState(anyState);
            }
        }
    }
}
