using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.Effects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KahaGameCore.GameEvents.Editor
{
    /// <summary>另一個事件的指令（例如 TriggerEvent）以 DocumentGuid 引用目前事件。</summary>
    internal sealed class GameEventCommandReference
    {
        public GameEventCommandReference(
            TextAsset eventAsset,
            string assetPath,
            string eventDisplayName,
            IReadOnlyList<string> commandNames)
        {
            EventAsset = eventAsset;
            AssetPath = assetPath ?? string.Empty;
            EventDisplayName = eventDisplayName ?? string.Empty;
            CommandNames = commandNames ?? Array.Empty<string>();
        }

        public TextAsset EventAsset { get; }
        public string AssetPath { get; }
        public string EventDisplayName { get; }
        public IReadOnlyList<string> CommandNames { get; }
    }

    internal enum GameEventObjectReferenceSource
    {
        Scene,
        Prefab,
        Asset
    }

    /// <summary>場景物件、prefab 或 ScriptableObject 的欄位引用目前事件。</summary>
    internal sealed class GameEventObjectReference
    {
        public GameEventObjectReference(
            GameEventObjectReferenceSource source,
            string assetPath,
            string objectPath,
            string componentTypeName,
            string propertyPath,
            string globalObjectId,
            IReadOnlyList<int> siblingPath)
        {
            Source = source;
            AssetPath = assetPath ?? string.Empty;
            ObjectPath = objectPath ?? string.Empty;
            ComponentTypeName = componentTypeName ?? string.Empty;
            PropertyPath = propertyPath ?? string.Empty;
            GlobalObjectId = globalObjectId ?? string.Empty;
            SiblingPath = siblingPath ?? Array.Empty<int>();
        }

        public GameEventObjectReferenceSource Source { get; }

        /// <summary>場景、prefab 或資產檔的路徑。</summary>
        public string AssetPath { get; }

        /// <summary>場景與 prefab 為 Hierarchy 路徑；資產為物件名稱。</summary>
        public string ObjectPath { get; }

        public string ComponentTypeName { get; }
        public string PropertyPath { get; }
        public string GlobalObjectId { get; }

        /// <summary>prefab 內從根物件往下的 sibling index，用來在 Prefab Mode 找回物件。</summary>
        public IReadOnlyList<int> SiblingPath { get; }
    }

    internal sealed class GameEventProjectReferences
    {
        public GameEventProjectReferences(
            IReadOnlyList<GameEventObjectReference> references,
            IReadOnlyList<string> warnings)
        {
            References = references ?? Array.Empty<GameEventObjectReference>();
            Warnings = warnings ?? Array.Empty<string>();
        }

        public IReadOnlyList<GameEventObjectReference> References { get; }
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 找出引用某個事件的地方：其他事件以指令參數（option source 為
    /// <see cref="GameEventEffectCommandModule.EventOptionSourceKey"/>）填它的 DocumentGuid，
    /// 或場景、prefab、ScriptableObject 以欄位指向它的 TextAsset。事件目錄收錄事件不算引用。
    /// </summary>
    internal static class GameEventReferenceFinder
    {
        public static IReadOnlyList<OpenGameEventUsageDocument> LoadCatalogDocuments(
            GameEventCatalogAsset catalog,
            OpenGameEventUsageDocument openDocument,
            ICollection<string> warnings)
        {
            List<OpenGameEventUsageDocument> documents = new List<OpenGameEventUsageDocument>();
            GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();
            foreach (TextAsset eventAsset in catalog?.Files ?? Array.Empty<TextAsset>())
            {
                if (eventAsset == null)
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(eventAsset);
                if (openDocument != null && SamePath(assetPath, openDocument.AssetPath))
                {
                    continue;
                }

                try
                {
                    GameEventDocument document = codec.Read(eventAsset.text);
                    documents.Add(new OpenGameEventUsageDocument(
                        eventAsset: eventAsset,
                        assetPath: assetPath,
                        displayName: document.DisplayName,
                        condition: document.Condition,
                        commands: document.Commands));
                }
                catch (Exception exception)
                {
                    warnings?.Add($"無法分析事件引用「{assetPath}」：{exception.Message}");
                }
            }

            if (openDocument != null)
            {
                documents.Add(openDocument);
            }

            return documents;
        }

        public static IReadOnlyList<GameEventCommandReference> FindCommandReferences(
            string documentGuid,
            IEnumerable<OpenGameEventUsageDocument> documents,
            IReadOnlyList<EffectCommandDescriptor> commandDescriptors,
            ICollection<string> warnings)
        {
            if (!Guid.TryParse(documentGuid?.Trim() ?? string.Empty, out Guid target))
            {
                return Array.Empty<GameEventCommandReference>();
            }

            Dictionary<string, EffectCommandDescriptor> commandsByName =
                GameEventCommandDescriptorLookup.Create(commandDescriptors);
            List<GameEventCommandReference> references = new List<GameEventCommandReference>();
            foreach (OpenGameEventUsageDocument document in
                     documents ?? Enumerable.Empty<OpenGameEventUsageDocument>())
            {
                if (document == null)
                {
                    continue;
                }

                string displayName = string.IsNullOrWhiteSpace(document.DisplayName)
                    ? "未命名事件"
                    : document.DisplayName;
                SortedSet<string> commandNames = new SortedSet<string>(StringComparer.Ordinal);
                try
                {
                    foreach (GameEventCommandDraft command in
                             GameEventCommandDraftCodec.Parse(document.Commands))
                    {
                        if (command.Name == null ||
                            !commandsByName.TryGetValue(
                                command.Name,
                                out EffectCommandDescriptor descriptor))
                        {
                            continue;
                        }

                        int argumentCount = Math.Min(
                            command.Arguments.Count,
                            descriptor.Parameters.Count);
                        for (int index = 0; index < argumentCount; index++)
                        {
                            if (string.Equals(
                                    descriptor.Parameters[index].OptionSourceKey,
                                    GameEventEffectCommandModule.EventOptionSourceKey,
                                    StringComparison.Ordinal) &&
                                Guid.TryParse(
                                    command.Arguments[index]?.Trim() ?? string.Empty,
                                    out Guid referenced) &&
                                referenced == target)
                            {
                                commandNames.Add(command.Name);
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    warnings?.Add($"無法分析事件「{displayName}」的指令引用：{exception.Message}");
                    continue;
                }

                if (commandNames.Count > 0)
                {
                    references.Add(new GameEventCommandReference(
                        eventAsset: document.EventAsset,
                        assetPath: document.AssetPath,
                        eventDisplayName: displayName,
                        commandNames: commandNames.ToArray()));
                }
            }

            return references
                .OrderBy(reference => reference.EventDisplayName, StringComparer.Ordinal)
                .ThenBy(reference => reference.AssetPath, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// 搜尋整個 Assets 中的場景、prefab 與 .asset。未開啟的場景會暫時以 additive 載入，
        /// 搜尋完關閉（原本就在 Hierarchy 但未載入的場景，關閉後保留在 Hierarchy）。
        /// </summary>
        public static GameEventProjectReferences FindInProject(TextAsset eventAsset)
        {
            if (eventAsset == null)
            {
                throw new ArgumentNullException(nameof(eventAsset));
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Play 模式中無法搜尋事件引用。");
            }

            string eventAssetPath = AssetDatabase.GetAssetPath(eventAsset);
            if (string.IsNullOrEmpty(eventAssetPath))
            {
                throw new InvalidOperationException("事件尚未存成資產，無法搜尋引用。");
            }

            IReadOnlyList<string> candidates = FindCandidatePaths(
                eventAssetPath,
                FindSearchablePaths(),
                path => AssetDatabase.GetDependencies(path, false));
            List<GameEventObjectReference> references = new List<GameEventObjectReference>();
            List<string> warnings = new List<string>();
            try
            {
                for (int index = 0; index < candidates.Count; index++)
                {
                    string path = candidates[index];
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "搜尋事件引用",
                            path,
                            (float)index / candidates.Count))
                    {
                        warnings.Add("搜尋已取消，結果可能不完整。");
                        break;
                    }

                    try
                    {
                        if (IsScene(path))
                        {
                            references.AddRange(FindInScene(eventAsset, path));
                        }
                        else if (IsPrefab(path))
                        {
                            references.AddRange(FindInPrefab(eventAsset, path));
                        }
                        else
                        {
                            references.AddRange(FindInAsset(eventAsset, path));
                        }
                    }
                    catch (Exception exception)
                    {
                        warnings.Add($"無法搜尋「{path}」：{exception.Message}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return new GameEventProjectReferences(references, warnings);
        }

        /// <summary>
        /// 以直接相依篩出可能引用事件的檔案：直接引用事件的 .asset 與 prefab、
        /// 含有（可巢狀）這些 prefab 的 prefab，以及直接引用事件或含有這些 prefab 的場景。
        /// 經由 ScriptableObject（例如事件目錄）間接相依的場景不列入。
        /// 依場景、prefab、資產排序，同類依路徑排序。
        /// </summary>
        internal static IReadOnlyList<string> FindCandidatePaths(
            string eventAssetPath,
            IEnumerable<string> assetPaths,
            Func<string, IEnumerable<string>> getDirectDependencies)
        {
            Dictionary<string, HashSet<string>> dependencies =
                new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in assetPaths ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrEmpty(path) ||
                    SamePath(path, eventAssetPath) ||
                    dependencies.ContainsKey(path))
                {
                    continue;
                }

                dependencies.Add(
                    path,
                    new HashSet<string>(
                        (getDirectDependencies(path) ?? Enumerable.Empty<string>())
                        .Where(dependency => !SamePath(dependency, path)),
                        StringComparer.OrdinalIgnoreCase));
            }

            HashSet<string> carriers =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { eventAssetPath };
            bool added = true;
            while (added)
            {
                added = false;
                foreach (KeyValuePair<string, HashSet<string>> pair in dependencies)
                {
                    if (IsPrefab(pair.Key) &&
                        !carriers.Contains(pair.Key) &&
                        pair.Value.Overlaps(carriers))
                    {
                        carriers.Add(pair.Key);
                        added = true;
                    }
                }
            }

            return dependencies
                .Where(pair => IsScene(pair.Key)
                    ? pair.Value.Overlaps(carriers)
                    : IsPrefab(pair.Key)
                        ? carriers.Contains(pair.Key)
                        : pair.Value.Contains(eventAssetPath))
                .Select(pair => pair.Key)
                .OrderBy(path => IsScene(path) ? 0 : IsPrefab(path) ? 1 : 2)
                .ThenBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// 列出 roots 底下（包含停用物件）所有 MonoBehaviour 中指向 eventAsset 的欄位，
        /// 依 Hierarchy 由上到下。
        /// </summary>
        internal static IReadOnlyList<GameEventObjectReference> FindInHierarchy(
            TextAsset eventAsset,
            IEnumerable<GameObject> roots,
            GameEventObjectReferenceSource source,
            string assetPath)
        {
            List<GameEventObjectReference> references = new List<GameEventObjectReference>();
            if (eventAsset == null)
            {
                return references;
            }

            foreach (GameObject root in roots ?? Enumerable.Empty<GameObject>())
            {
                if (root == null)
                {
                    continue;
                }

                foreach (MonoBehaviour behaviour in
                         root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    // 遺失腳本的元件會是 null。
                    if (behaviour == null)
                    {
                        continue;
                    }

                    foreach (string propertyPath in FindReferencingProperties(behaviour, eventAsset))
                    {
                        references.Add(new GameEventObjectReference(
                            source: source,
                            assetPath: assetPath,
                            objectPath: GetHierarchyPath(behaviour.transform),
                            componentTypeName: behaviour.GetType().Name,
                            propertyPath: propertyPath,
                            globalObjectId: UnityEditor.GlobalObjectId
                                .GetGlobalObjectIdSlow(behaviour)
                                .ToString(),
                            siblingPath: GetSiblingPath(
                                transform: behaviour.transform,
                                root: root.transform)));
                    }
                }
            }

            return references;
        }

        /// <summary>選取引用所在的物件；場景未開啟時先開啟，prefab 會進入 Prefab Mode。</summary>
        /// <returns>無法定位時的說明；成功或使用者取消時為 null。</returns>
        public static string Locate(GameEventObjectReference reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            switch (reference.Source)
            {
                case GameEventObjectReferenceSource.Scene:
                    return LocateInScene(reference);
                case GameEventObjectReferenceSource.Prefab:
                    return LocateInPrefab(reference);
                case GameEventObjectReferenceSource.Asset:
                    return LocateAsset(reference);
                default:
                    throw new ArgumentOutOfRangeException(nameof(reference));
            }
        }

        private static IEnumerable<string> FindSearchablePaths()
        {
            return AssetDatabase.GetAllAssetPaths()
                .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
                .Where(path => IsScene(path) || IsPrefab(path) ||
                    path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<GameEventObjectReference> FindInScene(
            TextAsset eventAsset,
            string scenePath)
        {
            return ScanScene(
                scenePath,
                roots => FindInHierarchy(
                    eventAsset,
                    roots,
                    GameEventObjectReferenceSource.Scene,
                    scenePath));
        }

        /// <summary>
        /// 以場景的根物件執行 scan。未開啟的場景暫時以 additive 載入、執行完關閉
        /// （原本就在 Hierarchy 但未載入的場景，關閉後保留在 Hierarchy）。
        /// </summary>
        internal static T ScanScene<T>(string scenePath, Func<GameObject[], T> scan)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                return scan(scene.GetRootGameObjects());
            }

            bool keepInHierarchy = scene.IsValid();
            Scene opened = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                return scan(opened.GetRootGameObjects());
            }
            finally
            {
                EditorSceneManager.CloseScene(opened, removeScene: !keepInHierarchy);
            }
        }

        private static IReadOnlyList<GameEventObjectReference> FindInPrefab(
            TextAsset eventAsset,
            string prefabPath)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return root == null
                ? Array.Empty<GameEventObjectReference>()
                : FindInHierarchy(
                    eventAsset,
                    new[] { root },
                    GameEventObjectReferenceSource.Prefab,
                    prefabPath);
        }

        private static IReadOnlyList<GameEventObjectReference> FindInAsset(
            TextAsset eventAsset,
            string assetPath)
        {
            List<GameEventObjectReference> references = new List<GameEventObjectReference>();
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                // 事件目錄收錄事件不算引用。
                if (!(asset is ScriptableObject) || asset is GameEventCatalogAsset)
                {
                    continue;
                }

                foreach (string propertyPath in FindReferencingProperties(asset, eventAsset))
                {
                    references.Add(new GameEventObjectReference(
                        source: GameEventObjectReferenceSource.Asset,
                        assetPath: assetPath,
                        objectPath: asset.name,
                        componentTypeName: asset.GetType().Name,
                        propertyPath: propertyPath,
                        globalObjectId: UnityEditor.GlobalObjectId
                            .GetGlobalObjectIdSlow(asset)
                            .ToString(),
                        siblingPath: Array.Empty<int>()));
                }
            }

            return references;
        }

        private static IReadOnlyList<string> FindReferencingProperties(
            UnityEngine.Object target,
            TextAsset eventAsset)
        {
            List<string> propertyPaths = new List<string>();
            using (SerializedObject serializedObject = new SerializedObject(target))
            {
                SerializedProperty property = serializedObject.GetIterator();
                bool enterChildren = true;
                while (property.Next(enterChildren))
                {
                    enterChildren = property.propertyType != SerializedPropertyType.String;
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.objectReferenceValue == eventAsset)
                    {
                        propertyPaths.Add(property.propertyPath);
                    }
                }
            }

            return propertyPaths;
        }

        private static string LocateInScene(GameEventObjectReference reference)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return "Play 模式中無法開啟場景。";
            }

            StageUtility.GoToMainStage();
            Scene scene = SceneManager.GetSceneByPath(reference.AssetPath);
            if (scene.IsValid() && !scene.isLoaded)
            {
                EditorSceneManager.OpenScene(reference.AssetPath, OpenSceneMode.Additive);
            }
            else if (!scene.IsValid())
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return null;
                }

                EditorSceneManager.OpenScene(reference.AssetPath, OpenSceneMode.Single);
            }

            if (UnityEditor.GlobalObjectId.TryParse(
                    reference.GlobalObjectId,
                    out UnityEditor.GlobalObjectId id) &&
                UnityEditor.GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id)
                    is Component component)
            {
                Select(component.gameObject);
                return null;
            }

            return $"在「{reference.AssetPath}」找不到「{reference.ObjectPath}」，請重新搜尋。";
        }

        private static string LocateInPrefab(GameEventObjectReference reference)
        {
            PrefabStage stage = PrefabStageUtility.OpenPrefab(reference.AssetPath);
            if (stage == null)
            {
                Select(AssetDatabase.LoadMainAssetAtPath(reference.AssetPath));
                return $"無法開啟 prefab「{reference.AssetPath}」。";
            }

            Transform current = stage.prefabContentsRoot.transform;
            foreach (int index in reference.SiblingPath)
            {
                if (index < 0 || index >= current.childCount)
                {
                    return $"在「{reference.AssetPath}」找不到「{reference.ObjectPath}」，請重新搜尋。";
                }

                current = current.GetChild(index);
            }

            Select(current.gameObject);
            return null;
        }

        private static string LocateAsset(GameEventObjectReference reference)
        {
            UnityEngine.Object target = null;
            if (UnityEditor.GlobalObjectId.TryParse(
                    reference.GlobalObjectId,
                    out UnityEditor.GlobalObjectId id))
            {
                target = UnityEditor.GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
            }

            if (target == null)
            {
                target = AssetDatabase.LoadMainAssetAtPath(reference.AssetPath);
            }

            if (target == null)
            {
                return $"找不到「{reference.AssetPath}」，請重新搜尋。";
            }

            Select(target);
            return null;
        }

        internal static void Select(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
            if (target is GameObject)
            {
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }

        internal static string GetHierarchyPath(Transform transform)
        {
            List<string> names = new List<string>();
            for (Transform current = transform; current != null; current = current.parent)
            {
                names.Add(current.name);
            }

            names.Reverse();
            return string.Join("/", names);
        }

        internal static IReadOnlyList<int> GetSiblingPath(Transform transform, Transform root)
        {
            List<int> indices = new List<int>();
            for (Transform current = transform;
                 current != null && current != root;
                 current = current.parent)
            {
                indices.Add(current.GetSiblingIndex());
            }

            indices.Reverse();
            return indices;
        }

        private static bool IsScene(string path)
        {
            return path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPrefab(string path)
        {
            return path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
        }

        private static bool SamePath(string left, string right)
        {
            return string.Equals(
                (left ?? string.Empty).Replace('\\', '/'),
                (right ?? string.Empty).Replace('\\', '/'),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
