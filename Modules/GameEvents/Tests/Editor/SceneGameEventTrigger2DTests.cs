using System;
using System.Reflection;
using System.Threading;
using KahaGameCore.Effects;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.Parameters;
using KahaGameCore.Parameters.EffectsIntegration;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class SceneGameEventTrigger2DTests
    {
        [Test]
        public void OnTriggerEnter2D_RunsDirectlyReferencedGameEventFile()
        {
            ParameterStore parameters = CreateParameters();
            GameEventRunner runner = CreateRunner(parameters);
            TextAsset file = CreateEvent(
                "50000000-0000-0000-0000-000000000003",
                "$Stage == 0");
            GameObject host = new GameObject("SceneGameEventTrigger2D Host");
            GameObject enteringObject = new GameObject("Entering Object");

            try
            {
                SceneGameEventTrigger2D trigger = host.AddComponent<SceneGameEventTrigger2D>();
                const int enteringLayer = 8;
                enteringObject.layer = enteringLayer;
                Collider2D enteringCollider = enteringObject.AddComponent<BoxCollider2D>();

                trigger.Configure(file, 1 << enteringLayer);
                trigger.Initialize(runner, new EventContext(CancellationToken.None));

                InvokeOnTriggerEnter2D(trigger, enteringCollider);
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
        public void OnTriggerEnter2D_IgnoresColliderOutsideConfiguredLayers()
        {
            ParameterStore parameters = CreateParameters();
            GameEventRunner runner = CreateRunner(parameters);
            TextAsset file = CreateEvent(
                "50000000-0000-0000-0000-000000000004",
                string.Empty);
            GameObject host = new GameObject("SceneGameEventTrigger2D Host");
            GameObject enteringObject = new GameObject("Entering Object");

            try
            {
                SceneGameEventTrigger2D trigger = host.AddComponent<SceneGameEventTrigger2D>();
                const int allowedLayer = 8;
                const int enteringLayer = 9;
                enteringObject.layer = enteringLayer;
                Collider2D enteringCollider = enteringObject.AddComponent<BoxCollider2D>();

                trigger.Configure(file, 1 << allowedLayer);
                trigger.Initialize(runner, new EventContext(CancellationToken.None));

                InvokeOnTriggerEnter2D(trigger, enteringCollider);
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

        private static ParameterStore CreateParameters()
        {
            return new ParameterStore(new[]
            {
                ParameterDefinition.Int("Stage", "Stage", 0, 0, 2)
            });
        }

        private static GameEventRunner CreateRunner(ParameterStore parameters)
        {
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
            return new GameEventRunner(
                new GameEventCatalog(Array.Empty<TextAsset>(), codec),
                new EffectRuntime(registry),
                parameters,
                codec);
        }

        private static TextAsset CreateEvent(string documentGuid, string condition)
        {
            return new TextAsset($@"{{
  ""SchemaVersion"": 2,
  ""DocumentGuid"": ""{documentGuid}"",
  ""DisplayName"": ""Scene Trigger 2D"",
  ""TriggerTiming"": """",
  ""Condition"": ""{condition}"",
  ""Commands"": ""SetParameter(Stage,1);""
}}");
        }

        private static void InvokeOnTriggerEnter2D(
            SceneGameEventTrigger2D trigger,
            Collider2D other)
        {
            MethodInfo method = typeof(SceneGameEventTrigger2D).GetMethod(
                "OnTriggerEnter2D",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(trigger, new object[] { other });
        }
    }
}
