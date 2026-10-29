using System;
using System.Reflection;
using KahaGameCore.Effects;
using KahaGameCore.Parameters;
using KahaGameCore.Parameters.EffectsIntegration;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.GameEvents.Tests
{
    /// <summary>Shared setup for the 2D and 3D scene trigger tests.</summary>
    internal static class SceneGameEventTriggerTestSupport
    {
        public const int ActivatorLayer = 8;

        public static ParameterStore CreateParameters()
        {
            return new ParameterStore(new[]
            {
                ParameterDefinition.Int(key: "Stage", displayName: "Stage", initialValue: 0, minValue: 0, maxValue: 2)
            });
        }

        public static GameEventRunner CreateRunner(ParameterStore parameters)
        {
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

        public static TextAsset CreateEvent(string documentGuid)
        {
            return new TextAsset($@"{{
  ""SchemaVersion"": 2,
  ""DocumentGuid"": ""{documentGuid}"",
  ""DisplayName"": ""Scene Trigger"",
  ""TriggerTiming"": """",
  ""Condition"": ""$Stage == 0"",
  ""Commands"": ""SetParameter(Stage,1);""
}}");
        }

        /// <summary>Runs an edit-mode lifecycle or Unity message method by name.</summary>
        public static void Invoke(object target, string method, params object[] arguments)
        {
            MethodInfo info = target.GetType().GetMethod(
                method,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(info, Is.Not.Null, method);
            info.Invoke(target, arguments);
        }
    }
}
