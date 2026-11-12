using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using KahaGameCore.Parameters.EffectsIntegration;
using KahaGameCore.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class DefaultSimpleGameLauncherStartupTests
    {
        // Each test builds its own catalog, so the process-wide session must
        // not leak between them.
        [SetUp]
        public void ResetSession()
        {
            GameEventSession.Reset();
        }

        [TestCase(true, true)]
        [TestCase(false, false)]
        public async Task StartTrigger_RunsOnlyWhenBinderLeavesObjectActive(
            bool actorIsAvailable,
            bool expectedEventExecution)
        {
            GameObject host = new GameObject("Simple Launcher");
            GameObject actor = new GameObject("Actor");
            actor.transform.SetParent(host.transform);
            GameEventCatalogAsset catalog =
                ScriptableObject.CreateInstance<GameEventCatalogAsset>();
            TextAsset parameters = CreateParameters(actorIsAvailable);
            TextAsset startEvent = CreateStartEvent();

            try
            {
                ConfigureCatalog(catalog, parameters);
                StartGameEventTrigger trigger =
                    actor.AddComponent<StartGameEventTrigger>();
                trigger.Configure(startEvent);
                ParameterStateBinder binder =
                    host.AddComponent<ParameterStateBinder>();
                binder.Configure(new[]
                {
                    new ParameterChildConditionBinding(actor, "$ActorAvailable")
                });
                StartupTestSimpleGameLauncher launcher =
                    host.AddComponent<StartupTestSimpleGameLauncher>();
                SerializedObject serializedLauncher =
                    new SerializedObject(launcher);
                serializedLauncher.FindProperty("catalog")
                    .objectReferenceValue = catalog;
                serializedLauncher.ApplyModifiedPropertiesWithoutUndo();

                launcher.InitializeForTest();
                await launcher.TriggerStartEventsForTestAsync();

                Assert.That(actor.activeInHierarchy, Is.EqualTo(actorIsAvailable));
                Assert.That(
                    launcher.Parameters.GetBool("StartEventExecuted"),
                    Is.EqualTo(expectedEventExecution));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(parameters);
                Object.DestroyImmediate(startEvent);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public async Task StartTrigger_OutsideLauncherHierarchyAndInactive_IsInitialized()
        {
            GameObject host = new GameObject("Simple Launcher");
            GameObject actor = new GameObject("Unrelated Root Actor");
            GameEventCatalogAsset catalog =
                ScriptableObject.CreateInstance<GameEventCatalogAsset>();
            TextAsset parameters = CreateParameters(actorIsAvailable: true);
            TextAsset startEvent = CreateStartEvent();

            try
            {
                ConfigureCatalog(catalog, parameters);
                StartGameEventTrigger trigger =
                    actor.AddComponent<StartGameEventTrigger>();
                trigger.Configure(startEvent);
                actor.SetActive(false);
                StartupTestSimpleGameLauncher launcher =
                    host.AddComponent<StartupTestSimpleGameLauncher>();
                SerializedObject serializedLauncher =
                    new SerializedObject(launcher);
                serializedLauncher.FindProperty("catalog")
                    .objectReferenceValue = catalog;
                serializedLauncher.ApplyModifiedPropertiesWithoutUndo();

                launcher.InitializeForTest();
                actor.SetActive(true);
                await launcher.TriggerStartEventsForTestAsync();

                Assert.That(
                    launcher.Parameters.GetBool("StartEventExecuted"),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(actor);
                Object.DestroyImmediate(parameters);
                Object.DestroyImmediate(startEvent);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public async Task StartTriggers_RunInHierarchyOrder()
        {
            GameObject host = new GameObject("Simple Launcher");
            // Created in the opposite order, then reordered, so creation order
            // cannot explain a pass.
            GameObject second = new GameObject("Second");
            GameObject first = new GameObject("First");
            second.transform.SetAsLastSibling();
            GameEventCatalogAsset catalog =
                ScriptableObject.CreateInstance<GameEventCatalogAsset>();
            TextAsset parameters = CreateParameters(actorIsAvailable: false);
            TextAsset firstEvent = CreateStartEvent(
                documentGuid: "5d0f1c1e-6f0b-4a53-9a39-0c6f6a0f4f01",
                commands: "SetParameter(ActorAvailable,true);SetParameter(StartEventExecuted,true);");
            TextAsset secondEvent = CreateStartEvent(
                documentGuid: "5d0f1c1e-6f0b-4a53-9a39-0c6f6a0f4f02",
                commands: "SetParameter(StartEventExecuted,false);");

            try
            {
                ConfigureCatalog(catalog, parameters);
                first.AddComponent<StartGameEventTrigger>().Configure(firstEvent);
                second.AddComponent<StartGameEventTrigger>().Configure(secondEvent);
                StartupTestSimpleGameLauncher launcher =
                    host.AddComponent<StartupTestSimpleGameLauncher>();
                SerializedObject serializedLauncher =
                    new SerializedObject(launcher);
                serializedLauncher.FindProperty("catalog")
                    .objectReferenceValue = catalog;
                serializedLauncher.ApplyModifiedPropertiesWithoutUndo();

                launcher.InitializeForTest();
                await launcher.TriggerStartEventsForTestAsync();

                // Both ran, and the lower one in the Hierarchy ran last.
                Assert.That(launcher.Parameters.GetBool("ActorAvailable"), Is.True);
                Assert.That(
                    launcher.Parameters.GetBool("StartEventExecuted"),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(parameters);
                Object.DestroyImmediate(firstEvent);
                Object.DestroyImmediate(secondEvent);
                Object.DestroyImmediate(catalog);
            }
        }

        private static TextAsset CreateParameters(bool actorIsAvailable)
        {
            string available = actorIsAvailable ? "true" : "false";
            return new TextAsset($@"{{
  ""SchemaVersion"": 1,
  ""TableGuid"": ""e4a1c4f4-2b5a-4f88-8b86-2e3b12eb725f"",
  ""DisplayName"": ""Startup Parameters"",
  ""Parameters"": [
    {{
      ""Key"": ""ActorAvailable"",
      ""DisplayName"": ""Actor Available"",
      ""Type"": ""Bool"",
      ""InitialValue"": ""{available}""
    }},
    {{
      ""Key"": ""StartEventExecuted"",
      ""DisplayName"": ""Start Event Executed"",
      ""Type"": ""Bool"",
      ""InitialValue"": ""false""
    }}
  ]
}}");
        }

        private static TextAsset CreateStartEvent()
        {
            return CreateStartEvent(
                documentGuid: "99a34bd8-55c1-4a91-bcaa-24a41328270e",
                commands: "SetParameter(StartEventExecuted,true);");
        }

        private static TextAsset CreateStartEvent(string documentGuid, string commands)
        {
            return new TextAsset($@"{{
  ""SchemaVersion"": 2,
  ""DocumentGuid"": ""{documentGuid}"",
  ""DisplayName"": ""Actor Start Event"",
  ""TriggerTiming"": """",
  ""Condition"": """",
  ""Commands"": ""{commands}""
}}");
        }

        private static void ConfigureCatalog(
            GameEventCatalogAsset catalog,
            TextAsset parameters)
        {
            catalog.SetParameterTables(new[] { parameters });
            System.Type factory = typeof(ParameterEffectCommandModuleFactory);
            catalog.SetCommandModules(new[]
            {
                new KahaGameCore.Effects.EffectCommandModuleReference(
                    factory.Assembly.GetName().Name,
                    $"{factory.FullName}, {factory.Assembly.GetName().Name}")
            });
            catalog.SetEnabledCommandNames(new[] { "SetParameter" });
        }
    }

    public sealed class StartupTestSimpleGameLauncher :
        DefaultSimpleGameLauncher
    {
        public void InitializeForTest()
        {
            base.Awake();
        }

        public UniTask TriggerStartEventsForTestAsync()
        {
            return TriggerActiveStartEventsAsync();
        }
    }
}
