using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.FontLocalization.Editor
{
    internal sealed class TmpFontTargetScannerWindow : EditorWindow
    {
        private readonly List<TmpFontTargetScanResult> results = new();
        private Vector2 resultScroll;
        private Vector2 ignoredScroll;
        private bool showIgnored;
        private string status = "尚未掃描。";

        [MenuItem("KahaGameCore/Font Localization/TMP Font Target Scanner")]
        private static void OpenWindow()
        {
            var window = GetWindow<TmpFontTargetScannerWindow>();
            window.titleContent = new GUIContent("TMP Font Scanner");
            window.minSize = new Vector2(760f, 420f);
            window.Show();
            window.Scan();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("TMP Font Target Scanner", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "掃描 Assets 內所有 Scene 與 Prefab。已忽略項目不會出現在掃描結果，Build 前也使用相同規則。",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("重新掃描", GUILayout.Width(100f)))
                {
                    Scan();
                }

                using (new EditorGUI.DisabledScope(results.Count == 0))
                {
                    if (GUILayout.Button("一鍵新增", GUILayout.Width(100f)))
                    {
                        AddAll();
                    }

                    if (GUILayout.Button("一鍵忽略", GUILayout.Width(100f)))
                    {
                        IgnoreAll();
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(status, EditorStyles.miniLabel, GUILayout.Width(320f));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"缺少 LocalizedFontTarget：{results.Count}", EditorStyles.boldLabel);

            resultScroll = EditorGUILayout.BeginScrollView(resultScroll);
            foreach (TmpFontTargetScanResult result in results.ToArray())
            {
                DrawResult(result);
            }
            EditorGUILayout.EndScrollView();

            showIgnored = EditorGUILayout.Foldout(
                showIgnored,
                $"已忽略：{TmpFontTargetIgnoreRegistry.instance.Entries.Count}",
                true);
            if (showIgnored)
            {
                DrawIgnoredEntries();
            }
        }

        private void DrawResult(TmpFontTargetScanResult result)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(result.AssetPath, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(result.HierarchyPath, EditorStyles.wordWrappedMiniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("定位", GUILayout.Width(70f)))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(result.AssetPath);
                        EditorGUIUtility.PingObject(Selection.activeObject);
                    }

                    if (GUILayout.Button("新增", GUILayout.Width(70f)))
                    {
                        Add(new[] { result });
                    }

                    if (GUILayout.Button("忽略", GUILayout.Width(70f)))
                    {
                        TmpFontTargetIgnoreRegistry.instance.Ignore(new[] { result });
                        Scan();
                    }
                }
            }
        }

        private void DrawIgnoredEntries()
        {
            ignoredScroll = EditorGUILayout.BeginScrollView(
                ignoredScroll,
                GUILayout.MaxHeight(180f));
            foreach (TmpFontTargetIgnoredEntry entry in
                     TmpFontTargetIgnoreRegistry.instance.Entries.ToArray())
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        $"{entry.AssetPath}\n{entry.HierarchyPath}",
                        EditorStyles.wordWrappedMiniLabel);
                    if (GUILayout.Button("取消忽略", GUILayout.Width(80f)))
                    {
                        TmpFontTargetIgnoreRegistry.instance.Remove(entry.StableId);
                        Scan();
                        GUIUtility.ExitGUI();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void Scan()
        {
            results.Clear();
            results.AddRange(TmpFontTargetScanner.ScanAll(
                TmpFontTargetIgnoreRegistry.instance,
                true));
            status = results.Count == 0
                ? "掃描完成，沒有未處理項目。"
                : $"掃描完成，找到 {results.Count} 個項目。";
            Repaint();
        }

        private void AddAll()
        {
            if (!EditorUtility.DisplayDialog(
                    "一鍵新增 LocalizedFontTarget",
                    $"即將修改 {results.Count} 個 TMP 所在的 Scene 或 Prefab。是否繼續？",
                    "新增",
                    "取消"))
            {
                return;
            }

            Add(results.ToArray());
        }

        private void IgnoreAll()
        {
            if (!EditorUtility.DisplayDialog(
                    "一鍵忽略 TMP",
                    $"即將永久記錄目前 {results.Count} 個 TMP。它們之後不會阻擋 Build，是否繼續？",
                    "忽略",
                    "取消"))
            {
                return;
            }

            TmpFontTargetIgnoreRegistry.instance.Ignore(results);
            Scan();
        }

        private void Add(IEnumerable<TmpFontTargetScanResult> targets)
        {
            TmpFontTargetMutationReport report = TmpFontTargetScanner.AddTargets(targets);
            status = $"已新增 {report.AddedCount} 個 LocalizedFontTarget。";
            if (report.Errors.Count > 0)
            {
                Debug.LogError(
                    "TMP Font Target Scanner 無法修改下列資產：\n" +
                    string.Join("\n", report.Errors));
                EditorUtility.DisplayDialog(
                    "部分資產修改失敗",
                    string.Join("\n", report.Errors.Take(8)),
                    "確定");
            }

            Scan();
        }
    }
}
