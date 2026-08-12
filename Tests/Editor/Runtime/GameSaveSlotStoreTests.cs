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
                        ["Greeting"] = ParameterValue.FromString("歡迎")
                    });
                SaveParticipantRegistry source =
                    new SaveParticipantRegistry();
                source.Register(
                    new LocalizedParticipant("機關", 1.25f));
                GameSaveDocumentJsonCodec codec =
                    new GameSaveDocumentJsonCodec();
                GameSaveSlotStore store =
                    new GameSaveSlotStore(rootDirectory);

                string json = codec.Write(
                    "工廠",
                    parameters,
                    source.Capture());
                store.Save(0, json);

                Assert.That(
                    json,
                    Does.Match("\"Ratio\"\\s*:\\s*1\\.25(?:[,}])"));
                Assert.That(json, Does.Not.Contain("1,25"));

                CultureInfo.CurrentCulture =
                    CultureInfo.GetCultureInfo("en-US");
                CultureInfo.CurrentUICulture =
                    CultureInfo.GetCultureInfo("en-US");

                LocalizedParticipant participant =
                    new LocalizedParticipant("Reset", 0f);
                SaveParticipantRegistry target =
                    new SaveParticipantRegistry();
                target.Register(participant);
                GameSaveSnapshot snapshot = codec.Read(
                    store.Load(0),
                    target);
                target.Restore(snapshot.Participants);

                Assert.That(snapshot.SceneKey, Is.EqualTo("工廠"));
                Assert.That(
                    snapshot.Parameters.TryGetValue(
                        "Temperature",
                        out ParameterValue temperature),
                    Is.True);
                Assert.That(temperature.AsFloat(), Is.EqualTo(2.5f));
                Assert.That(
                    snapshot.Parameters.TryGetValue(
                        "Greeting",
                        out ParameterValue greeting),
                    Is.True);
                Assert.That(greeting.AsString(), Is.EqualTo("歡迎"));
                Assert.That(participant.DisplayName, Is.EqualTo("機關"));
                Assert.That(participant.Ratio, Is.EqualTo(1.25f));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        public sealed class LocalizedSnapshot
        {
            public string DisplayName;
            public float Ratio;
        }

        private sealed class LocalizedParticipant :
            ISaveParticipant<LocalizedSnapshot>
        {
            public LocalizedParticipant(string displayName, float ratio)
            {
                DisplayName = displayName;
                Ratio = ratio;
            }

            public string SaveKey => "Tests.Localized";
            public string DisplayName { get; private set; }
            public float Ratio { get; private set; }

            public LocalizedSnapshot Capture()
            {
                return new LocalizedSnapshot
                {
                    DisplayName = DisplayName,
                    Ratio = Ratio
                };
            }

            public void Restore(LocalizedSnapshot snapshot)
            {
                DisplayName = snapshot.DisplayName;
                Ratio = snapshot.Ratio;
            }
        }
    }
}
