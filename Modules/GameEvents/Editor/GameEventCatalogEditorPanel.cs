using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    internal sealed class GameEventCatalogEditorPanel
    {
        private sealed class EventInfo
        {
            public TextAsset Asset;
            public GameEventDocument Document;
            public string Error;
        }

        private sealed class TimingGroup
        {
            public string Timing;
            public List<EventInfo> Events;
            public ReorderableList List;
        }

        private readonly GameEventDocumentJsonCodec codec =
            new GameEventDocumentJsonCodec();
        private readonly List<TimingGroup> groups = new List<TimingGroup>();
        private GameEventCatalogAsset catalog;
        private TextAsset addCandidate;

        public GameEventCatalogAsset Catalog => catalog;

        public void SetCatalog(GameEventCatalogAsset value)
        {
            if (catalog == value)
            {
                return;
            }

            catalog = value;
            addCandidate = null;
            Refresh();
        }

        public void Refresh()
        {
            groups.Clear();
            if (catalog == null)
            {
                return;
            }

            Dictionary<string, TimingGroup> byTiming =
                new Dictionary<string, TimingGroup>(StringComparer.Ordinal);
            for (int index = 0; index < catalog.Files.Count; index++)
            {
                EventInfo info = ReadEvent(catalog.Files[index]);
                string timing = info.Document != null
                    ? info.Document.TriggerTiming
                    : "<Invalid>";
                if (!byTiming.TryGetValue(timing, out TimingGroup group))
                {
                    group = CreateGroup(timing);
                    byTiming.Add(timing, group);
                    groups.Add(group);
                }

                group.Events.Add(info);
            }
        }

        public void Draw(TextAsset currentEvent, Action<TextAsset> openEvent)
        {
            if (catalog == null)
            {
                EditorGUILayout.HelpBox(
                    "請先選擇或建立事件目錄。事件目錄決定執行時包含哪些事件，以及事件的執行順序。",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("目錄事件", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "事件會依觸發時機分組；拖曳群組內的項目即可調整執行順序。",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(currentEvent == null))
                    {
                        if (GUILayout.Button("加入目前事件", GUILayout.Width(120f)))
                        {
                            AddEvent(currentEvent);
                        }
                    }

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("驗證目錄", GUILayout.Width(88f)))
                    {
                        new GameEventCatalog(catalog, codec);
                        Debug.Log(
                            $"事件目錄「{catalog.name}」驗證成功，共 {catalog.Files.Count} 個事件。",
                            catalog);
                    }
                }

                addCandidate = (TextAsset)EditorGUILayout.ObjectField(
                    "選擇事件",
                    addCandidate,
                    typeof(TextAsset),
                    false);
                using (new EditorGUI.DisabledScope(addCandidate == null))
                {
                    if (GUILayout.Button("加入選取的事件", GUILayout.Height(26f)))
                    {
                        AddEvent(addCandidate);
                        addCandidate = null;
                    }
                }
            }

            EditorGUILayout.Space();
            if (groups.Count == 0)
            {
                EditorGUILayout.HelpBox("事件目錄目前沒有任何事件。", MessageType.Info);
                return;
            }

            for (int index = 0; index < groups.Count; index++)
            {
                TimingGroup group = groups[index];
                group.List.drawElementCallback = (rect, row, active, focused) =>
                    DrawEventRow(group, rect, row, openEvent);
                group.List.DoLayoutList();
                EditorGUILayout.Space(4f);
            }
        }

        private TimingGroup CreateGroup(string timing)
        {
            TimingGroup group = new TimingGroup
            {
                Timing = timing,
                Events = new List<EventInfo>()
            };
            group.List = new ReorderableList(
                group.Events,
                typeof(EventInfo),
                true,
                true,
                false,
                false)
            {
                elementHeight = EditorGUIUtility.singleLineHeight + 6f,
                drawHeaderCallback = rect => EditorGUI.LabelField(
                    rect,
                    FormatTimingHeader(group.Timing),
                    EditorStyles.boldLabel)
            };
            group.List.onReorderCallback = _ => ApplyGroupOrder(group);
            return group;
        }

        private void DrawEventRow(
            TimingGroup group,
            Rect rect,
            int row,
            Action<TextAsset> openEvent)
        {
            if (row < 0 || row >= group.Events.Count)
            {
                return;
            }

            EventInfo info = group.Events[row];
            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;
            Rect orderRect = new Rect(rect.x, rect.y, 28f, rect.height);
            Rect removeRect = new Rect(rect.xMax - 24f, rect.y, 24f, rect.height);
            Rect editRect = new Rect(removeRect.x - 48f, rect.y, 44f, rect.height);
            Rect labelRect = new Rect(
                orderRect.xMax + 4f,
                rect.y,
                editRect.x - orderRect.xMax - 8f,
                rect.height);

            EditorGUI.LabelField(orderRect, (row + 1).ToString());
            string label = info.Document != null
                ? $"{info.Document.DisplayName}  ({info.Asset.name})"
                : $"{info.Asset?.name ?? "遺失資產"} — {info.Error}";
            EditorGUI.LabelField(labelRect, label);
            using (new EditorGUI.DisabledScope(info.Asset == null))
            {
                if (GUI.Button(editRect, "編輯"))
                {
                    openEvent?.Invoke(info.Asset);
                    GUIUtility.ExitGUI();
                }
            }

            if (GUI.Button(removeRect, "×"))
            {
                Remove(info.Asset);
                GUIUtility.ExitGUI();
            }
        }

        internal void AddEvent(TextAsset asset)
        {
            ValidateEventAsset(asset);
            if (catalog.Files.Contains(asset))
            {
                throw new InvalidOperationException(
                    $"「{asset.name}」已存在於事件目錄「{catalog.name}」中。");
            }

            List<TextAsset> files = catalog.Files.ToList();
            files.Add(asset);
            WriteFiles(files, "Add Game Event To Catalog");
        }

        private void Remove(TextAsset asset)
        {
            List<TextAsset> files = catalog.Files.ToList();
            int index = files.IndexOf(asset);
            if (index < 0)
            {
                return;
            }

            files.RemoveAt(index);
            WriteFiles(files, "Remove Game Event From Catalog");
        }

        private void ApplyGroupOrder(TimingGroup group)
        {
            List<TextAsset> files = catalog.Files.ToList();
            List<int> groupPositions = new List<int>();
            for (int index = 0; index < files.Count; index++)
            {
                EventInfo info = ReadEvent(files[index]);
                string timing = info.Document != null
                    ? info.Document.TriggerTiming
                    : "<Invalid>";
                if (string.Equals(timing, group.Timing, StringComparison.Ordinal))
                {
                    groupPositions.Add(index);
                }
            }

            for (int index = 0; index < groupPositions.Count; index++)
            {
                files[groupPositions[index]] = group.Events[index].Asset;
            }

            WriteFiles(files, "Reorder Game Events");
        }

        private void WriteFiles(IReadOnlyList<TextAsset> files, string undoName)
        {
            Undo.RecordObject(catalog, undoName);
            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SerializedProperty filesProperty = serializedCatalog.FindProperty("files");
            filesProperty.arraySize = files.Count;
            for (int index = 0; index < files.Count; index++)
            {
                filesProperty.GetArrayElementAtIndex(index).objectReferenceValue = files[index];
            }

            serializedCatalog.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Refresh();
        }

        private EventInfo ReadEvent(TextAsset asset)
        {
            if (asset == null)
            {
                return new EventInfo { Error = "遺失 TextAsset" };
            }

            try
            {
                return new EventInfo
                {
                    Asset = asset,
                    Document = codec.Read(asset.text)
                };
            }
            catch (Exception exception)
            {
                return new EventInfo
                {
                    Asset = asset,
                    Error = exception.Message
                };
            }
        }

        private void ValidateEventAsset(TextAsset asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            string path = AssetDatabase.GetAssetPath(asset);
            if (!path.EndsWith(".gameevent.json", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"「{path}」不是 .gameevent.json 資產。");
            }

            codec.Read(asset.text);
        }

        private static string FormatTimingHeader(string timing)
        {
            if (string.IsNullOrEmpty(timing))
            {
                return "無指定時機（由場景直接觸發）";
            }

            return timing == "<Invalid>" ? "無效事件" : timing;
        }
    }
}
