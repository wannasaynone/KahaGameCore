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
