using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using KahaGameCore.Effects;
using KahaGameCore.Parameters;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    /// <summary>
    /// 一處引用了不存在的參數鍵：事件（<see cref="EventAsset"/>）或場景、prefab 的欄位
    /// （<see cref="ObjectReference"/>），兩者擇一。
    /// </summary>
    internal sealed class ParameterReferenceIssue
    {
        private ParameterReferenceIssue(
            string key,
            string assetPath,
            string description,
            TextAsset eventAsset,
            GameEventObjectReference objectReference)
        {
            Key = key ?? string.Empty;
            AssetPath = assetPath ?? string.Empty;
            Description = description ?? string.Empty;
            EventAsset = eventAsset;
            ObjectReference = objectReference;
        }

        public string Key { get; }
        public string AssetPath { get; }
        public string Description { get; }
        public TextAsset EventAsset { get; }
        public GameEventObjectReference ObjectReference { get; }

        public static ParameterReferenceIssue ForEvent(
            string key,
            GameEventParameterReference reference)
        {
            return new ParameterReferenceIssue(
                key: key,
                assetPath: reference.AssetPath,
                description: $"事件「{reference.EventDisplayName}」· {reference.FormatUsage()}",
                eventAsset: reference.EventAsset,
                objectReference: null);
        }

        public static ParameterReferenceIssue ForObject(
            string key,
            GameEventObjectReference reference)
        {
            return new ParameterReferenceIssue(
                key: key,
                assetPath: reference.AssetPath,
                description: string.IsNullOrEmpty(reference.ComponentTypeName)
                    ? $"{reference.ObjectPath} · {reference.PropertyPath}"
                    : $"{reference.ObjectPath} · {reference.ComponentTypeName}.{reference.PropertyPath}",
                eventAsset: null,
                objectReference: reference);
        }

        /// <summary>檔案文字裡有這個鍵，但載入後的物件欄位找不到（例如欄位不是字串）。</summary>
        public static ParameterReferenceIssue ForFile(string key, string assetPath)
        {
            return new ParameterReferenceIssue(
                key: key,
                assetPath: assetPath,
                description: "檔案中出現，但找不到對應的物件欄位",
                eventAsset: null,
                objectReference: null);
        }
    }

    internal sealed class ParameterReferenceReport
    {
        public ParameterReferenceReport(
            IReadOnlyList<ParameterReferenceIssue> issues,
            IReadOnlyList<string> warnings,
            IReadOnlyCollection<string> definedKeys)
        {
            Issues = issues ?? Array.Empty<ParameterReferenceIssue>();
            Warnings = warnings ?? Array.Empty<string>();
            DefinedKeys = definedKeys ?? Array.Empty<string>();
        }

        public IReadOnlyList<ParameterReferenceIssue> Issues { get; }
        public IReadOnlyList<string> Warnings { get; }

        /// <summary>所有事件目錄參數表的鍵，用來提示相近的鍵。</summary>
        public IReadOnlyCollection<string> DefinedKeys { get; }
    }

    /// <summary>
    /// 找出引用不存在參數鍵的事件與場景、prefab 物件，規則與
    /// CatalogParameterReferenceTests 相同：事件只能用自己目錄參數表的鍵，
    /// 場景與 prefab 的公式可以用任一目錄的鍵。
    /// </summary>
    internal static class GameEventParameterReferenceChecker
    {
        public static ParameterReferenceReport CheckProject()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Play 模式中無法檢查參數引用。");
            }

            ParameterTableJsonCodec codec = new ParameterTableJsonCodec();
            List<ParameterReferenceIssue> issues = new List<ParameterReferenceIssue>();
            List<string> warnings = new List<string>();
            HashSet<string> allKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(GameEventCatalogAsset)))
            {
                string catalogPath = AssetDatabase.GUIDToAssetPath(guid);
                GameEventCatalogAsset catalog =
                    AssetDatabase.LoadAssetAtPath<GameEventCatalogAsset>(catalogPath);
                if (catalog == null)
                {
                    continue;
                }

                HashSet<string> catalogKeys = ReadKeys(catalog, codec, catalogPath, warnings);
                allKeys.UnionWith(catalogKeys);
                IReadOnlyList<EffectCommandDescriptor> descriptors =
                    EffectCommandAssemblyCatalog.GetDescriptors(
                        catalog.CommandAssemblyNames,
                        warnings: null);
                GameEventParameterUsageIndex index =
                    GameEventParameterUsageIndex.Build(catalog, descriptors);
                warnings.AddRange(index.Warnings.Select(warning => $"{catalogPath}：{warning}"));
                issues.AddRange(FindInEvents(index, catalogKeys));
            }

            issues.AddRange(FindInScenesAndPrefabs(allKeys, warnings));
            return new ParameterReferenceReport(issues, warnings, allKeys);
        }

        internal static IEnumerable<ParameterReferenceIssue> FindInEvents(
            GameEventParameterUsageIndex index,
            IReadOnlyCollection<string> definedKeys)
        {
            return index.ReferencedKeys
                .Where(key => !definedKeys.Contains(key))
                .OrderBy(key => key, StringComparer.Ordinal)
                .SelectMany(key => index.Find(key)
                    .Select(reference => ParameterReferenceIssue.ForEvent(key, reference)));
        }

        /// <summary>
        /// 列出 roots 底下（包含停用物件）所有元件字串欄位中，不在 definedKeys 的 `$Key`。
        /// prefab instance（包含巢狀 prefab 與 variant）只列出覆寫過的欄位，
        /// 沒覆寫的由來源 prefab 列出，同一處不會重複。
        /// </summary>
        internal static IReadOnlyList<ParameterReferenceIssue> FindInHierarchy(
            IEnumerable<GameObject> roots,
            IReadOnlyCollection<string> definedKeys,
            GameEventObjectReferenceSource source,
            string assetPath)
        {
            List<ParameterReferenceIssue> issues = new List<ParameterReferenceIssue>();
            foreach (GameObject root in roots ?? Enumerable.Empty<GameObject>())
            {
                if (root == null)
                {
                    continue;
                }

                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    // 遺失腳本的元件會是 null。
                    if (component == null)
                    {
                        continue;
                    }

                    // 只有來自 prefab 的元件需要略過未覆寫的欄位；一般場景物件與
                    // 加在 instance 上的元件一律檢查。
                    bool fromPrefab = PrefabUtility.IsPartOfPrefabInstance(component) &&
                        !PrefabUtility.IsAddedComponentOverride(component);
                    using (SerializedObject serializedObject = new SerializedObject(component))
                    {
                        SerializedProperty property = serializedObject.GetIterator();
                        bool enterChildren = true;
                        while (property.Next(enterChildren))
                        {
                            enterChildren = property.propertyType != SerializedPropertyType.String;
                            if (property.propertyType != SerializedPropertyType.String ||
                                (fromPrefab && !property.prefabOverride))
                            {
                                continue;
                            }

                            foreach (string key in GameEventParameterUsageIndex
                                         .ExtractExpressionKeys(property.stringValue)
                                         .Where(key => !definedKeys.Contains(key)))
                            {
                                issues.Add(ParameterReferenceIssue.ForObject(
                                    key,
                                    new GameEventObjectReference(
                                        source: source,
                                        assetPath: assetPath,
                                        objectPath: GameEventReferenceFinder.GetHierarchyPath(
                                            component.transform),
                                        componentTypeName: component.GetType().Name,
                                        propertyPath: property.propertyPath,
                                        globalObjectId: UnityEditor.GlobalObjectId
                                            .GetGlobalObjectIdSlow(component)
                                            .ToString(),
                                        siblingPath: GameEventReferenceFinder.GetSiblingPath(
                                            transform: component.transform,
                                            root: root.transform))));
                            }
                        }
                    }
                }
            }

            return issues;
        }

        /// <summary>依編輯距離列出最接近的鍵，供拼錯或改名後的鍵參考。</summary>
        internal static IReadOnlyList<string> SuggestKeys(
            string missingKey,
            IEnumerable<string> definedKeys,
            int maxCount = 3)
        {
            if (string.IsNullOrEmpty(missingKey))
            {
                return Array.Empty<string>();
            }

            int maxDistance = Math.Max(2, missingKey.Length / 3);
            return (definedKeys ?? Enumerable.Empty<string>())
                .Select(key => (key, distance: Distance(missingKey, key)))
                .Where(item => item.distance <= maxDistance)
                .OrderBy(item => item.distance)
                .ThenBy(item => item.key, StringComparer.Ordinal)
                .Take(maxCount)
                .Select(item => item.key)
                .ToArray();
        }

        /// <summary>開啟事件、選取物件或 ping 檔案。</summary>
        /// <returns>無法定位時的說明；成功或使用者取消時為 null。</returns>
        public static string Locate(ParameterReferenceIssue issue)
        {
            if (issue.EventAsset != null)
            {
                GameEventDocumentEditorWindow.OpenWindow(issue.EventAsset);
                return null;
            }

            if (issue.ObjectReference != null)
            {
                return GameEventReferenceFinder.Locate(issue.ObjectReference);
            }

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(issue.AssetPath);
            if (asset == null)
            {
                return $"找不到「{issue.AssetPath}」，請重新檢查。";
            }

            GameEventReferenceFinder.Select(asset);
            return null;
        }

        private static IEnumerable<ParameterReferenceIssue> FindInScenesAndPrefabs(
            IReadOnlyCollection<string> definedKeys,
            List<string> warnings)
        {
            // 先以檔案文字篩選，只載入真的含有未知鍵的場景與 prefab。
            List<(string path, string[] keys)> candidates = new List<(string, string[])>();
            foreach (string path in GameEventParameterRenamer.FindScenesAndPrefabs())
            {
                string[] missing;
                try
                {
                    missing = GameEventParameterUsageIndex
                        .ExtractExpressionKeys(File.ReadAllText(path))
                        .Where(key => !definedKeys.Contains(key))
                        .ToArray();
                }
                catch (Exception exception)
                {
                    warnings.Add($"無法讀取「{path}」：{exception.Message}");
                    continue;
                }

                if (missing.Length > 0)
                {
                    candidates.Add((path, missing));
                }
            }

            List<ParameterReferenceIssue> issues = new List<ParameterReferenceIssue>();
            try
            {
                for (int index = 0; index < candidates.Count; index++)
                {
                    (string path, string[] keys) = candidates[index];
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "檢查參數引用",
                            path,
                            (float)index / candidates.Count))
                    {
                        warnings.Add("檢查已取消，結果可能不完整。");
                        break;
                    }

                    IReadOnlyList<ParameterReferenceIssue> found;
                    try
                    {
                        found = path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
                            ? FindInSceneText(
                                scenePath: path,
                                sceneGuid: AssetDatabase.AssetPathToGUID(path),
                                text: File.ReadAllText(path),
                                definedKeys: definedKeys)
                            : FindInPrefab(path, definedKeys);
                    }
                    catch (Exception exception)
                    {
                        warnings.Add($"無法檢查「{path}」：{exception.Message}");
                        continue;
                    }

                    issues.AddRange(found);
                    // 未覆寫的 prefab 欄位不在 found 裡，由來源 prefab 列出；
                    // 只有文字中出現、卻哪裡都找不到的鍵才以檔案列出。
                    HashSet<string> foundKeys = new HashSet<string>(
                        found.Select(issue => issue.Key),
                        StringComparer.Ordinal);
                    issues.AddRange(keys
                        .Where(key => !foundKeys.Contains(key))
                        .Where(key => !IsInheritedFromPrefab(path, key, candidates))
                        .Select(key => ParameterReferenceIssue.ForFile(key, path)));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return issues;
        }

        private static readonly Regex DocumentHeader = new Regex(
            @"^--- !u!(?<class>\d+) &(?<id>-?\d+)(?<stripped> stripped)?\r?$",
            RegexOptions.Multiline);

        private sealed class SceneDocument
        {
            public int ClassId;
            public long FileId;
            public bool Stripped;
            public string Body;
        }

        /// <summary>
        /// 直接讀場景檔：找出含有未知鍵的元件或 prefab instance 覆寫，以 fileID 組成
        /// GlobalObjectId，定位時開啟場景後依它選取物件。不必載入場景。
        /// </summary>
        internal static IReadOnlyList<ParameterReferenceIssue> FindInSceneText(
            string scenePath,
            string sceneGuid,
            string text,
            IReadOnlyCollection<string> definedKeys)
        {
            Dictionary<long, SceneDocument> documents = ParseDocuments(text);
            Dictionary<long, long> transformByGameObject = new Dictionary<long, long>();
            foreach (SceneDocument document in documents.Values)
            {
                if ((document.ClassId == 4 || document.ClassId == 224) && !document.Stripped)
                {
                    long owner = ReadFileId(document.Body, "m_GameObject");
                    if (owner != 0 && !transformByGameObject.ContainsKey(owner))
                    {
                        transformByGameObject.Add(owner, document.FileId);
                    }
                }
            }

            List<ParameterReferenceIssue> issues = new List<ParameterReferenceIssue>();
            HashSet<string> listed = new HashSet<string>(StringComparer.Ordinal);
            foreach (SceneDocument document in documents.Values.OrderBy(item => item.FileId))
            {
                if (document.ClassId == 114 && !document.Stripped)
                {
                    long gameObject = ReadFileId(document.Body, "m_GameObject");
                    string objectPath = DescribeHierarchy(documents, transformByGameObject, gameObject);
                    string component = ReadComponentName(document.Body);
                    foreach ((string property, string value) in ReadExpressionLines(document.Body))
                    {
                        // 清單中多筆同名欄位（例如多個 binding 的 condition）同一個元件只列一次。
                        foreach (string key in UnknownKeys(value, definedKeys)
                                     .Where(key => listed.Add($"{key}|{document.FileId}|{property}")))
                        {
                            issues.Add(ParameterReferenceIssue.ForObject(
                                key,
                                new GameEventObjectReference(
                                    source: GameEventObjectReferenceSource.Scene,
                                    assetPath: scenePath,
                                    objectPath: objectPath,
                                    componentTypeName: component,
                                    propertyPath: property,
                                    globalObjectId:
                                        $"GlobalObjectId_V1-2-{sceneGuid}-{(ulong)document.FileId}-0",
                                    siblingPath: Array.Empty<int>())));
                        }
                    }
                }
                else if (document.ClassId == 1001)
                {
                    issues.AddRange(FindInPrefabInstanceModifications(
                        scenePath: scenePath,
                        sceneGuid: sceneGuid,
                        prefabInstance: document,
                        definedKeys: definedKeys));
                }
            }

            return issues;
        }

        // 場景裡的 prefab instance 覆寫寫在 m_Modifications：target 是來源 prefab 內的 fileID。
        private static IEnumerable<ParameterReferenceIssue> FindInPrefabInstanceModifications(
            string scenePath,
            string sceneGuid,
            SceneDocument prefabInstance,
            IReadOnlyCollection<string> definedKeys)
        {
            Match source = Regex.Match(
                prefabInstance.Body,
                @"m_SourcePrefab: \{fileID: -?\d+, guid: (?<guid>[0-9a-f]+)");
            string prefabName = source.Success
                ? Path.GetFileNameWithoutExtension(
                    AssetDatabase.GUIDToAssetPath(source.Groups["guid"].Value))
                : "prefab";
            foreach (Match modification in Regex.Matches(
                         prefabInstance.Body,
                         @"- target: \{fileID: (?<target>-?\d+)[^\n]*\n\s*propertyPath: (?<path>[^\r\n]*)\r?\n\s*value: (?<value>[^\r\n]*)"))
            {
                foreach (string key in UnknownKeys(modification.Groups["value"].Value, definedKeys))
                {
                    long target = long.Parse(modification.Groups["target"].Value);
                    yield return ParameterReferenceIssue.ForObject(
                        key,
                        new GameEventObjectReference(
                            source: GameEventObjectReferenceSource.Scene,
                            assetPath: scenePath,
                            objectPath: $"{prefabName}（prefab instance 覆寫）",
                            componentTypeName: string.Empty,
                            propertyPath: modification.Groups["path"].Value,
                            globalObjectId:
                                $"GlobalObjectId_V1-2-{sceneGuid}-{(ulong)target}-{(ulong)prefabInstance.FileId}",
                            siblingPath: Array.Empty<int>()));
                }
            }
        }

        private static Dictionary<long, SceneDocument> ParseDocuments(string text)
        {
            Dictionary<long, SceneDocument> documents = new Dictionary<long, SceneDocument>();
            MatchCollection headers = DocumentHeader.Matches(text);
            for (int index = 0; index < headers.Count; index++)
            {
                Match header = headers[index];
                int start = header.Index + header.Length;
                int end = index + 1 < headers.Count ? headers[index + 1].Index : text.Length;
                long fileId = long.Parse(header.Groups["id"].Value);
                documents[fileId] = new SceneDocument
                {
                    ClassId = int.Parse(header.Groups["class"].Value),
                    FileId = fileId,
                    Stripped = header.Groups["stripped"].Success,
                    Body = text.Substring(start, end - start)
                };
            }

            return documents;
        }

        private static long ReadFileId(string body, string field)
        {
            Match match = Regex.Match(body, @"\b" + field + @": \{fileID: (?<id>-?\d+)");
            return match.Success ? long.Parse(match.Groups["id"].Value) : 0;
        }

        // m_EditorClassIdentifier 形如 "Assembly::Namespace.Type"，取最後的型別名稱。
        private static string ReadComponentName(string body)
        {
            Match match = Regex.Match(
                body,
                @"m_EditorClassIdentifier: [^\r\n]*?(?<name>[A-Za-z0-9_]+)\r?$",
                RegexOptions.Multiline);
            return match.Success ? match.Groups["name"].Value : "MonoBehaviour";
        }

        // 含有 `$` 的「欄位: 值」行；清單元素去掉開頭的 "- "。
        private static IEnumerable<(string property, string value)> ReadExpressionLines(string body)
        {
            foreach (Match line in Regex.Matches(
                         body,
                         @"^\s*(?:- )?(?<name>[A-Za-z_][A-Za-z0-9_]*): (?<value>[^\r\n]*\$[^\r\n]*)\r?$",
                         RegexOptions.Multiline))
            {
                yield return (line.Groups["name"].Value, line.Groups["value"].Value);
            }
        }

        private static IEnumerable<string> UnknownKeys(
            string value,
            IReadOnlyCollection<string> definedKeys)
        {
            return GameEventParameterUsageIndex.ExtractExpressionKeys(value)
                .Where(key => !definedKeys.Contains(key));
        }

        // 沿 m_Father 往上組出 Hierarchy 路徑；經過 prefab instance 的節點讀不到名稱，以「…」表示。
        private static string DescribeHierarchy(
            IReadOnlyDictionary<long, SceneDocument> documents,
            IReadOnlyDictionary<long, long> transformByGameObject,
            long gameObject)
        {
            List<string> names = new List<string> { ReadName(documents, gameObject) };
            long transform = transformByGameObject.TryGetValue(gameObject, out long found) ? found : 0;
            for (int depth = 0; depth < 256 && transform != 0; depth++)
            {
                if (!documents.TryGetValue(transform, out SceneDocument transformDocument))
                {
                    break;
                }

                long father = ReadFileId(transformDocument.Body, "m_Father");
                if (father == 0)
                {
                    break;
                }

                if (!documents.TryGetValue(father, out SceneDocument fatherDocument) ||
                    fatherDocument.Stripped)
                {
                    names.Add("…");
                    break;
                }

                names.Add(ReadName(documents, ReadFileId(fatherDocument.Body, "m_GameObject")));
                transform = father;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private static string ReadName(
            IReadOnlyDictionary<long, SceneDocument> documents,
            long gameObject)
        {
            if (!documents.TryGetValue(gameObject, out SceneDocument document))
            {
                return "?";
            }

            Match match = Regex.Match(document.Body, @"m_Name: (?<name>[^\r\n]*)");
            return match.Success ? match.Groups["name"].Value.Trim() : "?";
        }

        private static IReadOnlyList<ParameterReferenceIssue> FindInPrefab(
            string prefabPath,
            IReadOnlyCollection<string> definedKeys)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return root == null
                ? Array.Empty<ParameterReferenceIssue>()
                : FindInHierarchy(
                    new[] { root },
                    definedKeys,
                    GameEventObjectReferenceSource.Prefab,
                    prefabPath);
        }

        // 欄位找不到、但另一個 prefab 的文字也含有同一個鍵時，視為從那個 prefab 繼承，
        // 由它列出。
        private static bool IsInheritedFromPrefab(
            string path,
            string key,
            IReadOnlyList<(string path, string[] keys)> candidates)
        {
            return candidates.Any(candidate =>
                !string.Equals(candidate.path, path, StringComparison.OrdinalIgnoreCase) &&
                candidate.path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) &&
                candidate.keys.Contains(key));
        }

        private static HashSet<string> ReadKeys(
            GameEventCatalogAsset catalog,
            ParameterTableJsonCodec codec,
            string catalogPath,
            List<string> warnings)
        {
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (TextAsset table in catalog.ParameterTables.Where(asset => asset != null))
            {
                try
                {
                    keys.UnionWith(codec.Read(table.text).Definitions
                        .Select(definition => definition.Key));
                }
                catch (Exception exception)
                {
                    warnings.Add(
                        $"{catalogPath}：無法讀取參數表「{AssetDatabase.GetAssetPath(table)}」：" +
                        exception.Message);
                }
            }

            return keys;
        }

        private static int Distance(string left, string right)
        {
            int[] previous = new int[right.Length + 1];
            int[] current = new int[right.Length + 1];
            for (int column = 0; column <= right.Length; column++)
            {
                previous[column] = column;
            }

            for (int row = 1; row <= left.Length; row++)
            {
                current[0] = row;
                for (int column = 1; column <= right.Length; column++)
                {
                    int substitution = char.ToLowerInvariant(left[row - 1]) ==
                        char.ToLowerInvariant(right[column - 1])
                            ? 0
                            : 1;
                    current[column] = Math.Min(
                        Math.Min(current[column - 1] + 1, previous[column] + 1),
                        previous[column - 1] + substitution);
                }

                (previous, current) = (current, previous);
            }

            return previous[right.Length];
        }
    }
}
