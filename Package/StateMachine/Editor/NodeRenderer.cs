using UnityEngine;
using UnityEditor;

namespace Assets.Scripts.StateMachine.Editor
{
    /// <summary>
    /// 負責繪製狀態節點
    /// </summary>
    public class NodeRenderer
    {
        private const float NODE_WIDTH = 150;
        private const float NODE_HEIGHT = 80;
        private const float ANY_STATE_NODE_HEIGHT = 100;

        private StateMachineEditorData editorData;

        // 事件
        public System.Action<StateDefinition> OnNodeClicked;
        public System.Action<StateDefinition> OnNodeRightClicked;
        public System.Action<StateDefinition, Vector2> OnNodeDragStarted;

        public NodeRenderer(StateMachineEditorData data)
        {
            editorData = data;
        }

        public void DrawStateNode(StateDefinition state)
        {
            Rect nodeRect = new Rect(state.editorPosition.x, state.editorPosition.y, NODE_WIDTH, NODE_HEIGHT);

            bool isSelected = editorData.SelectedState == state;
            bool isDefault = editorData.CurrentStateMachine.defaultState == state;

            GUI.Box(nodeRect, "", isSelected ? EditorStyles.helpBox : EditorStyles.textArea);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.alignment = TextAnchor.MiddleCenter;

            Rect titleRect = new Rect(nodeRect.x, nodeRect.y + 5, nodeRect.width, 20);
            GUI.Label(titleRect, state.stateName, titleStyle);

            if (isDefault)
            {
                Rect defaultRect = new Rect(nodeRect.x + 5, nodeRect.y + 5, 10, 10);
                GUI.DrawTexture(defaultRect, EditorGUIUtility.IconContent("d_Favorite").image);
            }

            HandleNodeEvents(state, nodeRect);
        }

        public void DrawAnyStateNode()
        {
            StateDefinition anyState = editorData.CurrentStateMachine.anyState;
            if (anyState == null) return;

            Rect nodeRect = new Rect(anyState.editorPosition.x, anyState.editorPosition.y, NODE_WIDTH, ANY_STATE_NODE_HEIGHT);

            bool isSelected = editorData.SelectedState == anyState;

            GUI.Box(nodeRect, "", isSelected ? EditorStyles.helpBox : EditorStyles.textArea);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.alignment = TextAnchor.MiddleCenter;

            Rect titleRect = new Rect(nodeRect.x, nodeRect.y + 5, nodeRect.width, 20);
            GUI.Label(titleRect, "Any State", titleStyle);

            Rect contentRect = new Rect(nodeRect.x + 5, nodeRect.y + 25, nodeRect.width - 10, nodeRect.height - 30);
            GUI.Label(contentRect, "Transitions from here override normal transitions", EditorStyles.wordWrappedMiniLabel);

            HandleNodeEvents(anyState, nodeRect);
        }

        private void HandleNodeEvents(StateDefinition state, Rect nodeRect)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && nodeRect.Contains(e.mousePosition))
            {
                if (e.button == 0) // Left click
                {
                    if (editorData.IsCreatingTransition)
                    {
                        // Finish transition creation
                        OnNodeClicked?.Invoke(state);
                    }
                    else
                    {
                        // Start selection and potential drag
                        OnNodeClicked?.Invoke(state);
                        // 計算拖曳偏移量時需要考慮滾動位置
                        Vector2 dragOffset = e.mousePosition - (state.editorPosition - editorData.GraphScrollPosition);
                        OnNodeDragStarted?.Invoke(state, dragOffset);
                    }
                    e.Use();
                }
                else if (e.button == 1) // Right click
                {
                    OnNodeRightClicked?.Invoke(state);
                    e.Use();
                }
            }
        }

        public Vector2 GetStateCenter(StateDefinition state)
        {
            float height = (state == editorData.CurrentStateMachine?.anyState) ? ANY_STATE_NODE_HEIGHT : NODE_HEIGHT;
            return new Vector2(
                state.editorPosition.x + NODE_WIDTH / 2,
                state.editorPosition.y + height / 2
            );
        }

        public Vector2 GetConnectionPoint(StateDefinition state, Vector2 direction, bool isSource)
        {
            float nodeWidth = NODE_WIDTH;
            float nodeHeight = (state == editorData.CurrentStateMachine?.anyState) ? ANY_STATE_NODE_HEIGHT : NODE_HEIGHT;
            Vector2 center = GetStateCenter(state);

            // 將方向向量標準化，確保長度為1
            direction = direction.normalized;

            // 計算連接點
            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);

            // 水平和垂直半尺寸
            float halfWidth = nodeWidth / 2f;
            float halfHeight = nodeHeight / 2f;

            Vector2 connectionPoint;

            if (absX == 0 && absY == 0)
            {
                // 防止零向量情況
                return center;
            }

            // 基於方向向量斜率確定連接點
            // 使用向量與矩形的交點公式
            if (absX * halfHeight > absY * halfWidth)
            {
                // 與左右邊界相交
                float t = halfWidth / Mathf.Abs(direction.x);
                float y = direction.y * t;

                if (direction.x > 0)
                {
                    // 右側邊界
                    connectionPoint = new Vector2(center.x + halfWidth, center.y + y);
                }
                else
                {
                    // 左側邊界
                    connectionPoint = new Vector2(center.x - halfWidth, center.y + y);
                }
            }
            else
            {
                // 與上下邊界相交
                float t = halfHeight / Mathf.Abs(direction.y);
                float x = direction.x * t;

                if (direction.y > 0)
                {
                    // 下側邊界
                    connectionPoint = new Vector2(center.x + x, center.y + halfHeight);
                }
                else
                {
                    // 上側邊界
                    connectionPoint = new Vector2(center.x + x, center.y - halfHeight);
                }
            }

            // 做一個微小的偏移，使箭頭不完全在邊界上
            float offset = 1.0f;
            connectionPoint -= direction * offset;

            return connectionPoint;
        }

        public static float NodeWidth => NODE_WIDTH;
        public static float NodeHeight => NODE_HEIGHT;
        public static float AnyStateNodeHeight => ANY_STATE_NODE_HEIGHT;
    }
}
