using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts.StateMachine.Editor
{
    /// <summary>
    /// 管理狀態機編輯器的數據和狀態
    /// </summary>
    public class StateMachineEditorData
    {
        private const string LAST_PATH_KEY = "StateMachineEditor_LastPath";

        // 當前數據
        public StateMachineDefinition CurrentStateMachine { get; private set; }
        public StateDefinition SelectedState { get; private set; }
        public TransitionDefinition SelectedTransition { get; private set; }

        // 視圖狀態
        public Vector2 GraphScrollPosition { get; set; }
        public Vector2 InspectorScrollPosition { get; set; }

        // 拖拽狀態
        public StateDefinition DraggedState { get; set; }
        public Vector2 DragOffset { get; set; }
        public bool IsDragging { get; set; }

        // 畫面拖拽狀態
        public bool IsPanningView { get; set; }
        public Vector2 LastPanMousePosition { get; set; }

        // 轉換創建狀態
        public bool IsCreatingTransition { get; set; }
        public StateDefinition TransitionSourceState { get; set; }

        // 連接緩存
        public Dictionary<string, List<TransitionDefinition>> ConnectionCache { get; private set; }

        // 事件
        public System.Action OnDataChanged;
        public System.Action OnSelectionChanged;

        public StateMachineEditorData()
        {
            ConnectionCache = new Dictionary<string, List<TransitionDefinition>>();
        }

        public void SetCurrentStateMachine(StateMachineDefinition stateMachine)
        {
            CurrentStateMachine = stateMachine;
            ClearSelection();
            OnDataChanged?.Invoke();
        }

        public void SelectState(StateDefinition state)
        {
            SelectedState = state;
            SelectedTransition = null;
            OnSelectionChanged?.Invoke();
        }

        public void SelectTransition(TransitionDefinition transition)
        {
            SelectedState = null;
            SelectedTransition = transition;
            OnSelectionChanged?.Invoke();
        }

        public void ClearSelection()
        {
            SelectedState = null;
            SelectedTransition = null;
            OnSelectionChanged?.Invoke();
        }

        public void ClearConnectionCache()
        {
            ConnectionCache.Clear();
        }

        public void AddConnectionToCache(StateDefinition source, StateDefinition target, TransitionDefinition transition)
        {
            string connectionKey = GetConnectionKey(source, target);

            if (!ConnectionCache.ContainsKey(connectionKey))
            {
                ConnectionCache[connectionKey] = new List<TransitionDefinition>();
            }

            ConnectionCache[connectionKey].Add(transition);
        }

        public bool HasReverseConnection(StateDefinition source, StateDefinition target, out int connectionCount, out int reverseIndex)
        {
            string connectionKey = GetConnectionKey(source, target);
            string reverseKey = GetConnectionKey(target, source);

            connectionCount = 0;
            reverseIndex = -1;

            if (!ConnectionCache.ContainsKey(connectionKey))
            {
                ConnectionCache[connectionKey] = new List<TransitionDefinition>();
            }

            if (ConnectionCache.ContainsKey(reverseKey))
            {
                reverseIndex = ConnectionCache[reverseKey].Count;
                connectionCount = reverseIndex;
                return true;
            }

            return false;
        }

        private string GetConnectionKey(StateDefinition source, StateDefinition target)
        {
            string sourceId = source == CurrentStateMachine?.anyState ? "AnyState" : source?.stateID ?? "";
            string targetId = target == CurrentStateMachine?.anyState ? "AnyState" : target?.stateID ?? "";
            return sourceId + "_" + targetId;
        }

        public void StartTransitionCreation(StateDefinition sourceState)
        {
            IsCreatingTransition = true;
            TransitionSourceState = sourceState;
        }

        public void CancelTransitionCreation()
        {
            IsCreatingTransition = false;
            TransitionSourceState = null;
        }

        public void StartDragging(StateDefinition state, Vector2 offset)
        {
            IsDragging = true;
            DraggedState = state;
            DragOffset = offset;
        }

        public void StopDragging()
        {
            IsDragging = false;
            DraggedState = null;
        }

        public void StartPanning(Vector2 mousePosition)
        {
            IsPanningView = true;
            LastPanMousePosition = mousePosition;
        }

        public void StopPanning()
        {
            IsPanningView = false;
        }

        public void UpdatePanning(Vector2 mousePosition)
        {
            if (IsPanningView)
            {
                Vector2 delta = LastPanMousePosition - mousePosition;
                GraphScrollPosition += delta;
                LastPanMousePosition = mousePosition;
            }
        }

        public string GetLastDirectory()
        {
            return UnityEditor.EditorPrefs.GetString(LAST_PATH_KEY, Application.dataPath);
        }

        public void SetLastDirectory(string path)
        {
            UnityEditor.EditorPrefs.SetString(LAST_PATH_KEY, path);
        }
    }
}
