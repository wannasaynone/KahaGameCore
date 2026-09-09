using System;
using System.Collections.Generic;
using KahaGameCore.Parameters;
using KahaGameCore.Persistence;
using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public sealed class GameSaveDocumentJsonCodecTests
    {
        [Test]
        public void WriteRead_RoundTripsSaveableObjects()
        {
            ParameterSnapshot parameters = new ParameterSnapshot(
                ParameterSnapshot.CurrentSchemaVersion,
                new Dictionary<string, ParameterValue>());
            SaveableObjectRecord[] objects =
            {
                new SaveableObjectRecord
                {
                    Id = "a1",
                    ResourcePath = "Prefabs/Corpse",
                    ScenePath = "Assets/Room3.unity",
                    X = 12.5f,
                    Y = -4f,
                    Z = 1f
                }
            };
            GameSaveDocumentJsonCodec codec = new GameSaveDocumentJsonCodec();

            GameSaveSnapshot snapshot = codec.Read(
                codec.Write("Factory", parameters, objects));

            Assert.That(snapshot.Objects, Has.Length.EqualTo(1));
            Assert.That(snapshot.Objects[0].Id, Is.EqualTo("a1"));
            Assert.That(snapshot.Objects[0].ResourcePath, Is.EqualTo("Prefabs/Corpse"));
            Assert.That(snapshot.Objects[0].ScenePath, Is.EqualTo("Assets/Room3.unity"));
            Assert.That(snapshot.Objects[0].X, Is.EqualTo(12.5f));
            Assert.That(snapshot.Objects[0].Y, Is.EqualTo(-4f));
            Assert.That(snapshot.Objects[0].Z, Is.EqualTo(1f));
        }

        [Test]
        public void Read_TreatsMissingObjectsAsEmpty()
        {
            const string json =
                "{\"SchemaVersion\":1," +
                "\"SceneKey\":\"Factory\"," +
                "\"Parameters\":{\"SchemaVersion\":1,\"Values\":[]}}";

            GameSaveSnapshot snapshot =
                new GameSaveDocumentJsonCodec().Read(json);

            Assert.That(snapshot.Objects, Is.Empty);
        }

        [Test]
        public void WriteRead_RoundTripsSceneAndParameters()
        {
            ParameterSnapshot parameters = new ParameterSnapshot(
                ParameterSnapshot.CurrentSchemaVersion,
                new Dictionary<string, ParameterValue>
                {
                    ["Score"] = ParameterValue.FromInt(7),
                    ["CurrentPhase"] = ParameterValue.FromString("Night")
                });
            GameSaveDocumentJsonCodec codec =
                new GameSaveDocumentJsonCodec();

            string json = codec.Write("Factory", parameters);

            Assert.That(json, Does.Contain("\"SchemaVersion\":1"));
            Assert.That(json, Does.Contain("\"SceneKey\":\"Factory\""));

            GameSaveSnapshot snapshot = codec.Read(json);

            Assert.That(snapshot.SceneKey, Is.EqualTo("Factory"));
            Assert.That(
                snapshot.Parameters.TryGetValue("Score", out ParameterValue score),
                Is.True);
            Assert.That(score, Is.EqualTo(ParameterValue.FromInt(7)));
            Assert.That(
                snapshot.Parameters.TryGetValue(
                    "CurrentPhase",
                    out ParameterValue phase),
                Is.True);
            Assert.That(phase.AsString(), Is.EqualTo("Night"));
        }

        [Test]
        public void Read_RejectsUnsupportedSchemaVersion()
        {
            const string json =
                "{\"SchemaVersion\":2," +
                "\"SceneKey\":\"Factory\"," +
                "\"Parameters\":{\"SchemaVersion\":1,\"Values\":[]}}";

            Assert.Throws<InvalidOperationException>(
                () => new GameSaveDocumentJsonCodec().Read(json));
        }

        [TestCase("null")]
        [TestCase("\"\"")]
        [TestCase("\"   \"")]
        public void Read_RejectsMissingSceneKey(string sceneKeyJson)
        {
            string json =
                "{\"SchemaVersion\":1," +
                $"\"SceneKey\":{sceneKeyJson}," +
                "\"Parameters\":{\"SchemaVersion\":1,\"Values\":[]}}";

            Assert.Throws<InvalidOperationException>(
                () => new GameSaveDocumentJsonCodec().Read(json));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Write_RejectsMissingSceneKey(string sceneKey)
        {
            ParameterSnapshot parameters = new ParameterSnapshot(
                ParameterSnapshot.CurrentSchemaVersion,
                new Dictionary<string, ParameterValue>());

            Assert.Throws<ArgumentException>(
                () => new GameSaveDocumentJsonCodec().Write(sceneKey, parameters));
        }

        [Test]
        public void Read_RejectsMissingParameters()
        {
            const string json =
                "{\"SchemaVersion\":1,\"SceneKey\":\"Factory\"}";

            Assert.Throws<InvalidOperationException>(
                () => new GameSaveDocumentJsonCodec().Read(json));
        }
    }
}
