using UnityEngine;
using UnityEditor;

namespace Assets.Scripts.StateMachine.Editor
{
    /// <summary>
    /// 處理圖形區域的事件
    /// </summary>
    public class GraphEventHandler
    {
        private StateMachineEditorData editorData;
        private StateMachineAssetManager assetManager;
        private NodeRenderer nodeRenderer;

        // 事件
        public System.Action<TransitionDefinition> OnTransitionCreated;

        public GraphEventHandler(StateMachineEditorData data, StateMachineAssetManager manager, NodeRenderer renderer)
        {
            editorData = data;
            assetManager = manager;
            nodeRenderer = renderer;
        }

        public void HandleGraphEvents(Rect graphRect, bool scrollChanged)
        {
            HandlePanningEvents(graphRect);
            HandleDragEvents(scrollChanged);
            HandleFocusControl();
            HandleContextMenu(graphRect);
        }

        private void HandlePanningEvents(Rect graphRect)
        {
            Event e = Event.current;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 2 && graphRect.Contains(e.mousePosition)) // 滑鼠中鍵
                    {
                        editorData.StartPanning(e.mousePosition);
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (editorData.IsPanningView)
                    {
                        editorData.UpdatePanning(e.mousePosition);
                        GUI.changed = true;
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (e.button == 2 && editorData.IsPanningView) // 滑鼠中鍵放開
                    {
                        editorData.StopPanning();
                        e.Use();
                    }
                    break;
            }
        }

        private void HandleDragEvents(bool scrollChanged)
        {
            Event e = Event.current;

            switch (e.type)
            {
                case EventType.MouseDrag:
                    if (editorData.IsDragging && editorData.DraggedState != null && !editorData.IsPanningView)
                    {
                        // 計算拖曳後的位置，dragOffset 已經在 NodeRenderer 中正確計算
                        editorData.DraggedState.editorPosition = e.mousePosition - editorData.DragOffset + editorData.GraphScrollPosition;

                        GUI.changed = true;
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (editorData.IsDragging)
                    {
                        editorData.StopDragging();
                        e.Use();
                    }

                    if (editorData.IsCreatingTransition && e.button == 1)
                    {
                        editorData.CancelTransitionCreation();
                        e.Use();
                    }
                    break;

                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Escape)
                    {
                        if (editorData.IsCreatingTransition)
                        {
                            editorData.CancelTransitionCreation();
                            GUI.changed = true;
                            e.Use();
                        }
                    }
                    break;
            }
        }

        private void HandleFocusControl()
        {
            // Check for mouse clicks outside of text fields
            Event e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                // Clear focus when clicking anywhere
                GUI.FocusControl(null);
            }
        }

        private void HandleContextMenu(Rect graphRect)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 1 && graphRect.Contains(e.mousePosition))
            {
                if (!editorData.IsCreatingTransition && !editorData.IsPanningView)
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Add State"), false, () =>
                    {
                        AddNewStateAt(e.mousePosition);
                    });

                    if (editorData.CurrentStateMachine.anyState == null)
                    {
                        menu.AddItem(new GUIContent("Add Any State"), false, () =>
                        {
                            AddAnyStateAt(e.mousePosition);
                        });
                    }

                    menu.ShowAsContext();
                    e.Use();
                }
            }
        }

        public void ShowStateContextMenu(StateDefinition state)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Delete State"), false, () =>
            {
                assetManager.DeleteState(state);
            });

            if (state != editorData.CurrentStateMachine.anyState)
            {
                menu.AddItem(new GUIContent("Set as Default"), false, () =>
                {
                    editorData.CurrentStateMachine.defaultState = state;
                    assetManager.SaveStateMachine();
                });
            }

            menu.AddItem(new GUIContent("Start Transition"), false, () =>
            {
                editorData.StartTransitionCreation(state);
            });

            menu.ShowAsContext();
        }

        public void ShowTransitionContextMenu(TransitionDefinition transition)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Delete Transition"), false, () =>
            {
                assetManager.DeleteTransition(transition);
            });

            menu.ShowAsContext();
        }

        public void FinishTransitionCreation(StateDefinition targetState)
        {
            if (editorData.TransitionSourceState == targetState)
            {
                editorData.CancelTransitionCreation();
                return;
            }

            TransitionDefinition newTransition = assetManager.CreateTransition(editorData.TransitionSourceState, targetState);

            editorData.CancelTransitionCreation();

            if (newTransition != null)
            {
                OnTransitionCreated?.Invoke(newTransition);
            }
        }

        private void AddNewStateAt(Vector2 position)
        {
            StateDefinition newState = assetManager.CreateNewState(position);
            if (newState != null)
            {
                editorData.SelectState(newState);
            }
        }

        private void AddAnyStateAt(Vector2 position)
        {
            StateDefinition anyState = assetManager.CreateAnyState(position);
            if (anyState != null)
            {
                editorData.SelectState(anyState);
            }
        }
    }
}
