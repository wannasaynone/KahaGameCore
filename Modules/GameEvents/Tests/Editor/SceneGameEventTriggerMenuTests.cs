using KahaGameCore.GameEvents.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class SceneGameEventTriggerMenuTests
    {
        [Test]
        public void TriggerMenus_AreGroupedUnderGameEventParts()
        {
            Assert.That(
                SceneGameEventTriggerMenu.SceneTriggerMenuPath,
                Is.EqualTo("GameObject/Add Game Event Parts/Add Scene Trigger"));
            Assert.That(
                SceneGameEventTriggerMenu.SceneTrigger2DMenuPath,
                Is.EqualTo("GameObject/Add Game Event Parts/Add Scene Trigger 2D"));
        }

        [Test]
        public void AddSceneTrigger_CreatesReady3DTriggerChild()
        {
            GameObject parent = new GameObject("Parent") { layer = 8 };
            Object previousSelection = Selection.activeObject;

            try
            {
                SceneGameEventTrigger trigger =
                    SceneGameEventTriggerMenu.AddSceneTrigger(parent);

                Assert.That(trigger.name, Is.EqualTo("Scene Trigger"));
                Assert.That(trigger.transform.parent, Is.EqualTo(parent.transform));
                Assert.That(trigger.gameObject.layer, Is.EqualTo(parent.layer));
                Assert.That(Selection.activeGameObject, Is.EqualTo(trigger.gameObject));

                BoxCollider collider = trigger.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    Assert.Fail("Scene Trigger requires a BoxCollider.");
                }
                Assert.That(collider.isTrigger, Is.True);

                Rigidbody body = trigger.GetComponent<Rigidbody>();
                if (body == null)
                {
                    Assert.Fail("Scene Trigger requires a Rigidbody.");
                }
                Assert.That(body.isKinematic, Is.True);
                Assert.That(body.useGravity, Is.False);
            }
            finally
            {
                Selection.activeObject = previousSelection;
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void AddSceneTrigger2D_CreatesReady2DTriggerChild()
        {
            GameObject parent = new GameObject("Parent") { layer = 9 };
            Object previousSelection = Selection.activeObject;

            try
            {
                SceneGameEventTrigger2D trigger =
                    SceneGameEventTriggerMenu.AddSceneTrigger2D(parent);

                Assert.That(trigger.name, Is.EqualTo("Scene Trigger 2D"));
                Assert.That(trigger.transform.parent, Is.EqualTo(parent.transform));
                Assert.That(trigger.gameObject.layer, Is.EqualTo(parent.layer));
                Assert.That(Selection.activeGameObject, Is.EqualTo(trigger.gameObject));

                BoxCollider2D collider = trigger.GetComponent<BoxCollider2D>();
                if (collider == null)
                {
                    Assert.Fail("Scene Trigger 2D requires a BoxCollider2D.");
                }
                Assert.That(collider.isTrigger, Is.True);

                Rigidbody2D body = trigger.GetComponent<Rigidbody2D>();
                if (body == null)
                {
                    Assert.Fail("Scene Trigger 2D requires a Rigidbody2D.");
                }
                Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
                Assert.That(body.gravityScale, Is.Zero);
            }
            finally
            {
                Selection.activeObject = previousSelection;
                Object.DestroyImmediate(parent);
            }
        }
    }
}
