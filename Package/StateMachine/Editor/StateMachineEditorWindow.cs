using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Assets.Scripts.StateMachine.Editor
{
    public class StateMachineEditorWindow : EditorWindow
    {
        private const string LAST_PATH_KEY = "StateMachineEditor_LastPath";

        private StateMachineDefinition currentStateMachine;
        private StateDefinition selectedState;
        private TransitionDefinition selectedTransition;

        private Vector2 graphScrollPosition;
        private Vector2 inspectorScrollPosition;

        private const float NODE_WIDTH = 150;
        private const float NODE_HEIGHT = 80;
        private const float ANY_STATE_NODE_HEIGHT = 100;

        private StateDefinition draggedState;
        private Vector2 dragOffset;

        private bool isDragging = false;
        private bool isCreatingTransition = false;
        private StateDefinition transitionSourceState;

        private Dictionary<string, List<TransitionDefinition>> connectionCache = new Dictionary<string, List<TransitionDefinition>>();

        [MenuItem("Tools/State Machine Editor")]
        public static void ShowWindow()
        {
            GetWindow<StateMachineEditorWindow>("State Machine Editor");
        }

        private void OnEnable()
        {
            EditorPrefs.DeleteKey(LAST_PATH_KEY);
        }

        // 在繪製所有轉換前清除緩存
        private void ClearConnectionCache()
        {
            connectionCache.Clear();
        }

        // 檢查是否有相反方向的連接
        private bool HasReverseConnection(StateDefinition source, StateDefinition target, out int connectionCount, out int reverseIndex)
        {
            // 創建連接識別符
            string connectionKey = source.stateID + "_" + target.stateID;
            string reverseKey = target.stateID + "_" + source.stateID;

            // 初始化返回值
            connectionCount = 0;
            reverseIndex = -1;

            // 檢查我們是否已經追蹤了這個連接
            if (!connectionCache.ContainsKey(connectionKey))
            {
                connectionCache[connectionKey] = new List<TransitionDefinition>();
            }

            // 檢查是否存在相反方向連接
            if (connectionCache.ContainsKey(reverseKey))
            {
                reverseIndex = connectionCache[reverseKey].Count;
                connectionCount = reverseIndex;
                return true;
            }

            return false;
        }

        // 添加連接到緩存
        private void AddConnectionToCache(StateDefinition source, StateDefinition target, TransitionDefinition transition)
        {
            string connectionKey = source.stateID + "_" + target.stateID;

            if (!connectionCache.ContainsKey(connectionKey))
            {
                connectionCache[connectionKey] = new List<TransitionDefinition>();
            }

            connectionCache[connectionKey].Add(transition);
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();

            DrawGraphArea();

            DrawInspector();

            EditorGUILayout.EndHorizontal();

            HandleDragEvents();
            HandleFocusControl();

            if (GUI.changed && currentStateMachine != null)
            {
                EditorUtility.SetDirty(currentStateMachine);
                foreach (var state in currentStateMachine.states)
                {
                    if (state != null)
                        EditorUtility.SetDirty(state);
                }

                if (currentStateMachine.anyState != null)
                {
                    EditorUtility.SetDirty(currentStateMachine.anyState);
                }
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
                Repaint();
            }
        }

        private void DrawToolbar()
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

            if (currentStateMachine != null)
            {
                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                {
                    SaveStateMachine();
                }

                if (GUILayout.Button("Add State", EditorStyles.toolbarButton))
                {
                    AddNewState();
                }

                if (currentStateMachine.anyState == null && GUILayout.Button("Add AnyState", EditorStyles.toolbarButton))
                {
                    AddAnyState();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGraphArea()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.7f));

            if (currentStateMachine != null)
            {
                // 儲存滾動位置前的偏移量
                Vector2 oldScrollPosition = graphScrollPosition;

                // 使用滾動視圖
                graphScrollPosition = EditorGUILayout.BeginScrollView(graphScrollPosition);

                // 檢測滾動位置是否變化
                bool scrollChanged = oldScrollPosition != graphScrollPosition;

                // 建立繪圖區域
                Rect graphRect = GUILayoutUtility.GetRect(2000, 2000);
                GUI.Box(graphRect, "");

                // 先繪製節點
                foreach (var state in currentStateMachine.states)
                {
                    if (state != null)
                        DrawStateNode(state);
                }

                if (currentStateMachine.anyState != null)
                {
                    DrawAnyStateNode();
                }

                // 再繪製轉換線和箭頭
                DrawTransitions();

                if (isCreatingTransition)
                {
                    DrawTransitionCreationLine();
                }

                // 處理背景右鍵選單
                HandleContextMenu(graphRect);

                // 如果滾動位置改變且正在拖曳，可能需要調整
                if (scrollChanged && isDragging && draggedState != null)
                {
                    // 滾動時更新拖曳位置
                    Event e = Event.current;
                    draggedState.editorPosition = e.mousePosition - dragOffset + graphScrollPosition;
                    Repaint();
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("No state machine loaded. Create a new one or load an existing one.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawInspector()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.3f));

            inspectorScrollPosition = EditorGUILayout.BeginScrollView(inspectorScrollPosition);

            if (currentStateMachine != null)
            {
                EditorGUILayout.LabelField("State Machine", EditorStyles.boldLabel);
                currentStateMachine.stateMachineName = EditorGUILayout.TextField("Name", currentStateMachine.stateMachineName);

                EditorGUILayout.Space();

                if (selectedState != null)
                {
                    DrawStateInspector();
                }
                else if (selectedTransition != null)
                {
                    DrawTransitionInspector();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawStateInspector()
        {
            EditorGUILayout.LabelField("Selected State", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            selectedState.stateName = EditorGUILayout.TextField("Name", selectedState.stateName);
            if (EditorGUI.EndChangeCheck() && string.IsNullOrEmpty(selectedState.stateID))
            {
                selectedState.stateID = Guid.NewGuid().ToString();
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Enter Behaviour", EditorStyles.boldLabel);
            selectedState.enterBehaviour = (StateBehaviourDefinition)EditorGUILayout.ObjectField(
                "Behaviour Asset", selectedState.enterBehaviour, typeof(StateBehaviourDefinition), false);

            if (selectedState.enterBehaviour == null)
            {
                if (GUILayout.Button("Create Enter Behaviour"))
                {
                    CreateBehaviourForState(selectedState, "Enter");
                }
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Behaviour header with name and buttons
                EditorGUILayout.BeginHorizontal();
                
                // Edit behaviour name directly with focus control
                GUI.SetNextControlName("EnterBehaviourName");
                EditorGUI.BeginChangeCheck();
                selectedState.enterBehaviour.behaviourName = EditorGUILayout.TextField(selectedState.enterBehaviour.behaviourName);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(selectedState.enterBehaviour);
                }
                
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    SelectBehaviour(selectedState.enterBehaviour);
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Edit behaviour content directly with focus control
                EditorGUILayout.LabelField("Behaviour Content", EditorStyles.boldLabel);
                GUI.SetNextControlName("EnterBehaviourContent");
                EditorGUI.BeginChangeCheck();
                selectedState.enterBehaviour.behaviourContent = EditorGUILayout.TextArea(
                    selectedState.enterBehaviour.behaviourContent, GUILayout.Height(60));
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(selectedState.enterBehaviour);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.LabelField("Exit Behaviour", EditorStyles.boldLabel);
            selectedState.exitBehaviour = (StateBehaviourDefinition)EditorGUILayout.ObjectField(
                "Behaviour Asset", selectedState.exitBehaviour, typeof(StateBehaviourDefinition), false);

            if (selectedState.exitBehaviour == null)
            {
                if (GUILayout.Button("Create Exit Behaviour"))
                {
                    CreateBehaviourForState(selectedState, "Exit");
                }
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Behaviour header with name and buttons
                EditorGUILayout.BeginHorizontal();
                
                // Edit behaviour name directly with focus control
                GUI.SetNextControlName("ExitBehaviourName");
                EditorGUI.BeginChangeCheck();
                selectedState.exitBehaviour.behaviourName = EditorGUILayout.TextField(selectedState.exitBehaviour.behaviourName);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(selectedState.exitBehaviour);
                }
                
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    SelectBehaviour(selectedState.exitBehaviour);
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Edit behaviour content directly with focus control
                EditorGUILayout.LabelField("Behaviour Content", EditorStyles.boldLabel);
                GUI.SetNextControlName("ExitBehaviourContent");
                EditorGUI.BeginChangeCheck();
                selectedState.exitBehaviour.behaviourContent = EditorGUILayout.TextArea(
                    selectedState.exitBehaviour.behaviourContent, GUILayout.Height(60));
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(selectedState.exitBehaviour);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.LabelField("Update Behaviour", EditorStyles.boldLabel);
            selectedState.updateBehaviour = (StateBehaviourDefinition)EditorGUILayout.ObjectField(
                "Behaviour Asset", selectedState.updateBehaviour, typeof(StateBehaviourDefinition), false);

            if (selectedState.updateBehaviour == null)
            {
                if (GUILayout.Button("Create Update Behaviour"))
                {
                    CreateBehaviourForState(selectedState, "Update");
                }
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Behaviour header with name and buttons
                EditorGUILayout.BeginHorizontal();
                
                // Edit behaviour name directly with focus control
                GUI.SetNextControlName("UpdateBehaviourName");
                EditorGUI.BeginChangeCheck();
                selectedState.updateBehaviour.behaviourName = EditorGUILayout.TextField(selectedState.updateBehaviour.behaviourName);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(selectedState.updateBehaviour);
                }
                
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    SelectBehaviour(selectedState.updateBehaviour);
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Edit behaviour content directly with focus control
                EditorGUILayout.LabelField("Behaviour Content", EditorStyles.boldLabel);
                GUI.SetNextControlName("UpdateBehaviourContent");
                EditorGUI.BeginChangeCheck();
                selectedState.updateBehaviour.behaviourContent = EditorGUILayout.TextArea(
                    selectedState.updateBehaviour.behaviourContent, GUILayout.Height(60));
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(selectedState.updateBehaviour);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);

            if (selectedState != null && selectedState.transitions != null && selectedState.transitions.Count > 0)
            {
                for (int i = 0; i < selectedState.transitions.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    TransitionDefinition transition = selectedState.transitions[i];

                    if (transition != null && !string.IsNullOrEmpty(transition.targetStateID) &&
                        currentStateMachine.statesByID.ContainsKey(transition.targetStateID))
                    {
                        string targetStateName = currentStateMachine.statesByID[transition.targetStateID].stateName;
                        EditorGUILayout.LabelField($"To: {targetStateName}");

                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            SelectTransition(transition);
                        }

                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            RemoveTransitionInStateWithIndex(selectedState, i);
                            EditorGUILayout.EndHorizontal();
                            break;
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Invalid Transition");

                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            RemoveTransitionInStateWithIndex(selectedState, i);
                            EditorGUILayout.EndHorizontal();
                            break;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No transitions. Right click on state to add a transition to connect to another state.", MessageType.Info);
            }

            EditorGUILayout.Space();

            if (currentStateMachine.defaultState != selectedState)
            {
                if (GUILayout.Button("Set as Default State"))
                {
                    currentStateMachine.defaultState = selectedState;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("This is the default state.", MessageType.Info);
            }
        }

        private void SelectBehaviour(StateBehaviourDefinition behaviour)
        {
            Selection.activeObject = behaviour;
        }

        private void DrawTransitionInspector()
        {
            EditorGUILayout.LabelField("Selected Transition", EditorStyles.boldLabel);

            List<string> stateNames = new List<string>();
            List<string> stateIDs = new List<string>();

            foreach (var state in currentStateMachine.states)
            {
                if (state != null)
                {
                    stateNames.Add(state.stateName);
                    stateIDs.Add(state.stateID);
                }
            }

            int selectedIndex = stateIDs.IndexOf(selectedTransition.targetStateID);
            if (selectedIndex < 0) selectedIndex = 0;

            selectedIndex = EditorGUILayout.Popup("Target State", selectedIndex, stateNames.ToArray());
            if (selectedIndex >= 0 && selectedIndex < stateIDs.Count)
            {
                selectedTransition.targetStateID = stateIDs[selectedIndex];
            }

            selectedTransition.lineColor = EditorGUILayout.ColorField("Line Color", selectedTransition.lineColor);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);

            if (selectedTransition.conditions.Count > 0)
            {
                for (int i = 0; i < selectedTransition.conditions.Count; i++)
                {
                    ConditionDefinition condition = selectedTransition.conditions[i];

                    if (condition != null)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        // Condition header with name and buttons
                        EditorGUILayout.BeginHorizontal();

                        // Edit condition name directly with focus control
                        GUI.SetNextControlName("ConditionName_" + i);
                        EditorGUI.BeginChangeCheck();
                        condition.conditionName = EditorGUILayout.TextField(condition.conditionName);
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(condition);
                        }

                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            SelectCondition(condition);
                        }

                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            RemoveCondition(selectedTransition, i);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                            break;
                        }

                        EditorGUILayout.EndHorizontal();

                        // Edit condition content directly with focus control
                        EditorGUILayout.LabelField("Condition Content", EditorStyles.boldLabel);
                        GUI.SetNextControlName("ConditionContent_" + i);
                        EditorGUI.BeginChangeCheck();
                        condition.conditionContent = EditorGUILayout.TextArea(condition.conditionContent, GUILayout.Height(60));
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(condition);
                        }

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space();
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Invalid Condition");

                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            RemoveCondition(selectedTransition, i);
                            EditorGUILayout.EndHorizontal();
                            break;
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            string sourceStateKey = string.Empty;
            foreach (var connection in connectionCache)
            {
                if (connection.Value.Contains(selectedTransition))
                {
                    sourceStateKey = connection.Key;
                    break;
                }
            }

            if (selectedTransition.conditions.Count == 0)
            {
                if (sourceStateKey.Contains("AnyState"))
                {
                    EditorGUILayout.HelpBox("Required conditions for AnyState transitions.", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("This transition has no conditions. It will always be taken.", MessageType.Warning);
                }

                if (GUILayout.Button("Add Condition"))
                {
                    AddConditionToTransition(selectedTransition);
                }
            }
        }

        private void DrawStateNode(StateDefinition state)
        {
            Rect nodeRect = new Rect(state.editorPosition.x, state.editorPosition.y, NODE_WIDTH, NODE_HEIGHT);

            bool isSelected = selectedState == state;
            bool isDefault = currentStateMachine.defaultState == state;

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

            Event e = Event.current;
            if (e.type == EventType.MouseDown && nodeRect.Contains(e.mousePosition))
            {
                if (e.button == 0)
                {
                    if (isCreatingTransition)
                    {
                        FinishTransitionCreation(state);
                    }
                    else
                    {
                        SelectState(state);
                        draggedState = state;
                        dragOffset = e.mousePosition - state.editorPosition;
                        isDragging = true;
                    }
                    e.Use();
                }
                else if (e.button == 1)
                {
                    ShowStateContextMenu(state);
                    e.Use();
                }
            }
        }

        private void DrawAnyStateNode()
        {
            StateDefinition anyState = currentStateMachine.anyState;

            Rect nodeRect = new Rect(anyState.editorPosition.x, anyState.editorPosition.y, NODE_WIDTH, ANY_STATE_NODE_HEIGHT);

            bool isSelected = selectedState == anyState;

            GUI.Box(nodeRect, "", isSelected ? EditorStyles.helpBox : EditorStyles.textArea);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.alignment = TextAnchor.MiddleCenter;

            Rect titleRect = new Rect(nodeRect.x, nodeRect.y + 5, nodeRect.width, 20);
            GUI.Label(titleRect, "Any State", titleStyle);

            Rect contentRect = new Rect(nodeRect.x + 5, nodeRect.y + 25, nodeRect.width - 10, nodeRect.height - 30);
            GUI.Label(contentRect, "Transitions from here override normal transitions", EditorStyles.wordWrappedMiniLabel);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && nodeRect.Contains(e.mousePosition))
            {
                if (e.button == 0)
                {
                    if (isCreatingTransition)
                    {
                        FinishTransitionCreation(anyState);
                    }
                    else
                    {
                        SelectState(anyState);
                        draggedState = anyState;
                        dragOffset = e.mousePosition - anyState.editorPosition;
                        isDragging = true;
                    }
                    e.Use();
                }
                else if (e.button == 1)
                {
                    ShowStateContextMenu(anyState);
                    e.Use();
                }
            }
        }

        // 添加這個結構來記錄潛在的點擊候選項
        private struct TransitionClickCandidate
        {
            public TransitionDefinition transition;
            public float distance;
            public Rect clickRect;
        }

        private void DrawTransitions()
        {
            // 清除連接緩存和點擊候選項
            ClearConnectionCache();
            ClearTransitionClickCandidates();

            // 先繪製普通狀態的轉換
            foreach (var state in currentStateMachine.states)
            {
                if (state != null)
                {
                    foreach (var transition in state.transitions)
                    {
                        if (transition != null && !string.IsNullOrEmpty(transition.targetStateID) &&
                            currentStateMachine.statesByID.ContainsKey(transition.targetStateID))
                        {
                            StateDefinition targetState = currentStateMachine.statesByID[transition.targetStateID];
                            // 添加到緩存但不立即繪製
                            AddConnectionToCache(state, targetState, transition);
                        }
                    }
                }
            }

            // 然後繪製AnyState的轉換
            if (currentStateMachine.anyState != null)
            {
                foreach (var transition in currentStateMachine.anyState.transitions)
                {
                    if (transition != null && !string.IsNullOrEmpty(transition.targetStateID) &&
                        currentStateMachine.statesByID.ContainsKey(transition.targetStateID))
                    {
                        StateDefinition targetState = currentStateMachine.statesByID[transition.targetStateID];
                        // 添加到緩存但不立即繪製
                        AddConnectionToCache(currentStateMachine.anyState, targetState, transition);
                    }
                }
            }

            // 現在繪製所有轉換，考慮雙向情況
            foreach (var entry in connectionCache)
            {
                // 解析連接鍵
                string connectionKey = entry.Key;

                StateDefinition source = null;
                StateDefinition target = null;

                // 處理 ANY STATE 的特殊情況
                if (connectionKey.StartsWith("AnyState_"))
                {
                    source = currentStateMachine.anyState;
                    string targetId = connectionKey.Substring("AnyState_".Length);
                    if (currentStateMachine.statesByID.ContainsKey(targetId))
                    {
                        target = currentStateMachine.statesByID[targetId];
                    }
                }
                else if (connectionKey.EndsWith("_AnyState"))
                {
                    string sourceId = connectionKey.Substring(0, connectionKey.Length - "_AnyState".Length);
                    if (currentStateMachine.statesByID.ContainsKey(sourceId))
                    {
                        source = currentStateMachine.statesByID[sourceId];
                    }
                    target = currentStateMachine.anyState;
                }
                else
                {
                    // 標準情況解析
                    string[] stateIds = connectionKey.Split('_');
                    if (stateIds.Length == 2 &&
                        currentStateMachine.statesByID.ContainsKey(stateIds[0]) &&
                        currentStateMachine.statesByID.ContainsKey(stateIds[1]))
                    {
                        source = currentStateMachine.statesByID[stateIds[0]];
                        target = currentStateMachine.statesByID[stateIds[1]];
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

            // 處理點擊候選項
            HandleTransitionClicks();
        }

        // 修改繪製轉換的方法 - 不要立即處理點擊事件
        private void DrawTransitionWithOffset(StateDefinition sourceState, StateDefinition targetState,
                                             TransitionDefinition transition, int index, int total)
        {
            bool isSelected = selectedTransition == transition;
            Color lineColor = isSelected ? Color.yellow : transition.lineColor;

            Handles.BeginGUI();
            Handles.color = lineColor;

            // 獲取節點的中心位置
            Vector2 sourceCenter = GetStateCenter(sourceState);
            Vector2 targetCenter = GetStateCenter(targetState);

            // 計算方向向量
            Vector2 direction = (targetCenter - sourceCenter).normalized;

            // 檢查是否存在相反方向的連接
            int connectionCount;
            int reverseIndex;
            bool hasReverseConnection = HasReverseConnection(sourceState, targetState, out connectionCount, out reverseIndex);

            // 保存實際的起點和終點，用於後續點擊計算
            Vector2 actualStartPos, actualEndPos;
            float offset;

            // 如果是自環，使用不同的繪製方法
            if (sourceState == targetState)
            {
                DrawSelfTransition(sourceState, transition, index, total);
                // 自環的點擊區域在DrawSelfTransition中處理
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
                Vector2 baseStartPos = GetConnectionPoint(sourceState, direction, true);
                Vector2 baseEndPos = GetConnectionPoint(targetState, -direction, false);

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
            // 不再立即處理點擊，而是將候選項添加到集合中
            float clickArea = 15f + Mathf.Abs(index) * 5f; // 增加點擊區域與偏移成比例
            Rect clickRect = GetLineClickRect(actualStartPos, actualEndPos, clickArea, offset != 0);

            // 計算到滑鼠位置的距離（用於後續選擇最近的轉換）
            float distanceToMouse = 0;
            Event e = Event.current;
            if (e.type == EventType.MouseDown && clickRect.Contains(e.mousePosition))
            {
                // 計算點擊位置到線段的距離
                distanceToMouse = DistancePointToLine(e.mousePosition, actualStartPos, actualEndPos);

                // 添加到候選項列表
                TransitionClickCandidate candidate = new TransitionClickCandidate
                {
                    transition = transition,
                    distance = distanceToMouse,
                    clickRect = clickRect
                };

                // 添加到候選列表
                AddTransitionClickCandidate(candidate);
            }
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

        // 轉換點擊候選項列表
        private List<TransitionClickCandidate> transitionClickCandidates = new List<TransitionClickCandidate>();

        // 清除候選項列表
        private void ClearTransitionClickCandidates()
        {
            transitionClickCandidates.Clear();
        }

        private void AddTransitionClickCandidate(TransitionClickCandidate candidate)
        {
            transitionClickCandidates.Add(candidate);
        }

        // 處理轉換點擊選擇
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
                        SelectTransition(closest.transition);
                        e.Use();
                    }
                    else if (e.button == 1)
                    {
                        ShowTransitionContextMenu(closest.transition);
                        e.Use();
                    }
                }
            }

            // 處理完後清除候選項
            ClearTransitionClickCandidates();
        }

        // 修改自環繪製方法以支持多個自環，並處理點擊事件
        private void DrawSelfTransition(StateDefinition state, TransitionDefinition transition, int index, int total)
        {
            Vector2 center = GetStateCenter(state);
            float nodeWidth = NODE_WIDTH;
            float nodeHeight = (state == currentStateMachine.anyState) ? ANY_STATE_NODE_HEIGHT : NODE_HEIGHT;

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
            Color lineColor = selectedTransition == transition ? Color.yellow : transition.lineColor;
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

        private Vector2 GetStateCenter(StateDefinition state)
        {
            float height = (state == currentStateMachine.anyState) ? ANY_STATE_NODE_HEIGHT : NODE_HEIGHT;
            return new Vector2(
                state.editorPosition.x + NODE_WIDTH / 2,
                state.editorPosition.y + height / 2
            );
        }

        // 根據方向獲取節點邊緣的連接點
        private Vector2 GetConnectionPoint(StateDefinition state, Vector2 direction, bool isSource)
        {
            float nodeWidth = NODE_WIDTH;
            float nodeHeight = (state == currentStateMachine.anyState) ? ANY_STATE_NODE_HEIGHT : NODE_HEIGHT;
            Vector2 center = GetStateCenter(state);

            // 精確計算連接點，考慮節點的實際邊界
            // 使用數學方式找出方向向量與矩形邊界的交點

            // 將方向向量標準化，確保長度為1
            direction = direction.normalized;

            // 計算連接點
            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);

            // 水平和垂直半尺寸
            float halfWidth = nodeWidth / 2f;
            float halfHeight = nodeHeight / 2f;

            Vector2 connectionPoint;

            // 節點頂部、底部、左側和右側的中點坐標
            Vector2 topCenter = new Vector2(center.x, center.y - halfHeight);
            Vector2 bottomCenter = new Vector2(center.x, center.y + halfHeight);
            Vector2 leftCenter = new Vector2(center.x - halfWidth, center.y);
            Vector2 rightCenter = new Vector2(center.x + halfWidth, center.y);

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

        private void DrawTransitionCreationLine()
        {
            if (transitionSourceState == null) return;

            Vector2 sourceCenter = GetStateCenter(transitionSourceState);
            Vector2 mousePos = Event.current.mousePosition;

            // 計算從源節點到鼠標的方向
            Vector2 direction = (mousePos - sourceCenter).normalized;

            // 獲取源節點的連接點
            Vector2 startPos = GetConnectionPoint(transitionSourceState, direction, true);

            Handles.BeginGUI();
            Handles.color = Color.white;
            Handles.DrawLine(startPos, mousePos, 2f);
            Handles.EndGUI();

            Repaint();
        }

        private void DrawConditionLabel(Vector2 position, TransitionDefinition transition)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;

            // 確定要顯示的文字
            string conditionText = transition.conditions.Count > 1
                ? $"{transition.conditions.Count} conditions"
                : transition.conditions.Count == 1
                    ? transition.conditions[0].conditionName
                    : "No conditions";

            // 計算文字大小
            Vector2 textSize = style.CalcSize(new GUIContent(conditionText));
            float width = Mathf.Max(textSize.x + 20, 80);
            float height = textSize.y + 10;

            // 創建標籤矩形
            Rect labelRect = new Rect(position.x - width / 2, position.y - height / 2, width, height);

            // 繪製帶有背景的標籤
            Color bgColor = transition.lineColor;
            bgColor.a = 0.8f;

            Color originalColor = GUI.color;
            GUI.color = bgColor;
            GUI.Box(labelRect, "", EditorStyles.helpBox);

            GUI.color = originalColor;
            GUI.Label(labelRect, conditionText, style);
        }

        // 修改線條點擊區域計算方法
        private Rect GetLineClickRect(Vector2 start, Vector2 end, float thickness, bool isCurve)
        {
            // 計算包含兩點的最小矩形，加上邊距
            Vector2 min = Vector2.Min(start, end);
            Vector2 max = Vector2.Max(start, end);

            // 線段中點
            Vector2 midPoint = (start + end) * 0.5f;

            // 擴大矩形以確保覆蓋曲線
            Vector2 diff = max - min;
            float diagonalLength = diff.magnitude;

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

        private void HandleDragEvents()
        {
            Event e = Event.current;

            switch (e.type)
            {
                case EventType.MouseDrag:
                    if (isDragging && draggedState != null)
                    {
                        // 正確計算拖曳後的位置，考慮滾動位置的影響
                        draggedState.editorPosition = e.mousePosition - dragOffset;

                        // 如果使用了滾動視圖，需要調整偏移量
                        draggedState.editorPosition += graphScrollPosition;

                        Repaint();
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (isDragging)
                    {
                        isDragging = false;
                        draggedState = null;
                        e.Use();
                    }

                    if (isCreatingTransition && e.button == 1)
                    {
                        isCreatingTransition = false;
                        transitionSourceState = null;
                        e.Use();
                    }
                    break;

                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Escape)
                    {
                        if (isCreatingTransition)
                        {
                            isCreatingTransition = false;
                            transitionSourceState = null;
                            Repaint();
                            e.Use();
                        }
                    }
                    break;
            }
        }

        private void HandleContextMenu(Rect graphRect)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 1 && graphRect.Contains(e.mousePosition))
            {
                if (!isCreatingTransition)
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Add State"), false, () =>
                    {
                        AddNewStateAt(e.mousePosition);
                    });

                    if (currentStateMachine.anyState == null)
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

        private void ShowStateContextMenu(StateDefinition state)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Delete State"), false, () =>
            {
                DeleteState(state);
            });

            if (state != currentStateMachine.anyState)
            {
                menu.AddItem(new GUIContent("Set as Default"), false, () =>
                {
                    currentStateMachine.defaultState = state;
                });
            }

            menu.AddItem(new GUIContent("Start Transition"), false, () =>
            {
                isCreatingTransition = true;
                transitionSourceState = state;
            });

            menu.ShowAsContext();
        }

        private void ShowTransitionContextMenu(TransitionDefinition transition)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Delete Transition"), false, () =>
            {
                DeleteTransition(transition);
            });

            menu.ShowAsContext();
        }

        private void CreateNewStateMachine()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create State Machine",
                "NewStateMachine",
                "asset",
                "Create a new state machine asset",
                GetLastDirectory()
            );

            if (string.IsNullOrEmpty(path)) return;

            SetLastDirectory(path);

            StateMachineDefinition newStateMachine = CreateInstance<StateMachineDefinition>();
            newStateMachine.stateMachineName = System.IO.Path.GetFileNameWithoutExtension(path);

            SaveStateMachine();

            currentStateMachine = newStateMachine;
            selectedState = null;
            selectedTransition = null;
        }

        private string GetLastDirectory()
        {
            string path = EditorPrefs.GetString("LastStateMachineDirectory", "Assets");
            return path;
        }

        private void SetLastDirectory(string path)
        {
            EditorPrefs.SetString("LastStateMachineDirectory", path);
        }

        private void LoadStateMachine()
        {
            string path = EditorUtility.OpenFilePanel(
                "Load State Machine",
                GetLastDirectory(),
                "asset"
            );

            if (string.IsNullOrEmpty(path)) return;

            SetLastDirectory(path);

            path = "Assets" + path.Substring(Application.dataPath.Length);

            StateMachineDefinition loadedStateMachine = AssetDatabase.LoadAssetAtPath<StateMachineDefinition>(path);

            if (loadedStateMachine != null)
            {
                currentStateMachine = loadedStateMachine;
                currentStateMachine.OnValidate();
                selectedState = null;
                selectedTransition = null;
            }
        }

        private void SaveStateMachine()
        {
            EditorUtility.SetDirty(currentStateMachine);
            AssetDatabase.SaveAssets();
        }

        private void AddNewState()
        {
            AddNewStateAt(new Vector2(200, 200));
        }

        private void AddNewStateAt(Vector2 position)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create State",
                "NewState",
                "asset",
                "Create a new state asset",
                GetLastDirectory()
            );

            if (string.IsNullOrEmpty(path)) return;

            SetLastDirectory(path);

            StateDefinition newState = CreateInstance<StateDefinition>();
            newState.stateID = Guid.NewGuid().ToString();
            newState.stateName = System.IO.Path.GetFileNameWithoutExtension(path);
            newState.editorPosition = position;

            AssetDatabase.CreateAsset(newState, path);
            AssetDatabase.SaveAssets();

            currentStateMachine.states.Add(newState);
            currentStateMachine.statesByID[newState.stateID] = newState;

            if (currentStateMachine.defaultState == null)
            {
                currentStateMachine.defaultState = newState;
            }

            SaveStateMachine();

            SelectState(newState);
        }

        private void AddAnyState()
        {
            AddAnyStateAt(new Vector2(50, 50));
        }

        private void AddAnyStateAt(Vector2 position)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Any State",
                "AnyState",
                "asset",
                "Create the Any State asset",
                GetLastDirectory()
            );

            if (string.IsNullOrEmpty(path)) return;

            SetLastDirectory(path);

            StateDefinition anyState = CreateInstance<StateDefinition>();
            anyState.stateID = "AnyState";
            anyState.stateName = "AnyState";
            anyState.editorPosition = position;

            AssetDatabase.CreateAsset(anyState, path);
            AssetDatabase.SaveAssets();

            currentStateMachine.anyState = anyState;

            SaveStateMachine();

            SelectState(anyState);
        }

        private void DeleteState(StateDefinition state)
        {
            if (state == currentStateMachine.anyState)
            {
                // Delete all transitions and conditions from AnyState before removing it
                if (currentStateMachine.anyState != null)
                {
                    // Delete all transitions and their conditions from the AnyState
                    for (int i = currentStateMachine.anyState.transitions.Count - 1; i >= 0; i--)
                    {
                        DeleteTransition(currentStateMachine.anyState.transitions[i]);
                    }
                }

                currentStateMachine.anyState = null;
            }
            else
            {
                // Delete all transitions and conditions from this state before removing it
                for (int i = state.transitions.Count - 1; i >= 0; i--)
                {
                    DeleteTransition(state.transitions[i]);
                }

                currentStateMachine.states.Remove(state);
                currentStateMachine.statesByID.Remove(state.stateID);

                if (currentStateMachine.defaultState == state)
                {
                    currentStateMachine.defaultState = currentStateMachine.states.Count > 0 ? currentStateMachine.states[0] : null;
                }

                // Remove transitions from other states that point to this state
                foreach (var otherState in currentStateMachine.states)
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
                if (currentStateMachine.anyState != null)
                {
                    for (int i = currentStateMachine.anyState.transitions.Count - 1; i >= 0; i--)
                    {
                        if (currentStateMachine.anyState.transitions[i].targetStateID == state.stateID)
                        {
                            DeleteTransition(currentStateMachine.anyState.transitions[i]);
                        }
                    }
                }
            }

            if (selectedState == state)
            {
                selectedState = null;
            }

            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(state));

            SaveStateMachine();
        }

        private void FinishTransitionCreation(StateDefinition targetState)
        {
            if (transitionSourceState == targetState)
            {
                isCreatingTransition = false;
                transitionSourceState = null;
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Create Transition",
                $"Transition_{transitionSourceState.stateName}_to_{targetState.stateName}",
                "asset",
                "Create a new transition asset",
                GetLastDirectory()
            );

            if (string.IsNullOrEmpty(path)) return;

            SetLastDirectory(path);

            TransitionDefinition newTransition = CreateInstance<TransitionDefinition>();
            newTransition.transitionID = Guid.NewGuid().ToString();
            newTransition.targetStateID = targetState.stateID;
            newTransition.lineColor = new Color(0.8f, 0.8f, 0.8f, 1f);

            AssetDatabase.CreateAsset(newTransition, path);
            AssetDatabase.SaveAssets();

            transitionSourceState.transitions.Add(newTransition);

            isCreatingTransition = false;
            transitionSourceState = null;

            SaveStateMachine();

            SelectTransition(newTransition);
        }

        private void CreateBehaviourForState(StateDefinition state, string behaviourType)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Behaviour",
                $"{state.stateName}_{behaviourType}Behaviour",
                "asset",
                $"Create a new {behaviourType} behaviour asset",
                GetLastDirectory()
            );

            if (string.IsNullOrEmpty(path)) return;

            SetLastDirectory(path);

            StateBehaviourDefinition newBehaviour = CreateInstance<StateBehaviourDefinition>();
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
        }

        private void AddConditionToTransition(TransitionDefinition transition)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Condition",
                $"Condition_{transition.transitionID}",
                "asset",
                "Create a new condition asset",
                GetLastDirectory()
            );

            if (string.IsNullOrEmpty(path)) return;

            SetLastDirectory(path);

            ConditionDefinition newCondition = CreateInstance<ConditionDefinition>();
            newCondition.conditionID = Guid.NewGuid().ToString();
            newCondition.conditionName = "New Condition";

            AssetDatabase.CreateAsset(newCondition, path);
            AssetDatabase.SaveAssets();

            transition.conditions.Add(newCondition);
            EditorUtility.SetDirty(transition);
            AssetDatabase.SaveAssets();
            Repaint();

            SelectCondition(newCondition);
        }

        private void SelectState(StateDefinition state)
        {
            // Clear focus when selecting a different state
            GUI.FocusControl(null);
            
            selectedState = state;
            selectedTransition = null;
            Repaint();
        }

        private void SelectTransition(TransitionDefinition transition)
        {
            // Clear focus when selecting a different transition
            GUI.FocusControl(null);

            selectedState = null;
            selectedTransition = transition;
            Repaint();
        }

        private void SelectCondition(ConditionDefinition condition)
        {
            Selection.activeObject = condition;
        }

        private void RemoveTransitionInStateWithIndex(StateDefinition state, int index)
        {
            TransitionDefinition transition = state.transitions[index];
            state.transitions.RemoveAt(index);

            if (selectedTransition == transition)
            {
                selectedTransition = null;
            }

            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(transition));
            SaveStateMachine();
        }

        private void RemoveCondition(TransitionDefinition transition, int index)
        {
            ConditionDefinition condition = transition.conditions[index];
            transition.conditions.RemoveAt(index);

            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(condition));
            EditorUtility.SetDirty(transition);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        private void DeleteTransition(TransitionDefinition transition)
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

            // Clear the conditions list
            transition.conditions.Clear();

            // Remove the transition from all states
            foreach (var state in currentStateMachine.states)
            {
                if (state != null)
                {
                    state.transitions.Remove(transition);
                }
            }

            if (currentStateMachine.anyState != null)
            {
                currentStateMachine.anyState.transitions.Remove(transition);
            }

            if (selectedTransition == transition)
            {
                selectedTransition = null;
            }

            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(transition));

            SaveStateMachine();
        }
    }
}
