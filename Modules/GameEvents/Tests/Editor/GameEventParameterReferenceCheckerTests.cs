using System.Linq;
using KahaGameCore.GameEvents.Editor;
using KahaGameCore.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class GameEventParameterReferenceCheckerTests
    {
        [Test]
        public void FindInHierarchy_ListsUnknownKeysWithObjectAndProperty()
        {
            GameObject root = new GameObject("Root");
            try
            {
                GameObject marker = new GameObject("Marker");
                marker.transform.SetParent(root.transform);
                GameObject icon = new GameObject("Icon");
                icon.transform.SetParent(marker.transform);
                ParameterStateBinder binder = marker.AddComponent<ParameterStateBinder>();
                binder.Configure(new[]
                {
                    new ParameterChildConditionBinding(icon, "$Known == 1 && $Missing == 0")
                });

                ParameterReferenceIssue[] issues = GameEventParameterReferenceChecker
                    .FindInHierarchy(
                        roots: new[] { root },
                        definedKeys: new[] { "Known" },
                        source: GameEventObjectReferenceSource.Scene,
                        assetPath: "Assets/Test.unity")
                    .ToArray();

                Assert.That(issues.Select(issue => issue.Key), Is.EqualTo(new[] { "Missing" }));
                GameEventObjectReference reference = issues[0].ObjectReference;
                Assert.That(reference.ObjectPath, Is.EqualTo("Root/Marker"));
                Assert.That(reference.ComponentTypeName, Is.EqualTo(nameof(ParameterStateBinder)));
                Assert.That(reference.PropertyPath, Does.Contain("condition"));
                Assert.That(reference.SiblingPath, Is.EqualTo(new[] { 0 }));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FindInSceneText_LocatesComponentByFileIdWithHierarchyPath()
        {
            const string scene = @"%YAML 1.1
--- !u!1 &10
GameObject:
  m_Name: Parent
--- !u!4 &11
Transform:
  m_GameObject: {fileID: 10}
  m_Father: {fileID: 0}
--- !u!1 &20
GameObject:
  m_Name: Binder
--- !u!4 &21
Transform:
  m_GameObject: {fileID: 20}
  m_Father: {fileID: 11}
--- !u!114 &22
MonoBehaviour:
  m_GameObject: {fileID: 20}
  m_EditorClassIdentifier: KahaGameCore.Modules.Presentation::KahaGameCore.Presentation.ParameterStateBinder
  bindings:
  - target: {fileID: 0}
    condition: $Missing==0
  - target: {fileID: 0}
    condition: $Missing==1 && $Known==1
";

            ParameterReferenceIssue[] issues = GameEventParameterReferenceChecker
                .FindInSceneText(
                    scenePath: "Assets/Test.unity",
                    sceneGuid: "0123456789abcdef0123456789abcdef",
                    text: scene,
                    definedKeys: new[] { "Known" })
                .ToArray();

            Assert.That(issues.Select(issue => issue.Key), Is.EqualTo(new[] { "Missing" }));
            GameEventObjectReference reference = issues[0].ObjectReference;
            Assert.That(reference.ObjectPath, Is.EqualTo("Parent/Binder"));
            Assert.That(reference.ComponentTypeName, Is.EqualTo("ParameterStateBinder"));
            Assert.That(reference.PropertyPath, Is.EqualTo("condition"));
            Assert.That(
                reference.GlobalObjectId,
                Is.EqualTo("GlobalObjectId_V1-2-0123456789abcdef0123456789abcdef-22-0"));
        }

        [Test]
        public void SuggestKeys_ReturnsClosestKeysFirst()
        {
            string[] suggestions = GameEventParameterReferenceChecker
                .SuggestKeys(
                    missingKey: "Act2_SlimeCont",
                    definedKeys: new[] { "Act2_SlimeCount", "Act1_SlimeCount", "Unrelated" })
                .ToArray();

            Assert.That(suggestions, Is.EqualTo(new[] { "Act2_SlimeCount", "Act1_SlimeCount" }));
        }

        [Test]
        public void SuggestKeys_IgnoresDistantKeys()
        {
            Assert.That(
                GameEventParameterReferenceChecker.SuggestKeys(
                    missingKey: "HansQuestStatus",
                    definedKeys: new[] { "MaggieVisibility" }),
                Is.Empty);
        }
    }
}
