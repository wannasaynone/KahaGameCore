using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game.Flow.Editor
{
    [CustomEditor(typeof(GameFlowSequence))]
    public class GameFlowSequenceEditor : UnityEditor.Editor
    {
        private GameFlowSequence targetSequence;
        private SerializedProperty descriptionProperty;
        private SerializedProperty stepsProperty;

        private ReorderableList stepsList;
        private Vector2 scrollPosition;
        private GUIStyle headerStyle;
        private GUIStyle stepStyle;

        // Search functionality
        private string searchText = "";
        private List<int> filteredStepIndices = new List<int>();

        // Foldout states for steps
        private Dictionary<string, bool> stepFoldouts = new Dictionary<string, bool>();

        // Cached step types
        private Type[] cachedStepTypes;
        private string[] cachedStepTypeNames;

        // 标记是否需要重新初始化
        private bool needsReinitialize = false;

        private void OnEnable()
        {
            targetSequence = (GameFlowSequence)target;
            InitializeProperties();
        }

        private void InitializeProperties()
        {
            // 确保目标有效
            if (target == null)
                return;

            serializedObject.Update();
            descriptionProperty = serializedObject.FindProperty("description");
            stepsProperty = serializedObject.FindProperty("steps");

            // Initialize styles
            headerStyle = new GUIStyle();
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            headerStyle.margin = new RectOffset(5, 5, 10, 10);

            // Initialize reorderable list
            SetupReorderableList();

            // Cache step types if needed
            if (cachedStepTypes == null || cachedStepTypes.Length == 0)
            {
                CacheStepTypes();
            }

            // Initialize filtered indices
            UpdateFilteredSteps();

            // 重置标记
            needsReinitialize = false;
        }

        private void SetupReorderableList()
        {
            if (stepsList != null)
            {
                // 清理现有列表
                stepsList = null;
            }

            stepsList = new ReorderableList(
                serializedObject,
                stepsProperty,
                true, // draggable
                true, // displayHeader
                true, // displayAddButton
                true  // displayRemoveButton
            );

            // Setup the header
            stepsList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Game Flow Steps");
            };

            // Setup the element height
            stepsList.elementHeightCallback = (int index) =>
            {
                return EditorGUIUtility.singleLineHeight * 2;
            };

            // Setup the element drawing
            stepsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index < 0 || index >= stepsProperty.arraySize)
                    return;

                SerializedProperty stepProperty = stepsProperty.GetArrayElementAtIndex(index);
                DrawStepElement(rect, stepProperty, index);
            };

            // Setup the add dropdown
            stepsList.onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
            {
                GenericMenu menu = new GenericMenu();

                // Add menu items for each step type
                for (int i = 0; i < cachedStepTypes.Length; i++)
                {
                    Type stepType = cachedStepTypes[i];
                    string stepName = cachedStepTypeNames[i];

                    // 使用本地变量避免闭包问题
                    Type localStepType = stepType;
                    menu.AddItem(new GUIContent(stepName), false, () =>
                    {
                        AddNewStep(localStepType);
                    });
                }

                menu.ShowAsContext();
            };

            // Setup the remove callback
            stepsList.onRemoveCallback = (ReorderableList list) =>
            {
                RemoveStep(list.index);
            };

            // Setup the reorder callback
            stepsList.onReorderCallback = (ReorderableList list) =>
            {
                // Mark the object as dirty
                EditorUtility.SetDirty(targetSequence);
            };
        }

        private void DrawStepElement(Rect rect, SerializedProperty stepProperty, int index)
        {
            GameFlowStep step = stepProperty.objectReferenceValue as GameFlowStep;
            if (step == null)
                return;

            string stepId = stepProperty.propertyPath;

            // Ensure the step has an entry in the foldout dictionary
            if (!stepFoldouts.ContainsKey(stepId))
            {
                stepFoldouts[stepId] = false;
            }

            // Calculate rects
            Rect foldoutRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

            // Draw foldout
            stepFoldouts[stepId] = EditorGUI.Foldout(foldoutRect, stepFoldouts[stepId], $"Step {index + 1}: {step.name} ({step.GetType().Name})");

            // If expanded, draw the step details
            if (stepFoldouts[stepId])
            {
                // Create a new editor for the step
                UnityEditor.Editor stepEditor = CreateEditor(step);

                // Draw the step editor
                EditorGUI.BeginChangeCheck();
                stepEditor.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(step);
                }

                // Cleanup
                DestroyImmediate(stepEditor);
            }
        }

        public override void OnInspectorGUI()
        {
            // 检查是否需要重新初始化
            if (needsReinitialize)
            {
                InitializeProperties();
            }

            serializedObject.Update();

            // Draw header
            DrawHeader();

            DrawDescriptionField();

            // Draw search bar
            DrawSearchBar();

            // Draw steps list
            DrawStepsList();

            int nullCount = 0;
            for (int i = 0; i < targetSequence.steps.Count; i++)
            {
                if (targetSequence.steps[i] == null)
                    nullCount++;
            }

            if (nullCount > 0)
            {
                CleanNullReferences();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private new void DrawHeader()
        {
            EditorGUILayout.LabelField("Game Flow Sequence " + targetSequence.name, headerStyle);
            EditorGUILayout.Space();
        }

        private void DrawDescriptionField()
        {
            EditorGUILayout.PropertyField(descriptionProperty, new GUIContent("Description"), GUILayout.Height(60));
            EditorGUILayout.Space();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.SetNextControlName("SearchField");
            string newSearchText = EditorGUILayout.TextField("Search Steps", searchText);

            if (newSearchText != searchText)
            {
                searchText = newSearchText;
                UpdateFilteredSteps();
            }

            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                searchText = "";
                UpdateFilteredSteps();
                GUI.FocusControl("SearchField");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawStepsList()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (string.IsNullOrEmpty(searchText))
            {
                if (stepsList == null)
                {
                    SetupReorderableList();
                }

                // Show all steps using the reorderable list
                stepsList.DoLayoutList();
            }
            else
            {
                stepStyle = new GUIStyle(EditorStyles.helpBox);
                stepStyle.margin = new RectOffset(5, 5, 2, 2);
                stepStyle.padding = new RectOffset(5, 5, 5, 5);

                // Show only filtered steps
                for (int i = 0; i < filteredStepIndices.Count; i++)
                {
                    int stepIndex = filteredStepIndices[i];

                    // 安全检查
                    if (stepIndex < 0 || stepIndex >= stepsProperty.arraySize)
                        continue;

                    SerializedProperty stepProperty = stepsProperty.GetArrayElementAtIndex(stepIndex);

                    EditorGUILayout.BeginVertical(stepStyle);

                    GameFlowStep step = stepProperty.objectReferenceValue as GameFlowStep;
                    if (step != null)
                    {
                        string stepId = stepProperty.propertyPath;

                        // Ensure the step has an entry in the foldout dictionary
                        if (!stepFoldouts.ContainsKey(stepId))
                        {
                            stepFoldouts[stepId] = false;
                        }

                        // Draw step header with foldout
                        EditorGUILayout.BeginHorizontal();
                        stepFoldouts[stepId] = EditorGUILayout.Foldout(stepFoldouts[stepId], $"Step {stepIndex + 1}: {step.name}");

                        // Draw step type
                        EditorGUILayout.LabelField($"Type: {step.GetType().Name}");

                        EditorGUILayout.EndHorizontal();

                        // If expanded, draw the step details
                        if (stepFoldouts[stepId])
                        {
                            // Create a new editor for the step
                            UnityEditor.Editor stepEditor = CreateEditor(step);

                            // Draw the step editor
                            EditorGUI.BeginChangeCheck();
                            stepEditor.OnInspectorGUI();
                            if (EditorGUI.EndChangeCheck())
                            {
                                EditorUtility.SetDirty(step);
                            }

                            // Cleanup
                            DestroyImmediate(stepEditor);
                        }
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }

                // Show message if no steps found
                if (filteredStepIndices.Count == 0)
                {
                    EditorGUILayout.HelpBox("No steps matching the search criteria.", MessageType.Info);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void AddNewStep(Type stepType)
        {
            try
            {
                // Create a new step asset
                GameFlowStep newStep = CreateInstance(stepType) as GameFlowStep;
                if (newStep == null)
                    return;

                // Set default name
                newStep.name = $"New {stepType.Name}";

                // Create a unique asset path
                string path = AssetDatabase.GetAssetPath(targetSequence);
                string directory = System.IO.Path.GetDirectoryName(path);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                string newAssetPath = $"{directory}/{fileName}_{stepType.Name}_{Guid.NewGuid().ToString().Substring(0, 8)}.asset";

                // Create the asset
                AssetDatabase.CreateAsset(newStep, newAssetPath);
                AssetDatabase.SaveAssets();

                // 使用Undo记录修改，并且直接修改targetSequence而不是通过SerializedProperty
                Undo.RecordObject(targetSequence, "Add Step");

                // 获取当前步骤列表
                var steps = new List<GameFlowStep>();
                for (int i = 0; i < targetSequence.steps.Count; i++)
                {
                    steps.Add(targetSequence.steps[i]);
                }

                // 添加新步骤
                steps.Add(newStep);

                // 更新目标序列的步骤数组
                targetSequence.steps = steps;

                // 标记对象为已修改
                EditorUtility.SetDirty(targetSequence);

                // 强制保存资产
                AssetDatabase.SaveAssets();

                // 延迟一帧后刷新编辑器
                EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        // 完全重新初始化编辑器界面
                        stepsList = null;
                        serializedObject.Update();
                        stepsProperty = serializedObject.FindProperty("steps");
                        SetupReorderableList();
                        UpdateFilteredSteps();
                        Repaint();
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error adding new step: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void RemoveStep(int index)
        {
            try
            {
                if (index < 0 || index >= targetSequence.steps.Count)
                    return;

                GameFlowStep stepToRemove = targetSequence.steps[index];

                if (stepToRemove != null)
                {
                    // 请求确认
                    if (EditorUtility.DisplayDialog("Remove Step",
                        $"Are you sure you want to remove the step '{stepToRemove.name}'?",
                        "Yes", "No"))
                    {
                        // 使用Undo记录修改
                        Undo.RecordObject(targetSequence, "Remove Step");

                        // 获取资产路径
                        string assetPath = AssetDatabase.GetAssetPath(stepToRemove);

                        // 创建一个不包含要移除步骤的新数组
                        var steps = new List<GameFlowStep>();
                        for (int i = 0; i < targetSequence.steps.Count; i++)
                        {
                            if (i != index)
                            {
                                steps.Add(targetSequence.steps[i]);
                            }
                        }

                        // 更新目标序列的步骤数组
                        targetSequence.steps = steps;

                        // 标记对象为已修改
                        EditorUtility.SetDirty(targetSequence);

                        // 删除资产（如果不在其他地方使用）
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            AssetDatabase.DeleteAsset(assetPath);
                        }

                        // 强制保存资产
                        AssetDatabase.SaveAssets();

                        // 延迟一帧后刷新编辑器
                        EditorApplication.delayCall += () =>
                        {
                            if (this != null)
                            {
                                // 完全重新初始化编辑器界面
                                stepsList = null;
                                serializedObject.Update();
                                stepsProperty = serializedObject.FindProperty("steps");
                                SetupReorderableList();
                                UpdateFilteredSteps();
                                Repaint();
                            }
                        };
                    }
                }
                else
                {
                    // 如果步骤为空，仍然从列表中移除它
                    Undo.RecordObject(targetSequence, "Remove Step");

                    // 创建一个不包含要移除步骤的新数组
                    var steps = new List<GameFlowStep>();
                    for (int i = 0; i < targetSequence.steps.Count; i++)
                    {
                        if (i != index)
                        {
                            steps.Add(targetSequence.steps[i]);
                        }
                    }

                    // 更新目标序列的步骤数组
                    targetSequence.steps = steps;

                    // 标记对象为已修改
                    EditorUtility.SetDirty(targetSequence);
                    AssetDatabase.SaveAssets();

                    // 延迟一帧后刷新编辑器
                    EditorApplication.delayCall += () =>
                    {
                        if (this != null)
                        {
                            // 完全重新初始化编辑器界面
                            stepsList = null;
                            serializedObject.Update();
                            stepsProperty = serializedObject.FindProperty("steps");
                            SetupReorderableList();
                            UpdateFilteredSteps();
                            Repaint();
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error removing step: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void CacheStepTypes()
        {
            try
            {
                // Get all types that inherit from GameFlowStep
                var stepTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly =>
                    {
                        try
                        {
                            return assembly.GetTypes();
                        }
                        catch (ReflectionTypeLoadException)
                        {
                            return new Type[0]; // Return empty array if assembly can't be loaded
                        }
                    })
                    .Where(type =>
                        type != null &&
                        type.IsClass &&
                        !type.IsAbstract &&
                        typeof(GameFlowStep).IsAssignableFrom(type))
                    .ToArray();

                // Cache the types and names
                cachedStepTypes = stepTypes;
                cachedStepTypeNames = stepTypes.Select(type => type.Name).ToArray();

                // Sort alphabetically
                Array.Sort(cachedStepTypeNames, cachedStepTypes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error caching step types: {ex.Message}");
                // 提供空数组作为后备
                cachedStepTypes = new Type[0];
                cachedStepTypeNames = new string[0];
            }
        }

        private void UpdateFilteredSteps()
        {
            try
            {
                filteredStepIndices.Clear();

                if (string.IsNullOrEmpty(searchText))
                {
                    // If search text is empty, all steps are in the filter
                    for (int i = 0; i < stepsProperty.arraySize; i++)
                    {
                        filteredStepIndices.Add(i);
                    }
                }
                else
                {
                    // Case-insensitive search
                    string lowerSearchText = searchText.ToLower();

                    // Find steps that contain the search text in their name or type
                    for (int i = 0; i < stepsProperty.arraySize; i++)
                    {
                        SerializedProperty stepProperty = stepsProperty.GetArrayElementAtIndex(i);
                        GameFlowStep step = stepProperty.objectReferenceValue as GameFlowStep;

                        if (step != null)
                        {
                            if (step.name.ToLower().Contains(lowerSearchText) ||
                                step.GetType().Name.ToLower().Contains(lowerSearchText))
                            {
                                filteredStepIndices.Add(i);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating filtered steps: {ex.Message}");
                filteredStepIndices.Clear();
            }
        }

        private void CleanNullReferences()
        {
            try
            {
                // 检查是否有null引用
                bool hasNullReferences = false;
                for (int i = 0; i < targetSequence.steps.Count; i++)
                {
                    if (targetSequence.steps[i] == null)
                    {
                        hasNullReferences = true;
                        break;
                    }
                }

                // 如果没有null引用，无需处理
                if (!hasNullReferences)
                    return;

                // 创建一个不包含null引用的新数组
                var validSteps = new List<GameFlowStep>();
                for (int i = 0; i < targetSequence.steps.Count; i++)
                {
                    if (targetSequence.steps[i] != null)
                    {
                        validSteps.Add(targetSequence.steps[i]);
                    }
                }

                // 更新目标序列的步骤数组
                targetSequence.steps = validSteps;
                if (targetSequence.steps == null)
                {
                    targetSequence.steps = new List<GameFlowStep>();
                }

                // 标记对象为已修改
                EditorUtility.SetDirty(targetSequence);

                // 强制保存资产
                AssetDatabase.SaveAssets();

                // 延迟一帧后刷新编辑器
                EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        // 完全重新初始化编辑器界面
                        stepsList = null;
                        serializedObject.Update();
                        stepsProperty = serializedObject.FindProperty("steps");
                        SetupReorderableList();
                        UpdateFilteredSteps();
                        Repaint();
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"清理null引用时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}