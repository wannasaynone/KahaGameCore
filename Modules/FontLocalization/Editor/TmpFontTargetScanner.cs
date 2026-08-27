using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KahaGameCore.FontLocalization.Editor
{
    internal enum TmpFontTargetAssetKind
    {
        Scene,
        Prefab
    }

    internal sealed class TmpFontTargetScanResult
    {
        public string StableId { get; }
        public string AssetPath { get; }
        public string HierarchyPath { get; }
        public TmpFontTargetAssetKind AssetKind { get; }

        public TmpFontTargetScanResult(
            string stableId,
            string assetPath,
            string hierarchyPath,
            TmpFontTargetAssetKind assetKind)
        {
            StableId = stableId;
            AssetPath = assetPath;
            HierarchyPath = hierarchyPath;
            AssetKind = assetKind;
        }
    }

    internal sealed class TmpFontTargetMutationReport
    {
        private readonly List<string> errors = new();

        public int AddedCount { get; private set; }
        public IReadOnlyList<string> Errors => errors;

        public void RecordAdded()
        {
            AddedCount++;
        }

        public void RecordError(string error)
        {
            errors.Add(error);
        }
    }

    internal static class TmpFontTargetScanner
    {
        public static List<TmpFontTargetScanResult> ScanAll(
            ITmpFontTargetIgnoreStore ignoreStore,
            bool showProgress = false)
        {
            string[] paths = FindScannableAssetPaths();
            var results = new List<TmpFontTargetScanResult>();

            try
            {
                for (int index = 0; index < paths.Length; index++)
                {
                    string path = paths[index];
                    if (showProgress)
                    {
                        EditorUtility.DisplayProgressBar(
                            "TMP Font Target Scanner",
                            path,
                            paths.Length == 0 ? 1f : (float)index / paths.Length);
                    }

                    results.AddRange(ScanPath(path, ignoreStore));
                }
            }
            finally
            {
                if (showProgress)
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            return results
                .OrderBy(result => result.AssetPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(result => result.HierarchyPath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        internal static List<TmpFontTargetScanResult> ScanPaths(
            IEnumerable<string> assetPaths,
            ITmpFontTargetIgnoreStore ignoreStore)
        {
            return assetPaths
                .SelectMany(path => ScanPath(path, ignoreStore))
                .OrderBy(result => result.AssetPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(result => result.HierarchyPath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        internal static TmpFontTargetMutationReport AddTargets(
            IEnumerable<TmpFontTargetScanResult> results)
        {
            var report = new TmpFontTargetMutationReport();
            foreach (IGrouping<string, TmpFontTargetScanResult> group in results.GroupBy(
                         result => result.AssetPath,
                         StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    AddTargetsToAsset(group.Key, group.Select(result => result.StableId), report);
                }
                catch (Exception exception)
                {
                    report.RecordError($"{group.Key}: {exception.Message}");
                }
            }

            return report;
        }

        internal static string GetStableId(TextMeshProUGUI text)
        {
            return GlobalObjectId.GetGlobalObjectIdSlow(text).ToString();
        }

        private static string[] FindScannableAssetPaths()
        {
            IEnumerable<string> scenePaths = AssetDatabase
                .FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase));
            IEnumerable<string> prefabPaths = AssetDatabase
                .FindAssets("t:Prefab", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase));

            return scenePaths
                .Concat(prefabPaths)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static IEnumerable<TmpFontTargetScanResult> ScanPath(
            string assetPath,
            ITmpFontTargetIgnoreStore ignoreStore)
        {
            string extension = Path.GetExtension(assetPath);
            if (string.Equals(extension, ".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return ScanPrefab(assetPath, ignoreStore);
            }

            if (string.Equals(extension, ".unity", StringComparison.OrdinalIgnoreCase))
            {
                return ScanScene(assetPath, ignoreStore);
            }

            return Array.Empty<TmpFontTargetScanResult>();
        }

        private static List<TmpFontTargetScanResult> ScanPrefab(
            string assetPath,
            ITmpFontTargetIgnoreStore ignoreStore)
        {
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(assetPath);
                return Collect(
                    new[] { root },
                    assetPath,
                    TmpFontTargetAssetKind.Prefab,
                    ignoreStore);
            }
            finally
            {
                if (root != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static List<TmpFontTargetScanResult> ScanScene(
            string assetPath,
            ITmpFontTargetIgnoreStore ignoreStore)
        {
            Scene scene = default;
            try
            {
                scene = EditorSceneManager.OpenPreviewScene(assetPath);
                return Collect(
                    scene.GetRootGameObjects(),
                    assetPath,
                    TmpFontTargetAssetKind.Scene,
                    ignoreStore);
            }
            finally
            {
                if (scene.IsValid())
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }

        private static List<TmpFontTargetScanResult> Collect(
            IEnumerable<GameObject> roots,
            string assetPath,
            TmpFontTargetAssetKind assetKind,
            ITmpFontTargetIgnoreStore ignoreStore)
        {
            var results = new List<TmpFontTargetScanResult>();
            foreach (TextMeshProUGUI text in roots.SelectMany(root =>
                         root.GetComponentsInChildren<TextMeshProUGUI>(true)))
            {
                if (text.GetComponent<LocalizedFontTarget>() != null ||
                    IsOwnedByAnotherAsset(text, assetPath))
                {
                    continue;
                }

                string stableId = GetStableId(text);
                if (string.IsNullOrEmpty(stableId) || ignoreStore.Contains(stableId))
                {
                    continue;
                }

                results.Add(new TmpFontTargetScanResult(
                    stableId,
                    assetPath,
                    GetHierarchyPath(text.transform),
                    assetKind));
            }

            return results;
        }

        private static bool IsOwnedByAnotherAsset(
            TextMeshProUGUI text,
            string currentAssetPath)
        {
            TextMeshProUGUI source =
                PrefabUtility.GetCorrespondingObjectFromSource(text);
            if (source == null)
            {
                return false;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            return !string.IsNullOrEmpty(sourcePath) &&
                   !string.Equals(
                       sourcePath,
                       currentAssetPath,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var segments = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                segments.Push($"{current.name}[{current.GetSiblingIndex()}]");
                current = current.parent;
            }

            return string.Join("/", segments);
        }

        private static void AddTargetsToAsset(
            string assetPath,
            IEnumerable<string> stableIds,
            TmpFontTargetMutationReport report)
        {
            var ids = new HashSet<string>(stableIds, StringComparer.Ordinal);
            if (assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                AddTargetsToPrefab(assetPath, ids, report);
                return;
            }

            if (assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                AddTargetsToScene(assetPath, ids, report);
            }
        }

        private static void AddTargetsToPrefab(
            string assetPath,
            ISet<string> stableIds,
            TmpFontTargetMutationReport report)
        {
            PrefabStage openStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (openStage != null && string.Equals(
                    openStage.assetPath,
                    assetPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Prefab is open in Prefab Mode. Save and close it before using Scanner actions.");
            }

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(assetPath);
                int added = AddTargetsInRoots(new[] { root }, stableIds);
                if (added == 0)
                {
                    return;
                }

                PrefabUtility.SaveAsPrefabAsset(root, assetPath, out bool success);
                if (!success)
                {
                    throw new InvalidOperationException("Unity could not save the Prefab.");
                }

                for (int index = 0; index < added; index++)
                {
                    report.RecordAdded();
                }
            }
            finally
            {
                if (root != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void AddTargetsToScene(
            string assetPath,
            ISet<string> stableIds,
            TmpFontTargetMutationReport report)
        {
            Scene scene = SceneManager.GetSceneByPath(assetPath);
            bool openedForMutation = !scene.IsValid() || !scene.isLoaded;
            Scene originalActiveScene = SceneManager.GetActiveScene();

            if (!openedForMutation && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Scene is open with unsaved changes. Save or close it before using Scanner actions.");
            }

            try
            {
                if (openedForMutation)
                {
                    scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
                }

                int added = AddTargetsInRoots(scene.GetRootGameObjects(), stableIds);
                if (added == 0)
                {
                    return;
                }

                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("Unity could not save the Scene.");
                }

                for (int index = 0; index < added; index++)
                {
                    report.RecordAdded();
                }
            }
            finally
            {
                if (openedForMutation && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(originalActiveScene);
                }
            }
        }

        private static int AddTargetsInRoots(
            IEnumerable<GameObject> roots,
            ISet<string> stableIds)
        {
            int added = 0;
            foreach (TextMeshProUGUI text in roots.SelectMany(root =>
                         root.GetComponentsInChildren<TextMeshProUGUI>(true)))
            {
                if (!stableIds.Contains(GetStableId(text)) ||
                    text.GetComponent<LocalizedFontTarget>() != null)
                {
                    continue;
                }

                text.gameObject.AddComponent<LocalizedFontTarget>();
                added++;
            }

            return added;
        }
    }
}
