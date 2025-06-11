using UnityEngine;
using UnityEditor;

namespace Assets.Scripts.StateMachine.Editor
{
    /// <summary>
    /// 管理狀態機圖形視圖的主要容器
    /// </summary>
    public class StateMachineGraphView
    {
        private StateMachineEditorData editorData;
        private StateMachineAssetManager assetManager;
        private NodeRenderer nodeRenderer;
        private TransitionRenderer transitionRenderer;
        private GraphEventHandler eventHandler;

        public StateMachineGraphView(StateMachineEditorData data, StateMachineAssetManager manager)
        {
            editorData = data;
            assetManager = manager;

            // 創建渲染器
            nodeRenderer = new NodeRenderer(editorData);
            transitionRenderer = new TransitionRenderer(editorData, nodeRenderer);

            // 創建事件處理器
            eventHandler = new GraphEventHandler(editorData, assetManager, nodeRenderer);

            // 設置事件回調
            SetupEventCallbacks();
        }

        private void SetupEventCallbacks()
        {
            // 節點事件
            nodeRenderer.OnNodeClicked += HandleNodeClicked;
            nodeRenderer.OnNodeRightClicked += HandleNodeRightClicked;
            nodeRenderer.OnNodeDragStarted += HandleNodeDragStarted;

            // 轉換事件
            transitionRenderer.OnTransitionClicked += HandleTransitionClicked;
            transitionRenderer.OnTransitionRightClicked += HandleTransitionRightClicked;

            // 事件處理器事件
            eventHandler.OnTransitionCreated += HandleTransitionCreated;
        }

        public void DrawGraphArea(Rect windowRect)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(windowRect.width * 0.7f));

            if (editorData.CurrentStateMachine != null)
            {
                // 儲存滾動位置前的偏移量
                Vector2 oldScrollPosition = editorData.GraphScrollPosition;

                // 使用滾動視圖
                editorData.GraphScrollPosition = EditorGUILayout.BeginScrollView(editorData.GraphScrollPosition);

                // 檢測滾動位置是否變化
                bool scrollChanged = oldScrollPosition != editorData.GraphScrollPosition;

                // 建立繪圖區域
                Rect graphRect = GUILayoutUtility.GetRect(2000, 2000);
                GUI.Box(graphRect, "");

                // 先繪製節點
                DrawAllNodes();

                // 再繪製轉換線和箭頭
                transitionRenderer.DrawAllTransitions();

                // 繪製轉換創建線
                if (editorData.IsCreatingTransition)
                {
                    transitionRenderer.DrawTransitionCreationLine();
                }

                // 處理事件
                eventHandler.HandleGraphEvents(graphRect, scrollChanged);

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("No state machine loaded. Create a new one or load an existing one.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAllNodes()
        {
            // 繪製普通狀態節點
            foreach (var state in editorData.CurrentStateMachine.states)
            {
                if (state != null)
                    nodeRenderer.DrawStateNode(state);
            }

            // 繪製AnyState節點
            if (editorData.CurrentStateMachine.anyState != null)
            {
                nodeRenderer.DrawAnyStateNode();
            }
        }

        private void HandleNodeClicked(StateDefinition state)
        {
            if (editorData.IsCreatingTransition)
            {
                // 完成轉換創建
                eventHandler.FinishTransitionCreation(state);
            }
            else
            {
                // 選擇狀態
                editorData.SelectState(state);
                // 清除焦點
                GUI.FocusControl(null);
            }
        }

        private void HandleNodeRightClicked(StateDefinition state)
        {
            eventHandler.ShowStateContextMenu(state);
        }

        private void HandleNodeDragStarted(StateDefinition state, Vector2 dragOffset)
        {
            if (!editorData.IsCreatingTransition)
            {
                editorData.StartDragging(state, dragOffset);
            }
        }

        private void HandleTransitionClicked(TransitionDefinition transition)
        {
            editorData.SelectTransition(transition);
            // 清除焦點
            GUI.FocusControl(null);
        }

        private void HandleTransitionRightClicked(TransitionDefinition transition)
        {
            eventHandler.ShowTransitionContextMenu(transition);
        }

        private void HandleTransitionCreated(TransitionDefinition transition)
        {
            editorData.SelectTransition(transition);
        }
    }
}
