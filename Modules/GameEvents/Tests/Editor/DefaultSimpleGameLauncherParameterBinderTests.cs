using System.Threading;
using System.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.Parameters.EffectsIntegration;
using KahaGameCore.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class DefaultSimpleGameLauncherParameterBinderTests
    {
        [Test]
        public void Awake_InitializesChildParameterStateBinders()
        {
            GameObject host = new GameObject("Simple Launcher");
            GameEventCatalogAsset catalog =
                ScriptableObject.CreateInstance<GameEventCatalogAsset>();
            TextAsset parameters = new TextAsset(@"{
  ""SchemaVersion"": 1,
  ""TableGuid"": ""d4d62b30-3987-4e1b-8ca1-087d515f4d01"",
  ""DisplayName"": ""Test Parameters"",
  ""Parameters"": [
    {
      ""Key"": ""EnemiesEnabled"",
      ""DisplayName"": ""Enemies Enabled"",
      ""Type"": ""Bool"",
      ""InitialValue"": ""false""
    }
  ]
}");

            try
            {
                catalog.SetParameterTables(new[] { parameters });
                System.Type parameterFactory =
                    typeof(ParameterEffectCommandModuleFactory);
                catalog.SetCommandModules(new[]
                {
                    new EffectCommandModuleReference(
                        parameterFactory.Assembly.GetName().Name,
                        $"{parameterFactory.FullName}, {parameterFactory.Assembly.GetName().Name}")
                });
                catalog.SetEnabledCommandNames(new[] { "SetParameter" });
                TestSimpleGameLauncher launcher =
                    host.AddComponent<TestSimpleGameLauncher>();
                SerializedObject serializedLauncher =
                    new SerializedObject(launcher);
                serializedLauncher.FindProperty("catalog")
                    .objectReferenceValue = catalog;
                serializedLauncher.ApplyModifiedPropertiesWithoutUndo();

                GameObject child = new GameObject("Action");
                child.transform.SetParent(host.transform);
                SimpleLauncherTestBehaviour target =
                    child.AddComponent<SimpleLauncherTestBehaviour>();
                ParameterStateBinder binder =
                    host.AddComponent<ParameterStateBinder>();
                binder.ConfigureBehaviourBinding(
                    new Behaviour[] { target },
                    "$EnemiesEnabled");

                launcher.InitializeForTest();

                Assert.That(target.enabled, Is.False);
                launcher.Parameters.Set("EnemiesEnabled", true);
                Assert.That(target.enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(parameters);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public async Task Awake_RegistersStandardWaitCommand()
        {
            GameObject host = new GameObject("Simple Launcher");
            GameEventCatalogAsset catalog =
                ScriptableObject.CreateInstance<GameEventCatalogAsset>();
            TextAsset parameters = new TextAsset(@"{
  ""SchemaVersion"": 1,
  ""TableGuid"": ""8843c536-3ca0-4f05-80a0-73de24bb0404"",
  ""DisplayName"": ""Empty Parameters"",
  ""Parameters"": []
}");

            try
            {
                catalog.SetParameterTables(new[] { parameters });
                catalog.SetCommandModules(new[]
                {
                    new EffectCommandModuleReference(
                        "KahaGameCore.Modules.Effects.StandardCommands",
                        "KahaGameCore.Effects.StandardCommands.StandardEffectCommandModuleFactory, KahaGameCore.Modules.Effects.StandardCommands")
                });
                catalog.SetEnabledCommandNames(new[] { "Wait" });
                TestSimpleGameLauncher launcher =
                    host.AddComponent<TestSimpleGameLauncher>();
                SerializedObject serializedLauncher =
                    new SerializedObject(launcher);
                serializedLauncher.FindProperty("catalog")
                    .objectReferenceValue = catalog;
                serializedLauncher.ApplyModifiedPropertiesWithoutUndo();

                launcher.InitializeForTest();

                EffectExecutionResult result = await launcher.Effects.ExecuteAsync(
                    "Wait(0);",
                    new EffectExecutionContext(),
                    CancellationToken.None);

                Assert.That(result.IsSuccess, Is.True, result.FormatDiagnostic());
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(parameters);
                Object.DestroyImmediate(catalog);
            }
        }
    }

    public sealed class TestSimpleGameLauncher : DefaultSimpleGameLauncher
    {
        public void InitializeForTest()
        {
            base.Awake();
        }
    }

    public sealed class SimpleLauncherTestBehaviour : MonoBehaviour
    {
    }
}
