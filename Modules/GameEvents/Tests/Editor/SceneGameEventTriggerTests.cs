using System;
using System.Threading;
using KahaGameCore.Effects;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.GameFlowSystem.DefaultImplements.Commands;
using KahaGameCore.Parameters;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class SceneGameEventTriggerTests
    {
        [Test]
        public void GameFlowSample_TriggerButtonPersistsSceneTriggerCall()
        {
            Scene scene = EditorSceneManager.OpenScene(
                "Assets/Scenes/GameFlowGame.unity",
                OpenSceneMode.Additive);

            try
            {
                Button triggerButton = null;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int index = 0; index < roots.Length && triggerButton == null; index++)
                {
                    Button[] buttons = roots[index].GetComponentsInChildren<Button>(true);
                    for (int buttonIndex = 0; buttonIndex < buttons.Length; buttonIndex++)
                    {
                        if (buttons[buttonIndex].name == "TriggerButton")
                        {
                            triggerButton = buttons[buttonIndex];
                            break;
                        }
                    }
                }

                Assert.That(triggerButton, Is.Not.Null);
                SceneGameEventTrigger trigger = triggerButton.GetComponent<SceneGameEventTrigger>();
                Assert.That(trigger, Is.Not.Null);
                Assert.That(triggerButton.onClick.GetPersistentEventCount(), Is.EqualTo(1));
                Assert.That(triggerButton.onClick.GetPersistentTarget(0), Is.SameAs(trigger));
                Assert.That(triggerButton.onClick.GetPersistentMethodName(0),
                    Is.EqualTo(nameof(SceneGameEventTrigger.Trigger)));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void TriggerAsync_RunsDirectlyReferencedGameEventFile()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Stage", "Stage", 0, 0, 2)
            });
            GameFlowExpressions expressions = new GameFlowExpressions(parameters);
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(new EffectCommandDefinition(
                "SetParameter",
                "Set Parameter",
                "Parameters",
                new[]
                {
                    new EffectCommandParameterDefinition("key", EffectCommandParameterKind.ParameterKey),
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.Literal)
                },
                new SetParameterCommand(parameters, expressions)));
            GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();
            GameEventRunner runner = new GameEventRunner(
                new GameEventCatalog(Array.Empty<TextAsset>(), codec),
                new EffectRuntime(registry),
                parameters,
                codec);
            TextAsset file = new TextAsset(@"{
  ""SchemaVersion"": 1,
  ""DocumentGuid"": ""50000000-0000-0000-0000-000000000001"",
  ""DisplayName"": ""Scene Trigger"",
  ""TriggerTiming"": """",
  ""Condition"": ""$Stage == 0"",
  ""Priority"": 0,
  ""Commands"": ""SetParameter(Stage,1);""
}");
            GameObject host = new GameObject("SceneGameEventTrigger Host");

            try
            {
                SceneGameEventTrigger trigger = host.AddComponent<SceneGameEventTrigger>();
                trigger.Configure(file);
                trigger.Initialize(runner, new EventContext(CancellationToken.None));

                trigger.TriggerAsync().GetAwaiter().GetResult();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(file);
            }

            Assert.That(parameters.GetInt("Stage"), Is.EqualTo(1));
        }
    }
}
