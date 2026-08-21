using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KahaGameCore.Parameters;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    internal static class GameEventConditionGui
    {
        private static readonly string[] NumericOperatorLabels =
            { "等於", "不等於", "大於", "大於或等於", "小於", "小於或等於" };
        private static readonly string[] NumericOperatorSymbols =
            { "==", "!=", ">", ">=", "<", "<=" };
        private static readonly string[] BoolOperatorLabels = { "為真", "為假" };
        private static readonly ParameterType[] ConditionParameterTypes =
            { ParameterType.Int, ParameterType.Float, ParameterType.Bool };

        public static bool IsSupportedParameterType(ParameterType type)
        {
            return type == ParameterType.Int ||
                type == ParameterType.Float ||
                type == ParameterType.Bool;
        }

        public static GameEventConditionGroupDraft CreateDefaultRoot(
            ParameterDefinition parameter)
        {
            GameEventConditionGroupDraft root = new GameEventConditionGroupDraft();
            root.Children.Add(CreateDefaultCondition(parameter));
            return root;
        }

        public static bool DrawGroup(
            GameEventConditionGroupDraft group,
            IReadOnlyList<ParameterAuthoringEntry> parameters,
            Func<GameEventProjectAuthoringCatalog> getCatalog,
            bool isRoot,
            string emptyMessage,
            Action<Rect, IReadOnlyList<ParameterType>, Action<string>>
                showCreateParameter,
            Action onAsyncChanged,
            out bool removeRequested)
        {
            bool changed = false;
            removeRequested = false;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(
                        isRoot ? "主要條件群組" : "條件群組",
                        EditorStyles.boldLabel,
                        GUILayout.Width(110f));
                    EditorGUI.BeginChangeCheck();
                    group.Mode = (GameEventConditionGroupMode)EditorGUILayout.Popup(
                        (int)group.Mode,
                        new[] { "全部成立（AND）", "任一成立（OR）" });
                    changed |= EditorGUI.EndChangeCheck();

                    if (!isRoot && GUILayout.Button("×", GUILayout.Width(28f)))
                    {
                        removeRequested = true;
                        return true;
                    }
                }

                int removeIndex = -1;
                for (int index = 0; index < group.Children.Count; index++)
                {
                    GameEventConditionDraft child = group.Children[index];
                    if (child is GameEventConditionClauseDraft clause)
                    {
                        changed |= DrawClause(
                            clause,
                            parameters,
                            getCatalog,
                            index,
                            showCreateParameter,
                            onAsyncChanged,
                            out bool removeClause);
                        if (removeClause)
                        {
                            removeIndex = index;
                        }
                    }
                    else if (child is GameEventConditionGroupDraft childGroup)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(18f);
                            changed |= DrawGroup(
                                childGroup,
                                parameters,
                                getCatalog,
                                false,
                                emptyMessage,
                                showCreateParameter,
                                onAsyncChanged,
                                out bool removeGroup);
                            if (removeGroup)
                            {
                                removeIndex = index;
                            }
                        }
                    }
                }

                if (removeIndex >= 0)
                {
                    group.Children.RemoveAt(removeIndex);
                    changed = true;
                }

                if (group.Children.Count == 0)
                {
                    if (isRoot)
                    {
                        EditorGUILayout.HelpBox(emptyMessage, MessageType.Info);
                    }
                    else
                    {
                        removeRequested = true;
                        return true;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("＋ 新增條件", GUILayout.Width(110f)))
                    {
                        group.Children.Add(
                            CreateDefaultCondition(parameters[0].Definition));
                        changed = true;
                    }

                    if (GUILayout.Button("＋ 新增群組", GUILayout.Width(100f)))
                    {
                        GameEventConditionGroupDraft childGroup =
                            new GameEventConditionGroupDraft();
                        childGroup.Children.Add(
                            CreateDefaultCondition(parameters[0].Definition));
                        group.Children.Add(childGroup);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static bool DrawClause(
            GameEventConditionClauseDraft draft,
            IReadOnlyList<ParameterAuthoringEntry> parameters,
            Func<GameEventProjectAuthoringCatalog> getCatalog,
            int index,
            Action<Rect, IReadOnlyList<ParameterType>, Action<string>>
                showCreateParameter,
            Action onAsyncChanged,
            out bool removeRequested)
        {
            removeRequested = false;
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"#{index + 1}", GUILayout.Width(28f));
                    DrawParameterKeyDropdown(
                        draft.ParameterKey,
                        parameters,
                        getCatalog(),
                        selectedKey =>
                        {
                            if (string.Equals(
                                    draft.ParameterKey,
                                    selectedKey,
                                    StringComparison.Ordinal))
                            {
                                return;
                            }

                            draft.ParameterKey = selectedKey;
                            ResetConditionForParameter(
                                draft,
                                getCatalog().ParameterEntries);
                            onAsyncChanged();
                        },
                        showCreateParameter);

                    if (GUILayout.Button("×", GUILayout.Width(28f)))
                    {
                        removeRequested = true;
                    }
                }

                if (getCatalog().TryGetParameter(
                    draft.ParameterKey,
                    out ParameterDefinition selectedParameter))
                {
                    DrawConditionComparison(draft, selectedParameter);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        $"找不到參數「{draft.ParameterKey}」。",
                        MessageType.Error);
                }
            }

            return EditorGUI.EndChangeCheck() || removeRequested;
        }

        private static void DrawParameterKeyDropdown(
            string currentValue,
            IReadOnlyList<ParameterAuthoringEntry> entries,
            GameEventProjectAuthoringCatalog catalog,
            Action<string> onSelected,
            Action<Rect, IReadOnlyList<ParameterType>, Action<string>>
                showCreateParameter)
        {
            Rect buttonRect = EditorGUILayout.GetControlRect();
            GUIContent content;
            if (catalog.TryGetParameterEntry(
                    currentValue,
                    out ParameterAuthoringEntry currentEntry))
            {
                content = new GUIContent(
                    currentEntry.TableDisplayName + " / " +
                    ParameterKeyAdvancedDropdown.FormatEntryLabel(currentEntry),
                    currentEntry.AssetPath);
            }
            else
            {
                content = string.IsNullOrWhiteSpace(currentValue)
                    ? new GUIContent("選擇或新增參數…")
                    : new GUIContent($"遺失／{currentValue}");
            }

            if (!EditorGUI.DropdownButton(
                    buttonRect,
                    content,
                    FocusType.Keyboard))
            {
                return;
            }

            ParameterKeyAdvancedDropdown dropdown =
                new ParameterKeyAdvancedDropdown(
                    new AdvancedDropdownState(),
                    entries,
                    onSelected,
                    () => showCreateParameter(
                        buttonRect,
                        ConditionParameterTypes,
                        onSelected));
            dropdown.Show(buttonRect);
        }

        private static void DrawConditionComparison(
            GameEventConditionClauseDraft draft,
            ParameterDefinition parameter)
        {
            if (parameter.Type == ParameterType.Bool)
            {
                bool expected = GetExpectedBoolean(draft);
                int selectedIndex = expected ? 0 : 1;
                selectedIndex = EditorGUILayout.Popup(
                    "狀態",
                    selectedIndex,
                    BoolOperatorLabels);
                draft.Operator = "==";
                draft.Value = selectedIndex == 0 ? "true" : "false";
                return;
            }

            int operatorIndex = Array.IndexOf(
                NumericOperatorSymbols,
                draft.Operator ?? string.Empty);
            operatorIndex = Mathf.Max(0, operatorIndex);
            operatorIndex = EditorGUILayout.Popup(
                "比較方式",
                operatorIndex,
                NumericOperatorLabels);
            draft.Operator = NumericOperatorSymbols[operatorIndex];

            if (parameter.Type == ParameterType.Int)
            {
                int.TryParse(
                    draft.Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int value);
                value = EditorGUILayout.IntField("比較值", value);
                draft.Value = value.ToString(CultureInfo.InvariantCulture);
                return;
            }

            float.TryParse(
                draft.Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float floatValue);
            floatValue = EditorGUILayout.FloatField("比較值", floatValue);
            draft.Value = floatValue.ToString("R", CultureInfo.InvariantCulture);
        }

        private static bool GetExpectedBoolean(GameEventConditionClauseDraft draft)
        {
            bool value = string.Equals(
                draft.Value,
                "true",
                StringComparison.OrdinalIgnoreCase);
            return draft.Operator == "!=" ? !value : value;
        }

        private static void ResetConditionForParameter(
            GameEventConditionClauseDraft draft,
            IReadOnlyList<ParameterAuthoringEntry> parameters)
        {
            ParameterDefinition parameter = parameters
                .First(candidate => candidate.Definition.Key == draft.ParameterKey)
                .Definition;
            draft.Operator = "==";
            draft.Value = parameter.Type == ParameterType.Bool ? "true" : "0";
        }

        private static GameEventConditionClauseDraft CreateDefaultCondition(
            ParameterDefinition parameter)
        {
            return new GameEventConditionClauseDraft
            {
                ParameterKey = parameter.Key,
                Operator = "==",
                Value = parameter.Type == ParameterType.Bool ? "true" : "0"
            };
        }
    }
}
