using System;
using System.Threading;
using KahaGameCore.Effects;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.GameFlowSystem.DefaultImplements.Commands;
using KahaGameCore.Parameters;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class SceneGameEventTriggerTests
    {
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
