using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.StateMachine.Editor
{
    /// <summary>
    /// 重構後的狀態機編輯器窗口 - 模塊化設計
    /// </summary>
    public class StateMachineEditorWindowRefactored : EditorWindow
    {
        // 模塊化組件
        private StateMachineEditorData editorData;
        private StateMachineAssetManager assetManager;
        private StateMachineToolbar toolbar;
        private StateMachineGraphView graphView;
        private StateMachineInspector inspector;

        [MenuItem("Tools/State Machine Editor (Refactored)")]
        public static void ShowWindow()
        {
            GetWindow<StateMachineEditorWindowRefactored>("State Machine Editor (Refactored)");
        }

        private void OnEnable()
        {
            InitializeModules();
            SetupEventCallbacks();
        }

        private void OnDisable()
        {
            CleanupEventCallbacks();
        }

        private void InitializeModules()
        {
            // 初始化數據管理器
            editorData = new StateMachineEditorData();

            // 初始化資產管理器
            assetManager = new StateMachineAssetManager(editorData);

            // 初始化UI模塊
            toolbar = new StateMachineToolbar(editorData, assetManager);
            graphView = new StateMachineGraphView(editorData, assetManager);
            inspector = new StateMachineInspector(editorData, assetManager);
        }

        private void SetupEventCallbacks()
        {
            if (editorData != null)
            {
                editorData.OnDataChanged += HandleDataChanged;
                editorData.OnSelectionChanged += HandleSelectionChanged;
            }
        }

        private void CleanupEventCallbacks()
        {
            if (editorData != null)
            {
                editorData.OnDataChanged -= HandleDataChanged;
                editorData.OnSelectionChanged -= HandleSelectionChanged;
            }
        }

        private void OnGUI()
        {
            // 確保模塊已初始化
            if (editorData == null)
            {
                InitializeModules();
                SetupEventCallbacks();
            }

            // 繪製工具欄
            toolbar.DrawToolbar();

            // 繪製主要內容區域
            EditorGUILayout.BeginHorizontal();

            // 繪製圖形視圖
            graphView.DrawGraphArea(position);

            // 繪製檢查器
            inspector.DrawInspector(position);

            EditorGUILayout.EndHorizontal();

            // 處理數據變更的保存
            HandleDataSaving();
        }

        private void HandleDataChanged()
        {
            Repaint();
        }

        private void HandleSelectionChanged()
        {
            Repaint();
        }

        private void HandleDataSaving()
        {
            if (GUI.changed && editorData.CurrentStateMachine != null)
            {
                // 標記狀態機為已修改
                EditorUtility.SetDirty(editorData.CurrentStateMachine);

                // 標記所有狀態為已修改
                foreach (var state in editorData.CurrentStateMachine.states)
                {
                    if (state != null)
                        EditorUtility.SetDirty(state);
                }

                // 標記AnyState為已修改
                if (editorData.CurrentStateMachine.anyState != null)
                {
                    EditorUtility.SetDirty(editorData.CurrentStateMachine.anyState);
                }
            }
        }

        private void Update()
        {
            // 如果正在創建轉換，需要持續重繪以顯示跟隨鼠標的線條
            if (editorData != null && editorData.IsCreatingTransition)
            {
                Repaint();
            }
        }
    }
}
