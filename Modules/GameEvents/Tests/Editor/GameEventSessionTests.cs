using System;
using KahaGameCore.Effects;
using KahaGameCore.Parameters.EffectsIntegration;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class GameEventSessionTests
    {
        [SetUp]
        public void ResetSession()
        {
            GameEventSession.Reset();
        }

        [Test]
        public void GetOrCreate_ReusesTheRuntimeSoParametersOutliveASceneLoad()
        {
            GameEventCatalogAsset catalog = CreateCatalog(out TextAsset table);

            try
            {
                GameEventRuntime first = GameEventSession.GetOrCreate(catalog);
                first.Parameters.Set("Score", 7);

                GameEventRuntime second = GameEventSession.GetOrCreate(catalog);

                Assert.That(second, Is.SameAs(first));
                Assert.That(second.Parameters.GetInt("Score"), Is.EqualTo(7));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void Reset_StartsANewGameFromInitialValues()
        {
            GameEventCatalogAsset catalog = CreateCatalog(out TextAsset table);

            try
            {
                GameEventSession.GetOrCreate(catalog).Parameters.Set("Score", 7);

                GameEventSession.Reset();

                Assert.That(
                    GameEventSession.GetOrCreate(catalog).Parameters.GetInt("Score"),
                    Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void GetOrCreate_RejectsASecondCatalogInsteadOfSilentlyIgnoringIt()
        {
            GameEventCatalogAsset catalog = CreateCatalog(out TextAsset table);
            GameEventCatalogAsset other = CreateCatalog(out TextAsset otherTable);

            try
            {
                GameEventSession.GetOrCreate(catalog);

                Assert.Throws<InvalidOperationException>(
                    () => GameEventSession.GetOrCreate(other));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(table);
                UnityEngine.Object.DestroyImmediate(other);
                UnityEngine.Object.DestroyImmediate(otherTable);
            }
        }

        private static GameEventCatalogAsset CreateCatalog(out TextAsset parameterTable)
        {
            parameterTable = new TextAsset(@"{
  ""SchemaVersion"": 1,
  ""TableGuid"": ""f1c0a5d2-0d1c-4d2b-9f4a-6b0f5c2a1e33"",
  ""DisplayName"": ""Session Parameters"",
  ""Parameters"": [
    {
      ""Key"": ""Score"",
      ""DisplayName"": ""Score"",
      ""Type"": ""Int"",
      ""InitialValue"": ""0"",
      ""MinValue"": ""0"",
      ""MaxValue"": ""99""
    }
  ]
}");
            GameEventCatalogAsset catalog =
                ScriptableObject.CreateInstance<GameEventCatalogAsset>();
            catalog.SetParameterTables(new[] { parameterTable });
            Type factory = typeof(ParameterEffectCommandModuleFactory);
            catalog.SetCommandModules(new[]
            {
                new EffectCommandModuleReference(
                    factory.Assembly.GetName().Name,
                    $"{factory.FullName}, {factory.Assembly.GetName().Name}")
            });
            catalog.SetEnabledCommandNames(new[] { "SetParameter" });
            return catalog;
        }
    }
}
