using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using KahaGameCore.Effects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KahaGameCore.GameEvents.Editor
{
    internal sealed class GameEventRewrite
    {
        public GameEventRewrite(string assetPath, string displayName, string json)
        {
            AssetPath = assetPath;
            DisplayName = displayName;
            Json = json;
        }

        public string AssetPath { get; }
        public string DisplayName { get; }
        public string Json { get; }
    }

    /// <summary>
    /// 改名只比對完整的參數鍵：指令參數依描述檔只看 ParameterKey 欄位，
    /// 公式只看 `$` 後面整段讀完的名稱，所以 A1B 不會吻合 A1BAC，
    /// 角色名稱等其他欄位也不會被誤改。
    /// </summary>
    internal static class GameEventParameterRenamer
    {
        private static readonly Regex KeyPattern =
            new Regex(@"^[A-Za-z_][A-Za-z0-9_.]*$", RegexOptions.CultureInvariant);

        public static bool IsValidKey(string key)
        {
            return !string.IsNullOrEmpty(key) && KeyPattern.IsMatch(key);
        }

        public static string RenameInExpression(string source, string oldKey, string newKey)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }

            StringBuilder builder = null;
            int copiedUntil = 0;
            for (int index = 0; index < source.Length; index++)
            {
                if (source[index] != '$' || index + 1 >= source.Length ||
                    (!char.IsLetter(source[index + 1]) && source[index + 1] != '_'))
                {
                    continue;
                }

                // 與 ExpressionTokenizer 讀名稱的規則相同：一路讀到不是名稱字元為止。
                int start = index + 1;
                int end = start + 1;
                while (end < source.Length &&
                       (char.IsLetterOrDigit(source[end]) ||
                        source[end] == '_' || source[end] == '.'))
                {
                    end++;
                }

                if (end - start == oldKey.Length &&
                    string.CompareOrdinal(source, start, oldKey, 0, oldKey.Length) == 0)
                {
                    builder ??= new StringBuilder(source.Length);
                    builder.Append(source, copiedUntil, start - copiedUntil).Append(newKey);
                    copiedUntil = end;
                }

                index = end - 1;
            }

            if (builder == null)
            {
                return source;
            }

            builder.Append(source, copiedUntil, source.Length - copiedUntil);
            return builder.ToString();
        }

        public static string RenameInCommands(
            string source,
            IReadOnlyDictionary<string, EffectCommandDescriptor> commandsByName,
            string oldKey,
            string newKey)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return source;
            }

            List<GameEventCommandDraft> drafts = GameEventCommandDraftCodec.Parse(source);
            bool changed = false;
            foreach (GameEventCommandDraft draft in drafts)
            {
                if (!commandsByName.TryGetValue(draft.Name, out EffectCommandDescriptor descriptor))
                {
                    continue;
                }

                int count = Math.Min(draft.Arguments.Count, descriptor.Parameters.Count);
                for (int index = 0; index < count; index++)
                {
                    string argument = draft.Arguments[index] ?? string.Empty;
                    string renamed;
                    switch (descriptor.Parameters[index].Kind)
                    {
                        case EffectCommandParameterKind.ParameterKey:
                            renamed = string.Equals(argument, oldKey, StringComparison.Ordinal)
                                ? newKey
                                : argument;
                            break;
                        case EffectCommandParameterKind.NumberExpression:
                        case EffectCommandParameterKind.ConditionExpression:
                            renamed = RenameInExpression(
                                source: argument,
                                oldKey: oldKey,
                                newKey: newKey);
                            break;
                        default:
                            continue;
                    }

                    if (!string.Equals(renamed, argument, StringComparison.Ordinal))
                    {
                        draft.Arguments[index] = renamed;
                        changed = true;
                    }
                }
            }

            // 沒有引用就保留原字串，不因重新序列化改動沒關係的事件檔。
            return changed ? GameEventCommandDraftCodec.Serialize(drafts) : source;
        }

        /// <summary>
        /// 先算出所有要改寫的事件，任何一個無法解析就整批放棄，不寫入任何檔案。
        /// </summary>
        public static IReadOnlyList<GameEventRewrite> Plan(
            GameEventCatalogAsset catalog,
            IReadOnlyList<EffectCommandDescriptor> commandDescriptors,
            string oldKey,
            string newKey)
        {
            Dictionary<string, EffectCommandDescriptor> commandsByName =
                GameEventCommandDescriptorLookup.Create(commandDescriptors);
            GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();
            List<GameEventRewrite> rewrites = new List<GameEventRewrite>();
            List<string> errors = new List<string>();

            foreach (TextAsset eventAsset in (catalog?.Files ?? Array.Empty<TextAsset>())
                         .Where(asset => asset != null))
            {
                string assetPath = AssetDatabase.GetAssetPath(eventAsset);
                try
                {
                    GameEventDocument document = codec.Read(File.ReadAllText(ToFullPath(assetPath)));
                    string condition = RenameInExpression(
                        source: document.Condition,
                        oldKey: oldKey,
                        newKey: newKey);
                    string commands = RenameInCommands(
                        source: document.Commands,
                        commandsByName: commandsByName,
                        oldKey: oldKey,
                        newKey: newKey);
                    if (ReferenceEquals(condition, document.Condition) &&
                        ReferenceEquals(commands, document.Commands))
                    {
                        continue;
                    }

                    GameEventDocument renamed = new GameEventDocument(
                        document.SchemaVersion,
                        document.DocumentGuid,
                        document.DisplayName,
                        document.TriggerTiming,
                        condition,
                        commands);
                    rewrites.Add(new GameEventRewrite(assetPath, document.DisplayName, codec.Write(renamed)));
                }
                catch (Exception exception)
                {
                    errors.Add($"「{assetPath}」：{exception.Message}");
                }
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "有事件無法解析，已取消改名，沒有修改任何檔案：\n" + string.Join("\n", errors));
            }

            return rewrites;
        }

        public static void Write(IReadOnlyList<GameEventRewrite> rewrites)
        {
            foreach (GameEventRewrite rewrite in rewrites)
            {
                File.WriteAllText(ToFullPath(rewrite.AssetPath), rewrite.Json, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(rewrite.AssetPath, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        public static IReadOnlyList<string> FindScenesAndPrefabs()
        {
            return AssetDatabase.FindAssets("t:Scene t:Prefab", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// 改寫所有場景與 prefab 字串欄位中的 `$舊鍵`（例如 ParameterStateBinder 的條件）。
        /// 先改 prefab 檔並重新匯入，場景裡的 prefab instance 才會直接拿到新值、不產生 override；
        /// 已開啟的場景改記憶體中的物件並標記未儲存，未開啟的場景直接改檔。
        /// </summary>
        /// <returns>有改到的資產路徑；已開啟的場景會標註「未儲存」。</returns>
        public static IReadOnlyList<string> RenameInScenesAndPrefabs(string oldKey, string newKey)
        {
            Dictionary<string, Scene> loadedScenes = new Dictionary<string, Scene>(
                StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene.isLoaded && !string.IsNullOrEmpty(scene.path))
                {
                    loadedScenes[scene.path] = scene;
                }
            }

            IReadOnlyList<string> paths = FindScenesAndPrefabs();
            List<string> changed = new List<string>();
            foreach (string path in paths.Where(item =>
                         item.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)))
            {
                if (RenameInFile(path, oldKey: oldKey, newKey: newKey))
                {
                    changed.Add(path);
                }
            }

            foreach (string path in paths.Where(item =>
                         item.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)))
            {
                if (loadedScenes.TryGetValue(path, out Scene scene))
                {
                    if (RenameInLoadedScene(scene, oldKey: oldKey, newKey: newKey))
                    {
                        changed.Add(path + "（已開啟，尚未儲存）");
                    }
                }
                else if (RenameInFile(path, oldKey: oldKey, newKey: newKey))
                {
                    changed.Add(path);
                }
            }

            return changed;
        }

        private static bool RenameInFile(string assetPath, string oldKey, string newKey)
        {
            string fullPath = ToFullPath(assetPath);
            string text = File.ReadAllText(fullPath);
            string renamed = RenameInExpression(source: text, oldKey: oldKey, newKey: newKey);
            if (ReferenceEquals(renamed, text))
            {
                return false;
            }

            File.WriteAllText(fullPath, renamed, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            return true;
        }

        private static bool RenameInLoadedScene(Scene scene, string oldKey, string newKey)
        {
            bool sceneChanged = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                    {
                        continue;
                    }

                    SerializedObject serializedObject = new SerializedObject(component);
                    SerializedProperty property = serializedObject.GetIterator();
                    bool componentChanged = false;
                    bool enterChildren = true;
                    while (property.Next(enterChildren))
                    {
                        enterChildren = property.propertyType != SerializedPropertyType.String;
                        if (property.propertyType != SerializedPropertyType.String)
                        {
                            continue;
                        }

                        string value = property.stringValue;
                        string renamed = RenameInExpression(source: value, oldKey: oldKey, newKey: newKey);
                        if (!ReferenceEquals(renamed, value))
                        {
                            property.stringValue = renamed;
                            componentChanged = true;
                        }
                    }

                    if (componentChanged)
                    {
                        serializedObject.ApplyModifiedProperties();
                        sceneChanged = true;
                    }
                }
            }

            if (sceneChanged)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            return sceneChanged;
        }

        private static string ToFullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }
    }
}
