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

        [Test]
        public void FindSceneTriggersReferencing_ReturnsOnlyTriggersUsingCurrentEvent()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            TextAsset currentEvent = new TextAsset("current");
            TextAsset otherEvent = new TextAsset("other");
            List<GameObject> createdObjects = new List<GameObject>();

            try
            {
                SceneGameEventTrigger matching3D =
                    CreateTrigger3D("Matching 3D", currentEvent, createdObjects);
                SceneGameEventTrigger2D matching2D =
                    CreateTrigger2D("Matching 2D", currentEvent, createdObjects);
                StartGameEventTrigger matchingStart =
                    CreateStartTrigger("Matching Start", currentEvent, createdObjects);
                CreateTrigger3D("Other Event", otherEvent, createdObjects);
                CreateTrigger2D("Unassigned", null, createdObjects);

                IReadOnlyList<Component> references =
                    GameEventDocumentEditorWindow.FindSceneTriggersReferencing(
                        currentEvent,
                        activeScene);

                Assert.That(references, Has.Count.EqualTo(3));
                Assert.That(references, Does.Contain(matching3D));
                Assert.That(references, Does.Contain(matching2D));
                Assert.That(references, Does.Contain(matchingStart));
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

                Object.DestroyImmediate(currentEvent);
                Object.DestroyImmediate(otherEvent);
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
