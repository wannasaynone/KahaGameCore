using System;
using System.Collections.Generic;
using KahaGameCore.GameEvents;
using KahaGameCore.GameFlowSystem.DefaultViews;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem.Tests
{
    public sealed class GameFlowDataCatalogAssetTests
    {
        private GameFlowDataCatalogAsset catalog;
        private GameEventCatalogAsset eventCatalog;
        private readonly List<TextAsset> textAssets = new List<TextAsset>();

        [SetUp]
        public void SetUp()
        {
            catalog = ScriptableObject.CreateInstance<GameFlowDataCatalogAsset>();
            eventCatalog = ScriptableObject.CreateInstance<GameEventCatalogAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(eventCatalog);
            for (int index = 0; index < textAssets.Count; index++)
            {
                UnityEngine.Object.DestroyImmediate(textAssets[index]);
            }
            textAssets.Clear();
        }

        [Test]
        public void ValidateRequiredReferences_AcceptsCompleteCatalog()
        {
            ConfigureCompleteCatalog();

            Assert.DoesNotThrow(catalog.ValidateRequiredReferences);
        }

        [Test]
        public void ValidateRequiredReferences_RejectsWrongTableName()
        {
            ConfigureCompleteCatalog();
            TextAsset wrong = NamedTextAsset("WrongName");
            catalog.SetGameDataTables(
                wrong,
                NamedTextAsset("PlayerActionData"),
                NamedTextAsset("LocationData"),
                NamedTextAsset("GameTextData"),
                NamedTextAsset("DialogueData"));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                catalog.ValidateRequiredReferences);

            StringAssert.Contains("TimePhaseData", exception.Message);
        }

        [Test]
        public void GameEventCommands_RoundTripSelectedNames()
        {
            catalog.SetGameEventCommands(new[] { "SetParameter", "Wait" });

            Assert.That(
                catalog.GameEventCommands,
                Is.EqualTo(new[] { "SetParameter", "Wait" }));
        }

        private void ConfigureCompleteCatalog()
        {
            catalog.SetGameDataTables(
                NamedTextAsset("TimePhaseData"),
                NamedTextAsset("PlayerActionData"),
                NamedTextAsset("LocationData"),
                NamedTextAsset("GameTextData"),
                NamedTextAsset("DialogueData"));
            catalog.SetParameterTables(new[] { NamedTextAsset("Runtime.parameters") });
            catalog.SetGameEventCatalog(eventCatalog);
        }

        private TextAsset NamedTextAsset(string name)
        {
            TextAsset asset = new TextAsset("[]") { name = name };
            textAssets.Add(asset);
            return asset;
        }
    }
}
