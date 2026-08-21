using System.Collections.Generic;
using KahaGameCore.GameEvents.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class SceneGameEventTriggerEditorTests
    {
        [Test]
        public void SceneTrigger_DefaultTriggerLayerIsNothing()
        {
            AssertDefaultTriggerLayerIsNothing<SceneGameEventTrigger>(
                typeof(SceneGameEventTriggerEditor));
        }

        [Test]
        public void SceneTrigger2D_DefaultTriggerLayerIsNothing()
        {
            AssertDefaultTriggerLayerIsNothing<SceneGameEventTrigger2D>(
                typeof(SceneGameEventTrigger2DEditor));
        }

        [Test]
        public void EmptyTriggerLayer_UsesRequestedErrorMessage()
        {
            GameObject host = new GameObject("Scene Trigger");

            try
            {
                SceneGameEventTrigger trigger =
                    host.AddComponent<SceneGameEventTrigger>();
                SerializedObject serializedTrigger = new SerializedObject(trigger);
                SerializedProperty layers =
                    serializedTrigger.FindProperty("triggeringLayers");

                Assert.That(
                    SceneGameEventTriggerEditorBase
                        .ShouldShowEmptyTriggerLayerError(layers),
                    Is.True);
                Assert.That(
                    SceneGameEventTriggerEditorBase.EmptyTriggerLayerMessage,
                    Is.EqualTo("現在沒有任何物件可以觸發本trigger"));

                layers.intValue = 1;
                Assert.That(
                    SceneGameEventTriggerEditorBase
                        .ShouldShowEmptyTriggerLayerError(layers),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void EmptyGameEvent_UsesRequestedErrorMessage()
        {
            GameObject host = new GameObject("Scene Trigger");
            TextAsset selectedEvent = CreateEvent(
                "50000000-0000-0000-0000-000000000010",
                "Selected Event");

            try
            {
                SceneGameEventTrigger trigger =
                    host.AddComponent<SceneGameEventTrigger>();
                SerializedObject serializedTrigger = new SerializedObject(trigger);
                SerializedProperty gameEvent =
                    serializedTrigger.FindProperty("gameEventFile");

                Assert.That(
                    SceneGameEventTriggerEditorBase
                        .ShouldShowEmptyGameEventError(gameEvent),
                    Is.True);
                Assert.That(
                    SceneGameEventTriggerEditorBase.EmptyGameEventMessage,
                    Is.EqualTo("這個觸發器還未選擇任何事件觸發"));

                gameEvent.objectReferenceValue = selectedEvent;
                Assert.That(
                    SceneGameEventTriggerEditorBase
                        .ShouldShowEmptyGameEventError(gameEvent),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(selectedEvent);
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void GameEventOptions_ComeFromSelectedCatalogEventList()
        {
            GameEventCatalogAsset catalog =
                ScriptableObject.CreateInstance<GameEventCatalogAsset>();
            TextAsset first = CreateEvent(
                "50000000-0000-0000-0000-000000000011",
                "First Event");
            TextAsset second = CreateEvent(
                "50000000-0000-0000-0000-000000000012",
                "Second Event");

            try
            {
                SerializedObject serializedCatalog = new SerializedObject(catalog);
                SerializedProperty files = serializedCatalog.FindProperty("files");
                files.arraySize = 2;
                files.GetArrayElementAtIndex(0).objectReferenceValue = first;
                files.GetArrayElementAtIndex(1).objectReferenceValue = second;
                serializedCatalog.ApplyModifiedPropertiesWithoutUndo();

                IReadOnlyList<GameEventTriggerOption>
                    options = SceneGameEventTriggerEditorBase.BuildGameEventOptions(
                        catalog,
                        null);

                Assert.That(options.Count, Is.EqualTo(3));
                Assert.That(options[0].Asset, Is.Null);
                Assert.That(options[0].Label, Is.EqualTo("未選擇"));
                Assert.That(options[1].Asset, Is.SameAs(first));
                Assert.That(
                    options[1].Label,
                    Is.EqualTo("First Event (FirstEvent)"));
                Assert.That(options[2].Asset, Is.SameAs(second));
                Assert.That(
                    options[2].Label,
                    Is.EqualTo("Second Event (SecondEvent)"));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void GameEventOutsideCatalog_RemainsVisibleAsInvalidOption()
        {
            GameEventCatalogAsset catalog =
                ScriptableObject.CreateInstance<GameEventCatalogAsset>();
            TextAsset outside = CreateEvent(
                "50000000-0000-0000-0000-000000000013",
                "Outside Event");

            try
            {
                IReadOnlyList<GameEventTriggerOption>
                    options = SceneGameEventTriggerEditorBase.BuildGameEventOptions(
                        catalog,
                        outside);

                Assert.That(options.Count, Is.EqualTo(2));
                Assert.That(options[1].Asset, Is.SameAs(outside));
                Assert.That(
                    options[1].Label,
                    Is.EqualTo("OutsideEvent（不在目前事件清單）"));
            }
            finally
            {
                Object.DestroyImmediate(outside);
                Object.DestroyImmediate(catalog);
            }
        }

        private static void AssertDefaultTriggerLayerIsNothing<T>(
            System.Type expectedEditorType)
            where T : MonoBehaviour
        {
            GameObject host = new GameObject(typeof(T).Name);
            UnityEditor.Editor inspector = null;

            try
            {
                T trigger = host.AddComponent<T>();
                inspector = UnityEditor.Editor.CreateEditor(trigger);
                SerializedObject serializedTrigger = new SerializedObject(trigger);
                SerializedProperty layers =
                    serializedTrigger.FindProperty("triggeringLayers");

                Assert.That(inspector.GetType(), Is.EqualTo(expectedEditorType));
                Assert.That(layers, Is.Not.Null);
                Assert.That(layers.intValue, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(inspector);
                Object.DestroyImmediate(host);
            }
        }

        private static TextAsset CreateEvent(string documentGuid, string displayName)
        {
            return new TextAsset($@"{{
  ""SchemaVersion"": 2,
  ""DocumentGuid"": ""{documentGuid}"",
  ""DisplayName"": ""{displayName}"",
  ""TriggerTiming"": """",
  ""Condition"": """",
  ""Commands"": """"
}}")
            {
                name = displayName.Replace(" ", string.Empty)
            };
        }
    }
}
