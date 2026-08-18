using System.Collections.Generic;
using KahaGameCore.GameEvents.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class GameEventSceneReferenceTests
    {
        [Test]
        public void FindSceneTriggersReferencing_ReturnsEmptyForInvalidScene()
        {
            TextAsset document = new TextAsset("target");

            try
            {
                Assert.That(
                    GameEventDocumentEditorWindow.FindSceneTriggersReferencing(
                        document,
                        default),
                    Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(document);
            }
        }

        [Test]
        public void FindSceneTriggersReferencing_ReturnsMatching3DAnd2DTriggersOnly()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            TextAsset targetDocument = new TextAsset("target");
            TextAsset otherDocument = new TextAsset("other");
            List<GameObject> createdObjects = new List<GameObject>();

            try
            {
                SceneGameEventTrigger matching3D =
                    CreateTrigger3D("Matching 3D", targetDocument, createdObjects);
                SceneGameEventTrigger2D matching2D =
                    CreateTrigger2D("Matching 2D", targetDocument, createdObjects);
                matching2D.gameObject.SetActive(false);
                CreateTrigger3D("Different Document", otherDocument, createdObjects);

                IReadOnlyList<Component> references =
                    GameEventDocumentEditorWindow.FindSceneTriggersReferencing(
                        targetDocument,
                        activeScene);

                Assert.That(references, Has.Count.EqualTo(2));
                Assert.That(references, Does.Contain(matching3D));
                Assert.That(references, Does.Contain(matching2D));
            }
            finally
            {
                for (int index = 0; index < createdObjects.Count; index++)
                {
                    if (createdObjects[index] != null)
                    {
                        Object.DestroyImmediate(createdObjects[index]);
                    }
                }

                Object.DestroyImmediate(targetDocument);
                Object.DestroyImmediate(otherDocument);
            }
        }

        private static SceneGameEventTrigger CreateTrigger3D(
            string name,
            TextAsset document,
            ICollection<GameObject> createdObjects)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            SceneGameEventTrigger trigger =
                gameObject.AddComponent<SceneGameEventTrigger>();
            trigger.Configure(document);
            return trigger;
        }

        private static SceneGameEventTrigger2D CreateTrigger2D(
            string name,
            TextAsset document,
            ICollection<GameObject> createdObjects)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            SceneGameEventTrigger2D trigger =
                gameObject.AddComponent<SceneGameEventTrigger2D>();
            trigger.Configure(document);
            return trigger;
        }
    }
}
