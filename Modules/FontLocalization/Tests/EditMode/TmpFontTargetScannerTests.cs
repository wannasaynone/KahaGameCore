using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using KahaGameCore.FontLocalization.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KahaGameCore.FontLocalization.Tests
{
    public sealed class TmpFontTargetScannerTests
    {
        private const string TestRoot = "Assets/__TmpFontTargetScannerTests";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.CreateFolder("Assets", "__TmpFontTargetScannerTests");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestRoot);
        }

        [Test]
        public void ScanPrefab_ReturnsTmpWithoutTarget()
        {
            string prefabPath = CreatePrefab("MissingTarget", addTarget: false);

            List<TmpFontTargetScanResult> results = TmpFontTargetScanner.ScanPaths(
                new[] { prefabPath },
                new MemoryIgnoreStore());

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].AssetPath, Is.EqualTo(prefabPath));
        }

        [Test]
        public void ScanPrefab_SkipsTmpWithTarget()
        {
            string prefabPath = CreatePrefab("HasTarget", addTarget: true);

            List<TmpFontTargetScanResult> results = TmpFontTargetScanner.ScanPaths(
                new[] { prefabPath },
                new MemoryIgnoreStore());

            Assert.That(results, Is.Empty);
        }

        [Test]
        public void IgnoreStore_RemovesResultFromFutureScans()
        {
            string prefabPath = CreatePrefab("IgnoredTarget", addTarget: false);
            var ignoreStore = new MemoryIgnoreStore();
            TmpFontTargetScanResult result = TmpFontTargetScanner.ScanPaths(
                new[] { prefabPath },
                ignoreStore).Single();

            ignoreStore.Ignore(new[] { result });

            Assert.That(
                TmpFontTargetScanner.ScanPaths(new[] { prefabPath }, ignoreStore),
                Is.Empty);
        }

        [Test]
        public void AddTargets_UpdatesPrefabAndClearsFinding()
        {
            string prefabPath = CreatePrefab("AddTarget", addTarget: false);
            var ignoreStore = new MemoryIgnoreStore();
            List<TmpFontTargetScanResult> results = TmpFontTargetScanner.ScanPaths(
                new[] { prefabPath },
                ignoreStore);

            TmpFontTargetMutationReport report = TmpFontTargetScanner.AddTargets(results);

            Assert.That(report.Errors, Is.Empty);
            Assert.That(report.AddedCount, Is.EqualTo(1));
            Assert.That(
                TmpFontTargetScanner.ScanPaths(new[] { prefabPath }, ignoreStore),
                Is.Empty);
        }

        [Test]
        public void ScanScene_FindsInactiveTmp()
        {
            string scenePath = CreateScene("InactiveText", inactive: true);

            List<TmpFontTargetScanResult> results = TmpFontTargetScanner.ScanPaths(
                new[] { scenePath },
                new MemoryIgnoreStore());

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].HierarchyPath, Does.Contain("InactiveText"));
        }

        [Test]
        public void AddTargets_UpdatesSceneAndClearsFinding()
        {
            string scenePath = CreateScene("SceneTarget", inactive: false);
            var ignoreStore = new MemoryIgnoreStore();
            List<TmpFontTargetScanResult> results = TmpFontTargetScanner.ScanPaths(
                new[] { scenePath },
                ignoreStore);

            TmpFontTargetMutationReport report = TmpFontTargetScanner.AddTargets(results);

            Assert.That(report.Errors, Is.Empty);
            Assert.That(report.AddedCount, Is.EqualTo(1));
            Assert.That(
                TmpFontTargetScanner.ScanPaths(new[] { scenePath }, ignoreStore),
                Is.Empty);
        }

        private static string CreatePrefab(string name, bool addTarget)
        {
            var root = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            if (addTarget)
            {
                root.AddComponent<LocalizedFontTarget>();
            }

            string path = $"{TestRoot}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return path;
        }

        private static string CreateScene(string name, bool inactive)
        {
            string scenePath = $"{TestRoot}/{name}.unity";
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            SceneManager.MoveGameObjectToScene(textObject, scene);
            textObject.SetActive(!inactive);
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            return scenePath;
        }

        private sealed class MemoryIgnoreStore : ITmpFontTargetIgnoreStore
        {
            private readonly List<TmpFontTargetIgnoredEntry> entries = new();

            public IReadOnlyList<TmpFontTargetIgnoredEntry> Entries => entries;

            public bool Contains(string stableId)
            {
                return entries.Any(entry => entry.StableId == stableId);
            }

            public void Ignore(IEnumerable<TmpFontTargetScanResult> results)
            {
                entries.AddRange(results.Select(result =>
                    new TmpFontTargetIgnoredEntry(
                        result.StableId,
                        result.AssetPath,
                        result.HierarchyPath)));
            }

            public bool Remove(string stableId)
            {
                return entries.RemoveAll(entry => entry.StableId == stableId) > 0;
            }
        }
    }
}
