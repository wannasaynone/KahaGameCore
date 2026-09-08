using System;
using System.Reflection;
using System.Threading;
using KahaGameCore.Effects;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.Parameters;
using KahaGameCore.Parameters.EffectsIntegration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class SceneGameEventTriggerTests
    {
        [Test]
        public void OnTriggerEnter_RunsDirectlyReferencedGameEventFile()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Stage", "Stage", 0, 0, 2)
            });
            GameFlowExpressions expressions = new GameFlowExpressions(parameters);
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(new EffectCommandDefinition(
                name: "SetParameter",
                displayName: "Set Parameter",
                category: "Parameters",
                new[]
                {
                    new EffectCommandParameterDefinition("key", EffectCommandParameterKind.ParameterKey),
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.Literal)
                },
                new SetParameterCommand(parameters)));
            GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();
            GameEventRunner runner = new GameEventRunner(
                new GameEventCatalog(Array.Empty<TextAsset>(), codec),
                new EffectRuntime(registry),
                parameters,
                codec);
            TextAsset file = new TextAsset(@"{
  ""SchemaVersion"": 2,
  ""DocumentGuid"": ""50000000-0000-0000-0000-000000000001"",
  ""DisplayName"": ""Scene Trigger"",
  ""TriggerTiming"": """",
  ""Condition"": ""$Stage == 0"",
  ""Commands"": ""SetParameter(Stage,1);""
}");
            GameObject host = new GameObject("SceneGameEventTrigger Host");
            GameObject enteringObject = new GameObject("Entering Object");

            try
            {
                SceneGameEventTrigger trigger = host.AddComponent<SceneGameEventTrigger>();
                const int enteringLayer = 8;
                enteringObject.layer = enteringLayer;
                Collider enteringCollider = enteringObject.AddComponent<BoxCollider>();

                trigger.Configure(file, 1 << enteringLayer);
                trigger.Initialize(runner, new EventContext(CancellationToken.None));

                InvokeOnTriggerEnter(trigger, enteringCollider);
                runner.WaitUntilIdleAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(enteringObject);
                UnityEngine.Object.DestroyImmediate(file);
            }

            Assert.That(parameters.GetInt("Stage"), Is.EqualTo(1));
        }

        [Test]
        public void OnTriggerEnter_IgnoresColliderOutsideConfiguredLayers()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Stage", "Stage", 0, 0, 2)
            });
            GameFlowExpressions expressions = new GameFlowExpressions(parameters);
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(new EffectCommandDefinition(
                name: "SetParameter",
                displayName: "Set Parameter",
                category: "Parameters",
                new[]
                {
                    new EffectCommandParameterDefinition("key", EffectCommandParameterKind.ParameterKey),
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.Literal)
                },
                new SetParameterCommand(parameters)));
            GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();
            GameEventRunner runner = new GameEventRunner(
                new GameEventCatalog(Array.Empty<TextAsset>(), codec),
                new EffectRuntime(registry),
                parameters,
                codec);
            TextAsset file = new TextAsset(@"{
  ""SchemaVersion"": 2,
  ""DocumentGuid"": ""50000000-0000-0000-0000-000000000002"",
  ""DisplayName"": ""Filtered Scene Trigger"",
  ""TriggerTiming"": """",
  ""Condition"": """",
  ""Commands"": ""SetParameter(Stage,1);""
}");
            GameObject host = new GameObject("SceneGameEventTrigger Host");
            GameObject enteringObject = new GameObject("Entering Object");

            try
            {
                SceneGameEventTrigger trigger = host.AddComponent<SceneGameEventTrigger>();
                const int allowedLayer = 8;
                const int enteringLayer = 9;
                enteringObject.layer = enteringLayer;
                Collider enteringCollider = enteringObject.AddComponent<BoxCollider>();

                trigger.Configure(file, 1 << allowedLayer);
                trigger.Initialize(runner, new EventContext(CancellationToken.None));

                InvokeOnTriggerEnter(trigger, enteringCollider);
                runner.WaitUntilIdleAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(enteringObject);
                UnityEngine.Object.DestroyImmediate(file);
            }

            Assert.That(parameters.GetInt("Stage"), Is.Zero);
        }

        [Test]
        public void OnTriggerEnter_WithTriggerInput_ShowsPromptAndWaitsForConfirmation()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Stage", "Stage", 0, 0, 2)
            });
            EffectCommandRegistry registry = new EffectCommandRegistry();
            registry.Register(new EffectCommandDefinition(
                name: "SetParameter",
                displayName: "Set Parameter",
                category: "Parameters",
                new[]
                {
                    new EffectCommandParameterDefinition("key", EffectCommandParameterKind.ParameterKey),
                    new EffectCommandParameterDefinition("value", EffectCommandParameterKind.Literal)
                },
                new SetParameterCommand(parameters)));
            GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();
            GameEventRunner runner = new GameEventRunner(
                new GameEventCatalog(Array.Empty<TextAsset>(), codec),
                new EffectRuntime(registry),
                parameters,
                codec);
            TextAsset file = new TextAsset(@"{
  ""SchemaVersion"": 2,
  ""DocumentGuid"": ""50000000-0000-0000-0000-000000000005"",
  ""DisplayName"": ""Scene Trigger"",
  ""TriggerTiming"": """",
  ""Condition"": ""$Stage == 0"",
  ""Commands"": ""SetParameter(Stage,1);""
}");
            GameObject host = new GameObject("SceneGameEventTrigger Host");
            GameObject enteringObject = new GameObject("Entering Object");
            GameObject prompt = new GameObject("Input Prompt");
            InputActionAsset actions = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionReference confirmInput = InputActionReference.Create(
                actions.AddActionMap("Test").AddAction("Confirm", InputActionType.Button));

            try
            {
                SceneGameEventTrigger trigger = host.AddComponent<SceneGameEventTrigger>();
                const int enteringLayer = 8;
                enteringObject.layer = enteringLayer;
                Collider enteringCollider = enteringObject.AddComponent<BoxCollider>();

                trigger.Configure(file, 1 << enteringLayer);
                trigger.Initialize(runner, new EventContext(CancellationToken.None));
                SetPrivateField(trigger, "triggerInput", confirmInput);
                SetPrivateField(trigger, "inputPrompt", prompt);

                InvokeOnTriggerEnter(trigger, enteringCollider);
                runner.WaitUntilIdleAsync(CancellationToken.None).GetAwaiter().GetResult();
                Assert.That(parameters.GetInt("Stage"), Is.Zero);
                Assert.That(prompt.activeSelf, Is.True);

                InvokePrivate(trigger, "ConfirmKeyPress");
                runner.WaitUntilIdleAsync(CancellationToken.None).GetAwaiter().GetResult();
                Assert.That(parameters.GetInt("Stage"), Is.EqualTo(1));
                Assert.That(prompt.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(enteringObject);
                UnityEngine.Object.DestroyImmediate(prompt);
                UnityEngine.Object.DestroyImmediate(confirmInput);
                UnityEngine.Object.DestroyImmediate(actions);
                UnityEngine.Object.DestroyImmediate(file);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, Array.Empty<object>());
        }

        private static void InvokeOnTriggerEnter(
            SceneGameEventTrigger trigger,
            Collider other)
        {
            MethodInfo method = typeof(SceneGameEventTrigger).GetMethod(
                "OnTriggerEnter",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(trigger, new object[] { other });
        }
    }
}
