using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KahaGameCore.Effects;
using KahaGameCore.GameEvents.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class GameEventReferenceFinderTests
    {
        private const string TestRoot = "Assets/__GameEventReferenceFinderTests";
        private const string TargetGuid = "40000000-0000-0000-0000-000000000001";
        private const string OtherGuid = "40000000-0000-0000-0000-000000000002";

        private static readonly IReadOnlyList<EffectCommandDescriptor> Commands = new[]
        {
            Describe(
                "TriggerEvent",
                new EffectCommandParameterDefinition(
                    name: "event",
                    kind: EffectCommandParameterKind.AssetKey,
                    optionSourceKey: GameEventEffectCommandModule.EventOptionSourceKey)),
            Describe(
                "Say",
                new EffectCommandParameterDefinition(
                    name: "text",
                    kind: EffectCommandParameterKind.Literal))
        };

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestRoot);
        }

        [Test]
        public void FindCommandReferences_ListsEventsWhoseEventParameterHoldsTheGuid()
        {
            List<string> warnings = new List<string>();

            IReadOnlyList<GameEventCommandReference> references =
                GameEventReferenceFinder.FindCommandReferences(
                    TargetGuid,
                    new[]
                    {
                        Document(
                            displayName: "Direct",
                            commands: "TriggerEvent(" + TargetGuid + ");"),
                        Document(
                            displayName: "Nested",
                            commands: "If($A > 0);TriggerEvent(" + TargetGuid.ToUpperInvariant() +
                                ");EndIf();"),
                        Document(
                            displayName: "Other",
                            commands: "TriggerEvent(" + OtherGuid + ");"),
                        Document(
                            displayName: "Literal",
                            commands: "Say(" + TargetGuid + ");")
                    },
                    Commands,
                    warnings);

            Assert.That(
                references.Select(reference => reference.EventDisplayName),
                Is.EqualTo(new[] { "Direct", "Nested" }));
            Assert.That(references[0].CommandNames, Is.EqualTo(new[] { "TriggerEvent" }));
            Assert.That(warnings, Is.Empty);
        }

        [Test]
        public void FindCommandReferences_UnparsableCommands_AddsWarningAndSkipsEvent()
        {
            List<string> warnings = new List<string>();

            IReadOnlyList<GameEventCommandReference> references =
                GameEventReferenceFinder.FindCommandReferences(
                    TargetGuid,
                    new[] { Document(displayName: "Broken", commands: "TriggerEvent(") },
                    Commands,
                    warnings);

            Assert.That(references, Is.Empty);
            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("Broken"));
        }

        [Test]
        public void FindCommandReferences_InvalidTargetGuid_ReturnsEmpty()
        {
            Assert.That(
                GameEventReferenceFinder.FindCommandReferences(
                    "not-a-guid",
                    new[] { Document(displayName: "Any", commands: "TriggerEvent(not-a-guid);") },
                    Commands,
                    new List<string>()),
                Is.Empty);
        }

        [Test]
        public void FindCandidatePaths_FollowsNestedPrefabsButNotScriptableObjects()
        {
            const string eventPath = "Assets/Events/Target.gameevent.json";
            Dictionary<string, string[]> graph = new Dictionary<string, string[]>
            {
                ["Assets/Prefabs/Slime.prefab"] = new[] { eventPath },
                ["Assets/Prefabs/SlimeGroup.prefab"] = new[] { "Assets/Prefabs/Slime.prefab" },
                ["Assets/Prefabs/Unrelated.prefab"] = Array.Empty<string>(),
                ["Assets/Scenes/WithGroup.unity"] = new[] { "Assets/Prefabs/SlimeGroup.prefab" },
                ["Assets/Scenes/Direct.unity"] = new[] { eventPath },
                ["Assets/Scenes/CatalogOnly.unity"] = new[] { "Assets/Data/Catalog.asset" },
                ["Assets/Data/Catalog.asset"] = new[] { eventPath },
                ["Assets/Data/Unrelated.asset"] = Array.Empty<string>()
            };

            IReadOnlyList<string> candidates = GameEventReferenceFinder.FindCandidatePaths(
                eventPath,
                graph.Keys,
                path => graph[path]);

            Assert.That(
                candidates,
                Is.EqualTo(new[]
                {
                    "Assets/Scenes/Direct.unity",
                    "Assets/Scenes/WithGroup.unity",
                    "Assets/Prefabs/Slime.prefab",
                    "Assets/Prefabs/SlimeGroup.prefab",
                    "Assets/Data/Catalog.asset"
                }));
        }

        [Test]
        public void FindInHierarchy_ListsEveryFieldPointingAtTheEventIncludingInactiveObjects()
        {
            TextAsset target = new TextAsset("target");
            TextAsset other = new TextAsset("other");
            GameObject root = new GameObject("Root");
            try
            {
                root.AddComponent<StartGameEventTrigger>().Configure(target);
                GameObject otherChild = new GameObject("Other");
                otherChild.transform.SetParent(root.transform);
                otherChild.AddComponent<StartGameEventTrigger>().Configure(other);
                GameObject hiddenChild = new GameObject("Hidden");
                hiddenChild.transform.SetParent(root.transform);
                hiddenChild.SetActive(false);
                hiddenChild.AddComponent<StartGameEventTrigger>().Configure(target);

                IReadOnlyList<GameEventObjectReference> references =
                    GameEventReferenceFinder.FindInHierarchy(
                        target,
                        new[] { root },
                        GameEventObjectReferenceSource.Prefab,
                        "Assets/Test.prefab");

                Assert.That(
                    references.Select(reference => reference.ObjectPath),
                    Is.EqualTo(new[] { "Root", "Root/Hidden" }));
                Assert.That(
                    references.Select(reference => reference.ComponentTypeName),
                    Is.All.EqualTo(nameof(StartGameEventTrigger)));
                Assert.That(
                    references.Select(reference => reference.PropertyPath),
                    Is.All.EqualTo("gameEventFile"));
                Assert.That(references[0].SiblingPath, Is.Empty);
                Assert.That(references[1].SiblingPath, Is.EqualTo(new[] { 1 }));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void FindInProject_FindsSceneAndPrefabReferencesAndClosesTheLoadedScene()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.CreateFolder("Assets", "__GameEventReferenceFinderTests");
            TextAsset target = CreateTextAsset("Target.gameevent.json");
            TextAsset other = CreateTextAsset("Other.gameevent.json");

            GameObject prefabSource = new GameObject("TriggerPrefab");
            prefabSource.AddComponent<StartGameEventTrigger>().Configure(target);
            string prefabPath = TestRoot + "/TriggerPrefab.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabSource, prefabPath);
            Object.DestroyImmediate(prefabSource);

            string scenePath = TestRoot + "/References.unity";
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            GameObject sceneTrigger = new GameObject("SceneTrigger");
            SceneManager.MoveGameObjectToScene(sceneTrigger, scene);
            sceneTrigger.AddComponent<StartGameEventTrigger>().Configure(target);
            GameObject otherTrigger = new GameObject("OtherTrigger");
            SceneManager.MoveGameObjectToScene(otherTrigger, scene);
            otherTrigger.AddComponent<StartGameEventTrigger>().Configure(other);
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            int sceneCount = SceneManager.sceneCount;

            GameEventProjectReferences result = GameEventReferenceFinder.FindInProject(target);

            Assert.That(result.Warnings, Is.Empty);
            Assert.That(
                result.References
                    .Where(reference => reference.AssetPath == scenePath)
                    .Select(reference => reference.ObjectPath),
                Is.EquivalentTo(new[] { "TriggerPrefab", "SceneTrigger" }));
            Assert.That(
                result.References
                    .Where(reference => reference.AssetPath == prefabPath)
                    .Select(reference => reference.ObjectPath),
                Is.EqualTo(new[] { "TriggerPrefab" }));
            Assert.That(result.References.Select(reference => reference.AssetPath).Distinct(),
                Is.EquivalentTo(new[] { scenePath, prefabPath }));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCount));
            Assert.That(SceneManager.GetSceneByPath(scenePath).IsValid(), Is.False);
        }

        private static TextAsset CreateTextAsset(string fileName)
        {
            string assetPath = TestRoot + "/" + fileName;
            File.WriteAllText(
                Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath)),
                "{}");
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
        }

        private static OpenGameEventUsageDocument Document(string displayName, string commands)
        {
            return new OpenGameEventUsageDocument(
                eventAsset: null,
                assetPath: "Assets/" + displayName + ".gameevent.json",
                displayName: displayName,
                condition: string.Empty,
                commands: commands);
        }

        private static EffectCommandDescriptor Describe(
            string name,
            params EffectCommandParameterDefinition[] parameters)
        {
            return new EffectCommandDescriptor(
                name: name,
                displayName: name,
                category: "Test",
                parameters: parameters);
        }
    }
}
