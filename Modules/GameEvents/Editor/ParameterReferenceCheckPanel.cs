using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    /// <summary>
    /// 參數表分頁上方的「檢查缺少的鍵」：列出引用不存在參數鍵的事件與場景、prefab 物件，
    /// 每一處都能直接定位；每個鍵可以在同一頁的參數表搜尋它或相近的鍵。
    /// </summary>
    internal sealed class ParameterReferenceCheckPanel
    {
        private sealed class KeyGroup
        {
            public string Key;
            public IReadOnlyList<ParameterReferenceIssue> Issues;
            public IReadOnlyList<string> Suggestions;
        }

        private readonly Action<string> search;
        private readonly Action repaint;
        private IReadOnlyList<KeyGroup> groups;
        private IReadOnlyList<string> warnings = Array.Empty<string>();
        private string checkedAt;
        private string statusMessage;
        private MessageType statusType;
        private bool expanded = true;

        /// <param name="search">在參數表搜尋一個鍵。</param>
        /// <param name="repaint">重畫所在的視窗。</param>
        public ParameterReferenceCheckPanel(Action<string> search, Action repaint)
        {
            this.search = search ?? throw new ArgumentNullException(nameof(search));
            this.repaint = repaint ?? throw new ArgumentNullException(nameof(repaint));
        }

        public void Draw()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string title = groups == null || groups.Count == 0
                        ? "缺少的參數鍵"
                        : $"缺少的參數鍵（{groups.Count} 個鍵，" +
                          $"{groups.Sum(group => group.Issues.Count)} 處）";
                    expanded = EditorGUILayout.Foldout(expanded, title, toggleOnLabelClick: true);
                    GUILayout.FlexibleSpace();
                    if (checkedAt != null)
                    {
                        GUILayout.Label("上次檢查：" + checkedAt, EditorStyles.miniLabel);
                    }

                    using (new EditorGUI.DisabledScope(
                               EditorApplication.isPlayingOrWillChangePlaymode))
                    {
                        if (GUILayout.Button(
                                new GUIContent(
                                    "檢查",
                                    "掃描所有事件目錄的事件，以及所有場景與 prefab 的公式欄位" +
                                    "（例如 ParameterStateBinder 的條件）。"),
                                GUILayout.Width(60f)))
                        {
                            // 檢查會開關場景，不能在 OnGUI 中途改變 Hierarchy。
                            EditorApplication.delayCall += Check;
                            expanded = true;
                        }
                    }
                }

                if (!expanded)
                {
                    return;
                }

                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorGUILayout.HelpBox("Play 模式中無法檢查。", MessageType.Info);
                }

                if (!string.IsNullOrEmpty(statusMessage))
                {
                    EditorGUILayout.HelpBox(statusMessage, statusType);
                }

                if (groups == null)
                {
                    EditorGUILayout.LabelField(
                        "按「檢查」找出事件、場景與 prefab 中引用了不存在參數鍵的地方。",
                        EditorStyles.wordWrappedMiniLabel);
                    return;
                }

                foreach (string warning in warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }

                if (groups.Count == 0)
                {
                    EditorGUILayout.HelpBox("沒有引用不存在參數鍵的地方。", MessageType.Info);
                }

                foreach (KeyGroup group in groups)
                {
                    DrawGroup(group);
                }
            }
        }

        private void Check()
        {
            try
            {
                ParameterReferenceReport report = GameEventParameterReferenceChecker.CheckProject();
                groups = report.Issues
                    .GroupBy(issue => issue.Key, StringComparer.Ordinal)
                    .OrderBy(group => group.Key, StringComparer.Ordinal)
                    .Select(group => new KeyGroup
                    {
                        Key = group.Key,
                        Issues = group
                            .OrderBy(issue => issue.EventAsset != null ? 0 : 1)
                            .ThenBy(issue => issue.AssetPath, StringComparer.Ordinal)
                            .ThenBy(issue => issue.Description, StringComparer.Ordinal)
                            .ToArray(),
                        Suggestions = GameEventParameterReferenceChecker.SuggestKeys(
                            group.Key,
                            report.DefinedKeys)
                    })
                    .ToArray();
                warnings = report.Warnings;
                checkedAt = DateTime.Now.ToString("HH:mm:ss");
                SetStatus(null, MessageType.None);
            }
            catch (Exception exception)
            {
                SetStatus("檢查失敗：" + exception.Message, MessageType.Error);
            }
        }

        private void DrawGroup(KeyGroup group)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("$" + group.Key, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("複製鍵", EditorStyles.miniButton, GUILayout.Width(56f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = group.Key;
                        SetStatus($"已複製「{group.Key}」。", MessageType.Info);
                    }
                }

                if (group.Suggestions.Count > 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(
                            "相近的鍵：",
                            EditorStyles.miniLabel,
                            GUILayout.ExpandWidth(false));
                        foreach (string suggestion in group.Suggestions)
                        {
                            if (GUILayout.Button(
                                    new GUIContent(suggestion, "在下方參數表搜尋這個鍵"),
                                    EditorStyles.miniButton,
                                    GUILayout.ExpandWidth(false)))
                            {
                                search(suggestion);
                            }
                        }

                        GUILayout.FlexibleSpace();
                    }
                }

                foreach (ParameterReferenceIssue issue in group.Issues)
                {
                    DrawIssue(issue);
                }
            }
        }

        private void DrawIssue(ParameterReferenceIssue issue)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label(issue.Description, EditorStyles.wordWrappedLabel);
                    GUILayout.Label(issue.AssetPath, EditorStyles.miniLabel);
                }

                string label = issue.EventAsset != null ? "開啟事件" : "定位";
                if (GUILayout.Button(label, GUILayout.Width(72f)))
                {
                    // 定位可能開啟場景或進入 Prefab Mode，延到 OnGUI 之後。
                    EditorApplication.delayCall += () => Locate(issue);
                }
            }
        }

        private void Locate(ParameterReferenceIssue issue)
        {
            try
            {
                SetStatus(GameEventParameterReferenceChecker.Locate(issue), MessageType.Warning);
            }
            catch (Exception exception)
            {
                SetStatus("無法定位：" + exception.Message, MessageType.Error);
            }
        }

        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
            repaint();
        }
    }
}
