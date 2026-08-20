using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.Parameters;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    internal sealed class CreateParameterPopupContent : PopupWindowContent
    {
        private readonly ParameterTableWorkspace workspace;
        private readonly ParameterType[] allowedTypes;
        private readonly Action<string> onCreated;
        private int tableIndex;
        private int typeIndex;
        private string key = string.Empty;
        private string displayName = string.Empty;
        private int intInitial;
        private int intMin;
        private int intMax = 100;
        private float floatInitial;
        private float floatMin;
        private float floatMax = 100f;
        private bool boolInitial;
        private string stringInitial = string.Empty;
        private string error;

        public CreateParameterPopupContent(
            ParameterTableWorkspace workspace,
            IReadOnlyList<ParameterType> allowedTypes,
            Action<string> onCreated)
        {
            this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            this.allowedTypes = (allowedTypes ?? throw new ArgumentNullException(nameof(allowedTypes)))
                .Distinct()
                .ToArray();
            this.onCreated = onCreated ?? throw new ArgumentNullException(nameof(onCreated));

            ParameterTableWorkspace.Session selected = workspace.Sessions
                .FirstOrDefault(session => session.Expanded);
            if (selected != null)
            {
                tableIndex = Math.Max(
                    0,
                    workspace.Sessions.ToList().IndexOf(selected));
            }
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(390f, 300f);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("新增參數", EditorStyles.boldLabel);
            EditorGUILayout.Space(3f);
            if (workspace.Sessions.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "目前沒有可編輯的參數表，請先在「參數表」分頁建立或加入一張表。",
                    MessageType.Warning);
                return;
            }

            string[] tableLabels = workspace.Sessions
                .Select(session =>
                    (session.Editor.IsDirty ? "* " : string.Empty) +
                    session.Editor.TableDisplayName)
                .ToArray();
            tableIndex = EditorGUILayout.Popup("目標參數表", tableIndex, tableLabels);
            string[] typeLabels = allowedTypes.Select(FormatType).ToArray();
            typeIndex = EditorGUILayout.Popup("類型", typeIndex, typeLabels);
            key = EditorGUILayout.TextField("參數鍵", key ?? string.Empty);
            displayName = EditorGUILayout.TextField("顯示名稱", displayName ?? string.Empty);
            DrawValueFields(allowedTypes[typeIndex]);

            if (!string.IsNullOrEmpty(error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("取消", GUILayout.Width(72f)))
                {
                    editorWindow.Close();
                }

                if (GUILayout.Button("新增並選取", GUILayout.Width(104f)))
                {
                    Create();
                }
            }
        }

        private void DrawValueFields(ParameterType type)
        {
            switch (type)
            {
                case ParameterType.Int:
                    intInitial = EditorGUILayout.IntField("初始值", intInitial);
                    intMin = EditorGUILayout.IntField("最小值", intMin);
                    intMax = EditorGUILayout.IntField("最大值", intMax);
                    break;
                case ParameterType.Float:
                    floatInitial = EditorGUILayout.FloatField("初始值", floatInitial);
                    floatMin = EditorGUILayout.FloatField("最小值", floatMin);
                    floatMax = EditorGUILayout.FloatField("最大值", floatMax);
                    break;
                case ParameterType.Bool:
                    boolInitial = EditorGUILayout.Toggle("初始值", boolInitial);
                    break;
                case ParameterType.String:
                    stringInitial = EditorGUILayout.TextField("初始值", stringInitial ?? string.Empty);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Create()
        {
            try
            {
                string trimmedKey = (key ?? string.Empty).Trim();
                ParameterDefinition definition = CreateDefinition(
                    allowedTypes[typeIndex],
                    trimmedKey,
                    (displayName ?? string.Empty).Trim());
                string path = workspace.Sessions[tableIndex].AssetPath;
                workspace.AddParameter(path, definition);
                onCreated(trimmedKey);
                editorWindow.Close();
            }
            catch (Exception exception)
            {
                error = exception.Message;
            }
        }

        private ParameterDefinition CreateDefinition(
            ParameterType type,
            string parameterKey,
            string parameterDisplayName)
        {
            switch (type)
            {
                case ParameterType.Int:
                    return ParameterDefinition.Int(
                        parameterKey,
                        parameterDisplayName,
                        intInitial,
                        intMin,
                        intMax);
                case ParameterType.Float:
                    return ParameterDefinition.Float(
                        parameterKey,
                        parameterDisplayName,
                        floatInitial,
                        floatMin,
                        floatMax);
                case ParameterType.Bool:
                    return ParameterDefinition.Bool(
                        parameterKey,
                        parameterDisplayName,
                        boolInitial);
                case ParameterType.String:
                    return ParameterDefinition.String(
                        parameterKey,
                        parameterDisplayName,
                        stringInitial ?? string.Empty);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string FormatType(ParameterType type)
        {
            switch (type)
            {
                case ParameterType.Int: return "整數";
                case ParameterType.Float: return "小數";
                case ParameterType.Bool: return "布林";
                case ParameterType.String: return "文字";
                default: return type.ToString();
            }
        }
    }
}
