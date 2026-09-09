using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using KahaGameCore.Parameters;
using KahaGameCore.Persistence;
using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public sealed class GameSaveSlotStoreTests
    {
        private string rootDirectory;

        [SetUp]
        public void SetUp()
        {
            rootDirectory = Path.Combine(
                Path.GetTempPath(),
                "KahaGameCore.GameSaveSlotStoreTests",
                Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, true);
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_RejectsMissingRootDirectory(
            string invalidRootDirectory)
        {
            Assert.Throws<ArgumentException>(
                () => new GameSaveSlotStore(invalidRootDirectory));
        }

        [Test]
        public void SaveLoad_RoundTripsAcrossStoreInstances()
        {
            const string json =
                "{\"SchemaVersion\":1,\"SceneKey\":\"Factory\"}";
            new GameSaveSlotStore(rootDirectory).Save(2, json);

            string loaded =
                new GameSaveSlotStore(rootDirectory).Load(2);

            Assert.That(loaded, Is.EqualTo(json));
        }

        [Test]
        public void Exists_ReportsWhetherSlotWasSaved()
        {
            GameSaveSlotStore store =
                new GameSaveSlotStore(rootDirectory);

            Assert.That(store.Exists(4), Is.False);

            store.Save(4, "{}");

            Assert.That(store.Exists(4), Is.True);
        }

        [Test]
        public void Delete_RemovesExistingSlotAndReportsResult()
        {
            GameSaveSlotStore store =
                new GameSaveSlotStore(rootDirectory);
            store.Save(1, "{}");

            Assert.That(store.Delete(1), Is.True);
            Assert.That(store.Exists(1), Is.False);
            Assert.That(store.Delete(1), Is.False);
        }

        [Test]
        public void SlotOperations_RejectNegativeSlot()
        {
            GameSaveSlotStore store =
                new GameSaveSlotStore(rootDirectory);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => store.Save(-1, "{}"));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => store.Load(-1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => store.Exists(-1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => store.Delete(-1));
        }

        [Test]
        public void SaveDocument_RoundTripsTextAndDecimalsAcrossCultures()
        {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture =
                    CultureInfo.GetCultureInfo("fr-FR");
                CultureInfo.CurrentUICulture =
                    CultureInfo.GetCultureInfo("fr-FR");

                ParameterSnapshot parameters = new ParameterSnapshot(
                    ParameterSnapshot.CurrentSchemaVersion,
                    new Dictionary<string, ParameterValue>
                    {
                        ["Temperature"] = ParameterValue.FromFloat(2.5f),
                        ["Ratio"] = ParameterValue.FromFloat(1.25f),
                        ["Greeting"] = ParameterValue.FromString("歡迎")
                    });
                GameSaveDocumentJsonCodec codec =
                    new GameSaveDocumentJsonCodec();
                GameSaveSlotStore store =
                    new GameSaveSlotStore(rootDirectory);

                string json = codec.Write("工廠", parameters);
                store.Save(0, json);

                Assert.That(json, Does.Match("1\.25(?:[,\"}])"));
                Assert.That(json, Does.Not.Contain("1,25"));

                CultureInfo.CurrentCulture =
                    CultureInfo.GetCultureInfo("en-US");
                CultureInfo.CurrentUICulture =
                    CultureInfo.GetCultureInfo("en-US");

                GameSaveSnapshot snapshot = codec.Read(store.Load(0));

                Assert.That(snapshot.SceneKey, Is.EqualTo("工廠"));
                Assert.That(
                    snapshot.Parameters.TryGetValue(
                        "Temperature",
                        out ParameterValue temperature),
                    Is.True);
                Assert.That(temperature.AsFloat(), Is.EqualTo(2.5f));
                Assert.That(
                    snapshot.Parameters.TryGetValue(
                        "Ratio",
                        out ParameterValue ratio),
                    Is.True);
                Assert.That(ratio.AsFloat(), Is.EqualTo(1.25f));
                Assert.That(
                    snapshot.Parameters.TryGetValue(
                        "Greeting",
                        out ParameterValue greeting),
                    Is.True);
                Assert.That(greeting.AsString(), Is.EqualTo("歡迎"));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }
    }
}
