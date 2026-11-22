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
        public void FindSceneTriggers_ReturnsEmptyForInvalidScene()
        {
            Assert.That(
                GameEventDocumentEditorWindow.FindSceneTriggers(default),
                Is.Empty);
        }

        [Test]
        public void FindSceneTriggers_ReturnsEveryTriggerAndItsMountedEvent()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            TextAsset targetDocument = new TextAsset("target");
            List<GameObject> createdObjects = new List<GameObject>();

            try
            {
                SceneGameEventTrigger trigger3D =
                    CreateTrigger3D("All 3D", targetDocument, createdObjects);
                SceneGameEventTrigger2D trigger2D =
                    CreateTrigger2D("All 2D", null, createdObjects);
                trigger2D.gameObject.SetActive(false);
                StartGameEventTrigger startTrigger =
                    CreateStartTrigger("All Start", targetDocument, createdObjects);

                IReadOnlyList<Component> triggers =
                    GameEventDocumentEditorWindow.FindSceneTriggers(activeScene);

                Assert.That(triggers, Does.Contain(trigger3D));
                Assert.That(triggers, Does.Contain(trigger2D));
                Assert.That(triggers, Does.Contain(startTrigger));
                Assert.That(
                    GameEventDocumentEditorWindow.GetSceneTriggerEventFile(trigger3D),
                    Is.SameAs(targetDocument));
                Assert.That(
                    GameEventDocumentEditorWindow.GetSceneTriggerEventFile(trigger2D),
                    Is.Null);
                Assert.That(
                    GameEventDocumentEditorWindow.GetSceneTriggerKind(startTrigger),
                    Is.EqualTo("Start"));
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
            }
        }

        private static SceneGameEventTrigger CreateTrigger3D(
            string name,
            TextAsset document,
            ICollection<GameObject> createdObjects)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            gameObject.AddComponent<BoxCollider>();
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
            gameObject.AddComponent<BoxCollider2D>();
            SceneGameEventTrigger2D trigger =
                gameObject.AddComponent<SceneGameEventTrigger2D>();
            trigger.Configure(document);
            return trigger;
        }

        private static StartGameEventTrigger CreateStartTrigger(
            string name,
            TextAsset document,
            ICollection<GameObject> createdObjects)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            StartGameEventTrigger trigger =
                gameObject.AddComponent<StartGameEventTrigger>();
            trigger.Configure(document);
            return trigger;
        }
    }
}
