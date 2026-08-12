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
        public void WriteRead_RoundTripsSceneParametersAndParticipants()
        {
            ParameterSnapshot parameters = new ParameterSnapshot(
                ParameterSnapshot.CurrentSchemaVersion,
                new Dictionary<string, ParameterValue>
                {
                    ["Score"] = ParameterValue.FromInt(7)
                });
            SaveParticipantRegistry source = new SaveParticipantRegistry();
            source.Register(new PhaseParticipant("Night"));
            GameSaveDocumentJsonCodec codec =
                new GameSaveDocumentJsonCodec();

            string json = codec.Write(
                "Factory",
                parameters,
                source.Capture());

            Assert.That(json, Does.Contain("\"SchemaVersion\":1"));
            Assert.That(json, Does.Contain("\"SceneKey\":\"Factory\""));
            Assert.That(json, Does.Not.Contain(nameof(PhaseSnapshot)));
            PhaseParticipant phase = new PhaseParticipant("Morning");
            SaveParticipantRegistry target = new SaveParticipantRegistry();
            target.Register(phase);

            GameSaveSnapshot snapshot = codec.Read(json, target);
            target.Restore(snapshot.Participants);

            Assert.That(snapshot.SceneKey, Is.EqualTo("Factory"));
            Assert.That(
                snapshot.Parameters.TryGetValue("Score", out ParameterValue score),
                Is.True);
            Assert.That(score, Is.EqualTo(ParameterValue.FromInt(7)));
            Assert.That(phase.CurrentPhaseKey, Is.EqualTo("Night"));
        }

        [Test]
        public void Read_RejectsUnsupportedSchemaVersion()
        {
            const string json =
                "{\"SchemaVersion\":2," +
                "\"SceneKey\":\"Factory\"," +
                "\"Parameters\":{\"SchemaVersion\":1,\"Values\":[]}," +
                "\"Participants\":[{" +
                "\"SaveKey\":\"GameFlow.CurrentPhase\"," +
                "\"Snapshot\":{\"CurrentPhaseKey\":\"Night\"}}]}";
            SaveParticipantRegistry registry = new SaveParticipantRegistry();
            registry.Register(new PhaseParticipant("Morning"));

            Assert.Throws<InvalidOperationException>(
                () => new GameSaveDocumentJsonCodec().Read(json, registry));
        }

        [TestCase("null")]
        [TestCase("\"\"")]
        [TestCase("\"   \"")]
        public void Read_RejectsMissingSceneKey(string sceneKeyJson)
        {
            string json =
                "{\"SchemaVersion\":1," +
                $"\"SceneKey\":{sceneKeyJson}," +
                "\"Parameters\":{\"SchemaVersion\":1,\"Values\":[]}," +
                "\"Participants\":[]}";

            Assert.Throws<InvalidOperationException>(
                () => new GameSaveDocumentJsonCodec().Read(
                    json,
                    new SaveParticipantRegistry()));
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
                () => new GameSaveDocumentJsonCodec().Write(
                    sceneKey,
                    parameters,
                    new SaveParticipantRegistry().Capture()));
        }

        [Test]
        public void Read_RejectsMissingParameters()
        {
            const string json =
                "{\"SchemaVersion\":1," +
                "\"SceneKey\":\"Factory\"," +
                "\"Participants\":[]}";

            Assert.Throws<InvalidOperationException>(
                () => new GameSaveDocumentJsonCodec().Read(
                    json,
                    new SaveParticipantRegistry()));
        }

        [Test]
        public void Read_RejectsMissingParticipants()
        {
            const string json =
                "{\"SchemaVersion\":1," +
                "\"SceneKey\":\"Factory\"," +
                "\"Parameters\":{\"SchemaVersion\":1,\"Values\":[]}}";

            Assert.Throws<InvalidOperationException>(
                () => new GameSaveDocumentJsonCodec().Read(
                    json,
                    new SaveParticipantRegistry()));
        }

        public sealed class PhaseSnapshot
        {
            public string CurrentPhaseKey;
        }

        private sealed class PhaseParticipant :
            ISaveParticipant<PhaseSnapshot>
        {
            public PhaseParticipant(string currentPhaseKey)
            {
                CurrentPhaseKey = currentPhaseKey;
            }

            public string SaveKey => "GameFlow.CurrentPhase";
            public string CurrentPhaseKey { get; private set; }

            public PhaseSnapshot Capture()
            {
                return new PhaseSnapshot
                {
                    CurrentPhaseKey = CurrentPhaseKey
                };
            }

            public void Restore(PhaseSnapshot snapshot)
            {
                CurrentPhaseKey = snapshot.CurrentPhaseKey;
            }
        }
    }
}
