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
