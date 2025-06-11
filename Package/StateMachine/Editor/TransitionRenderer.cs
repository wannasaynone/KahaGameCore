using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Assets.Scripts.StateMachine.Editor
{
    /// <summary>
    /// 負責繪製轉換線和箭頭
    /// </summary>
    public class TransitionRenderer
    {
        private StateMachineEditorData editorData;
        private NodeRenderer nodeRenderer;

        // 轉換點擊候選項
        private struct TransitionClickCandidate
        {
            public TransitionDefinition transition;
            public float distance;
            public Rect clickRect;
        }

        private List<TransitionClickCandidate> transitionClickCandidates = new List<TransitionClickCandidate>();

        // 事件
        public System.Action<TransitionDefinition> OnTransitionClicked;
        public System.Action<TransitionDefinition> OnTransitionRightClicked;

        public TransitionRenderer(StateMachineEditorData data, NodeRenderer nodeRenderer)
        {
            editorData = data;
            this.nodeRenderer = nodeRenderer;
        }

        public void DrawAllTransitions()
        {
            if (editorData.CurrentStateMachine == null) return;

            // 清除連接緩存和點擊候選項
            editorData.ClearConnectionCache();
            ClearTransitionClickCandidates();

            // 先收集所有轉換到緩存
            CollectTransitionsToCache();

            // 繪製所有轉換
            DrawCachedTransitions();

            // 處理點擊候選項
            HandleTransitionClicks();
        }

        public void DrawTransitionCreationLine()
        {
            if (!editorData.IsCreatingTransition || editorData.TransitionSourceState == null) return;

            Vector2 sourceCenter = nodeRenderer.GetStateCenter(editorData.TransitionSourceState);
            Vector2 mousePos = Event.current.mousePosition;

            // 計算從源節點到鼠標的方向
            Vector2 direction = (mousePos - sourceCenter).normalized;

            // 獲取源節點的連接點
            Vector2 startPos = nodeRenderer.GetConnectionPoint(editorData.TransitionSourceState, direction, true);

            Handles.BeginGUI();
            Handles.color = Color.white;
            Handles.DrawLine(startPos, mousePos, 2f);
            Handles.EndGUI();
        }

        private void CollectTransitionsToCache()
        {
            // 收集普通狀態的轉換
            foreach (var state in editorData.CurrentStateMachine.states)
            {
                if (state != null)
                {
                    foreach (var transition in state.transitions)
                    {
                        if (transition != null && !string.IsNullOrEmpty(transition.targetStateID) &&
                            editorData.CurrentStateMachine.statesByID.ContainsKey(transition.targetStateID))
                        {
                            StateDefinition targetState = editorData.CurrentStateMachine.statesByID[transition.targetStateID];
                            editorData.AddConnectionToCache(state, targetState, transition);
                        }
                    }
                }
            }

            // 收集AnyState的轉換
            if (editorData.CurrentStateMachine.anyState != null)
            {
                foreach (var transition in editorData.CurrentStateMachine.anyState.transitions)
                {
                    if (transition != null && !string.IsNullOrEmpty(transition.targetStateID) &&
                        editorData.CurrentStateMachine.statesByID.ContainsKey(transition.targetStateID))
                    {
                        StateDefinition targetState = editorData.CurrentStateMachine.statesByID[transition.targetStateID];
                        editorData.AddConnectionToCache(editorData.CurrentStateMachine.anyState, targetState, transition);
                    }
                }
            }
        }

        private void DrawCachedTransitions()
        {
            foreach (var entry in editorData.ConnectionCache)
            {
                // 解析連接鍵
                string connectionKey = entry.Key;
                StateDefinition source = null;
                StateDefinition target = null;

                // 處理 ANY STATE 的特殊情況
                if (connectionKey.StartsWith("AnyState_"))
                {
                    source = editorData.CurrentStateMachine.anyState;
                    string targetId = connectionKey.Substring("AnyState_".Length);
                    if (editorData.CurrentStateMachine.statesByID.ContainsKey(targetId))
                    {
                        target = editorData.CurrentStateMachine.statesByID[targetId];
                    }
                }
                else if (connectionKey.EndsWith("_AnyState"))
                {
                    string sourceId = connectionKey.Substring(0, connectionKey.Length - "_AnyState".Length);
                    if (editorData.CurrentStateMachine.statesByID.ContainsKey(sourceId))
                    {
                        source = editorData.CurrentStateMachine.statesByID[sourceId];
                    }
                    target = editorData.CurrentStateMachine.anyState;
                }
                else
                {
                    // 標準情況解析
                    string[] stateIds = connectionKey.Split('_');
                    if (stateIds.Length == 2 &&
                        editorData.CurrentStateMachine.statesByID.ContainsKey(stateIds[0]) &&
                        editorData.CurrentStateMachine.statesByID.ContainsKey(stateIds[1]))
                    {
                        source = editorData.CurrentStateMachine.statesByID[stateIds[0]];
                        target = editorData.CurrentStateMachine.statesByID[stateIds[1]];
                    }
                }

                // 確保源和目標都有效
                if (source != null && target != null)
                {
                    for (int i = 0; i < entry.Value.Count; i++)
                    {
                        DrawTransitionWithOffset(source, target, entry.Value[i], i, entry.Value.Count);
                    }
                }
            }
        }

        private void DrawTransitionWithOffset(StateDefinition sourceState, StateDefinition targetState,
                                             TransitionDefinition transition, int index, int total)
        {
            bool isSelected = editorData.SelectedTransition == transition;
            Color lineColor = isSelected ? Color.yellow : transition.lineColor;

            Handles.BeginGUI();
            Handles.color = lineColor;

            // 獲取節點的中心位置
            Vector2 sourceCenter = nodeRenderer.GetStateCenter(sourceState);
            Vector2 targetCenter = nodeRenderer.GetStateCenter(targetState);

            // 計算方向向量
            Vector2 direction = (targetCenter - sourceCenter).normalized;

            // 檢查是否存在相反方向的連接
            int connectionCount;
            int reverseIndex;
            bool hasReverseConnection = editorData.HasReverseConnection(sourceState, targetState, out connectionCount, out reverseIndex);

            // 保存實際的起點和終點，用於後續點擊計算
            Vector2 actualStartPos, actualEndPos;
            float offset;

            // 如果是自環，使用不同的繪製方法
            if (sourceState == targetState)
            {
                DrawSelfTransition(sourceState, transition, index, total);
                return;
            }
            else
            {
                // 計算垂直於方向的偏移向量
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);

                // 計算偏移量（基於連接數量和索引）
                float baseOffset = 15f; // 基本偏移距離

                if (hasReverseConnection)
                {
                    // 如果有雙向連接，一邊為正偏移，一邊為負偏移
                    if (index == 0 && reverseIndex == 0)
                    {
                        // 第一個連接使用正偏移
                        offset = baseOffset;
                    }
                    else if (index > 0 && reverseIndex > 0)
                    {
                        // 多個連接時，交替使用不同偏移
                        offset = baseOffset * (1 + index * 0.5f) * (index % 2 == 0 ? 1 : -1);
                    }
                    else
                    {
                        // 單向多連接時
                        offset = baseOffset * (1 + index * 0.5f);
                    }
                }
                else
                {
                    // 單向連接
                    if (total == 1)
                    {
                        // 如果只有一個連接，使用標準偏移
                        offset = 0;
                    }
                    else
                    {
                        // 多個單向連接時，使用逐漸增加的偏移
                        float t = (float)index / (float)(total - 1) - 0.5f;
                        offset = baseOffset * 2 * t;
                    }
                }

                // 應用偏移到起點和終點
                Vector2 offsetVector = perpendicular * offset;

                // 獲取基本連接點
                Vector2 baseStartPos = nodeRenderer.GetConnectionPoint(sourceState, direction, true);
                Vector2 baseEndPos = nodeRenderer.GetConnectionPoint(targetState, -direction, false);

                // 應用偏移
                Vector2 startPos = baseStartPos + offsetVector;
                Vector2 endPos = baseEndPos + offsetVector;

                // 保存實際的連接點
                actualStartPos = startPos;
                actualEndPos = endPos;

                // 重新計算實際的方向向量（從起點到終點）
                Vector2 actualDirection = (endPos - startPos).normalized;

                // 繪製曲線而非直線（使用貝茲曲線）
                if (offset != 0)
                {
                    // 計算控制點
                    float distance = Vector2.Distance(startPos, endPos);
                    Vector2 midPoint = (startPos + endPos) * 0.5f;
                    Vector2 controlPoint = midPoint + offsetVector * 0.5f;

                    // 繪製貝茲曲線
                    Handles.DrawBezier(startPos, endPos,
                                      startPos + actualDirection * (distance * 0.25f),
                                      endPos - actualDirection * (distance * 0.25f),
                                      lineColor, null, 2f);
                }
                else
                {
                    // 如果沒有偏移，就繪製直線
                    Handles.DrawLine(startPos, endPos);
                }

                // 繪製箭頭
                DrawArrowhead(endPos, actualDirection, lineColor, 10f);

                // 在線段中間繪製條件標籤
                if (transition.conditions.Count > 0)
                {
                    Vector2 midPoint = (startPos + endPos) * 0.5f + offsetVector * 0.5f;
                    DrawConditionLabel(midPoint, transition);
                }
            }

            Handles.EndGUI();

            // 計算點擊區域
            float clickArea = 15f + Mathf.Abs(index) * 5f; // 增加點擊區域與偏移成比例
            Rect clickRect = GetLineClickRect(actualStartPos, actualEndPos, clickArea, offset != 0);

            // 計算到滑鼠位置的距離（用於後續選擇最近的轉換）
            Event e = Event.current;
            if (e.type == EventType.MouseDown && clickRect.Contains(e.mousePosition))
            {
                // 計算點擊位置到線段的距離
                float distanceToMouse = DistancePointToLine(e.mousePosition, actualStartPos, actualEndPos);

                // 添加到候選項列表
                TransitionClickCandidate candidate = new TransitionClickCandidate
                {
                    transition = transition,
                    distance = distanceToMouse,
                    clickRect = clickRect
                };

                AddTransitionClickCandidate(candidate);
            }
        }

        private void DrawSelfTransition(StateDefinition state, TransitionDefinition transition, int index, int total)
        {
            Vector2 center = nodeRenderer.GetStateCenter(state);
            float nodeWidth = NodeRenderer.NodeWidth;
            float nodeHeight = (state == editorData.CurrentStateMachine.anyState) ? NodeRenderer.AnyStateNodeHeight : NodeRenderer.NodeHeight;

            float halfHeight = nodeHeight / 2;
            float halfWidth = nodeWidth / 2;

            // 根據索引計算自環的位置和大小
            float baseSize = 30f;
            float size = baseSize + index * 15f; // 每個額外的自環增加大小

            // 計算自環的位置（右側、頂部、左側、底部）
            float angleOffset = index % 4 * 90f; // 在四個方向上分布自環
            float angle = angleOffset * Mathf.Deg2Rad;

            // 計算自環的起點
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 startPoint = new Vector2(
                center.x + direction.x * halfWidth,
                center.y + direction.y * halfHeight
            );

            // 計算控制點和終點
            Vector2 controlPoint1 = startPoint + direction * size;
            Vector2 controlPoint2 = startPoint + new Vector2(
                direction.x * size,
                direction.y * size + size
            );
            Vector2 endPoint = startPoint + new Vector2(0, size * 0.5f);

            // 使用單一顏色
            Color lineColor = editorData.SelectedTransition == transition ? Color.yellow : transition.lineColor;
            Color originalColor = Handles.color;
            Handles.color = lineColor;

            // 繪製貝茲曲線作為自環
            Handles.DrawBezier(
                startPoint,
                endPoint,
                controlPoint1,
                controlPoint2,
                lineColor,
                null,
                2f
            );

            // 計算箭頭方向 - 基於曲線終點的切線
            Vector2 tangent = (endPoint - controlPoint2).normalized;

            // 繪製箭頭
            DrawArrowhead(endPoint, tangent, lineColor, 10f);

            // 恢復顏色
            Handles.color = originalColor;

            // 繪製條件標籤在自環旁邊
            if (transition.conditions.Count > 0)
            {
                Vector2 labelPos = startPoint + direction * (size + 15f);
                DrawConditionLabel(labelPos, transition);
            }

            // 計算自環的點擊區域
            Rect clickRect = GetSelfLoopClickRect(startPoint, endPoint, controlPoint1, controlPoint2, 15f);

            // 計算到滑鼠位置的距離
            Event e = Event.current;
            if (e.type == EventType.MouseDown && clickRect.Contains(e.mousePosition))
            {
                // 對於自環，使用中心點到點擊位置的距離作為度量
                float distanceToMouse = Vector2.Distance(e.mousePosition, (startPoint + endPoint + controlPoint1 + controlPoint2) / 4);

                // 添加到候選項列表
                TransitionClickCandidate candidate = new TransitionClickCandidate
                {
                    transition = transition,
                    distance = distanceToMouse,
                    clickRect = clickRect
                };

                AddTransitionClickCandidate(candidate);
            }
        }

        private void DrawArrowhead(Vector2 position, Vector2 direction, Color color, float size)
        {
            // 計算垂直於方向的向量
            Vector2 right = new Vector2(-direction.y, direction.x);

            // 計算箭頭的三個點
            Vector2 p1 = position - direction * size + right * (size * 0.5f);
            Vector2 p2 = position - direction * size - right * (size * 0.5f);
            Vector2 p3 = position;

            // 繪製填充的箭頭
            Color originalColor = Handles.color;
            Handles.color = color;

            // 使用三角形來繪製實心箭頭
            Vector3[] points = new Vector3[] { p1, p2, p3 };
            Handles.DrawAAConvexPolygon(points);

            Handles.color = originalColor;
        }

        private void DrawConditionLabel(Vector2 position, TransitionDefinition transition)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;

            // 繪製帶有背景的標籤
            Color bgColor = transition.lineColor;
            bgColor.a = 0.8f;

            Color originalColor = GUI.color;
            GUI.color = bgColor;
            GUI.color = originalColor;
        }

        private float DistancePointToLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            // 向量 AB 是線段
            Vector2 AB = lineEnd - lineStart;
            // 向量 AP 是從線段起點到點 P
            Vector2 AP = point - lineStart;

            // 計算點到線的投影
            float projAB_AP = Vector2.Dot(AP, AB);
            float lenAB_squared = AB.sqrMagnitude;

            // 參數 t 表示投影點在線段上的位置
            float t = Mathf.Clamp01(projAB_AP / lenAB_squared);

            // 計算投影點
            Vector2 projection = lineStart + t * AB;

            // 返回點到投影點的距離
            return Vector2.Distance(point, projection);
        }

        private Rect GetLineClickRect(Vector2 start, Vector2 end, float thickness, bool isCurve)
        {
            // 計算包含兩點的最小矩形，加上邊距
            Vector2 min = Vector2.Min(start, end);
            Vector2 max = Vector2.Max(start, end);

            // 對於曲線，使用更大的點擊區域
            if (isCurve)
            {
                // 曲線通常會偏離直線，所以使用更大的區域
                min -= new Vector2(thickness * 1.5f, thickness * 1.5f);
                max += new Vector2(thickness * 1.5f, thickness * 1.5f);
            }
            else
            {
                // 線條點擊區域
                min -= new Vector2(thickness, thickness);
                max += new Vector2(thickness, thickness);
            }

            // 如果是幾乎水平或垂直的線，進一步擴大另一個方向的點擊區域
            Vector2 diff = max - min;
            if (diff.x < 10 && diff.y > diff.x * 3)
            {
                // 幾乎垂直的線
                min.x -= thickness * 2;
                max.x += thickness * 2;
            }
            else if (diff.y < 10 && diff.x > diff.y * 3)
            {
                // 幾乎水平的線
                min.y -= thickness * 2;
                max.y += thickness * 2;
            }

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        private Rect GetSelfLoopClickRect(Vector2 start, Vector2 end, Vector2 control1, Vector2 control2, float thickness)
        {
            // 計算包含所有控制點的最小矩形
            float minX = Mathf.Min(Mathf.Min(Mathf.Min(start.x, end.x), control1.x), control2.x);
            float minY = Mathf.Min(Mathf.Min(Mathf.Min(start.y, end.y), control1.y), control2.y);
            float maxX = Mathf.Max(Mathf.Max(Mathf.Max(start.x, end.x), control1.x), control2.x);
            float maxY = Mathf.Max(Mathf.Max(Mathf.Max(start.y, end.y), control1.y), control2.y);

            // 添加邊距
            minX -= thickness;
            minY -= thickness;
            maxX += thickness;
            maxY += thickness;

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private void ClearTransitionClickCandidates()
        {
            transitionClickCandidates.Clear();
        }

        private void AddTransitionClickCandidate(TransitionClickCandidate candidate)
        {
            transitionClickCandidates.Add(candidate);
        }

        private void HandleTransitionClicks()
        {
            if (transitionClickCandidates.Count == 0)
                return;

            Event e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                // 按距離排序候選項，選擇最近的
                transitionClickCandidates.Sort((a, b) => a.distance.CompareTo(b.distance));

                TransitionClickCandidate closest = transitionClickCandidates[0];

                if (closest.clickRect.Contains(e.mousePosition))
                {
                    if (e.button == 0)
                    {
                        OnTransitionClicked?.Invoke(closest.transition);
                        e.Use();
                    }
                    else if (e.button == 1)
                    {
                        OnTransitionRightClicked?.Invoke(closest.transition);
                        e.Use();
                    }
                }
            }

            // 處理完後清除候選項
            ClearTransitionClickCandidates();
        }
    }
}
