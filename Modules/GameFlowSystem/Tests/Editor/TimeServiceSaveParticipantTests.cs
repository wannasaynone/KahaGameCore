using System;
using KahaGameCore.Foundation.Messaging;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.GameFlowSystem.DefaultImplements.DataAccess;
using KahaGameCore.GameFlowSystem.DefaultImplements.Events;
using KahaGameCore.Parameters;
using KahaGameCore.Persistence;
using KahaGameCore.StaticData;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem.Tests
{
    public sealed class TimeServiceSaveParticipantTests
    {
        [TearDown]
        public void TearDown()
        {
            MessageBus.ForceClearAll();
        }

        [Test]
        public void CaptureRestore_RoundTripsCurrentPhaseWithoutChangingDay()
        {
            TimeService time = CreateTimeService();
            time.ResetToFirstPhase();
            time.SetPhase("Night");
            ISaveParticipant<TimeServiceSnapshot> participant = time;

            TimeServiceSnapshot snapshot = participant.Capture();
            time.SetPhase("Morning");
            participant.Restore(snapshot);

            Assert.That(participant.SaveKey, Is.EqualTo("GameFlow.CurrentPhase"));
            Assert.That(snapshot.CurrentPhaseKey, Is.EqualTo("Night"));
            Assert.That(time.CurrentPhase.Key, Is.EqualTo("Night"));
            Assert.That(time.CurrentDay, Is.EqualTo(1));
        }

        [Test]
        public void SetPhase_RejectsUnknownPhaseKey()
        {
            TimeService time = CreateTimeService();
            time.ResetToFirstPhase();

            Assert.Throws<InvalidOperationException>(
                () => time.SetPhase("MissingPhase"));
            Assert.That(time.CurrentPhase.Key, Is.EqualTo("Morning"));
        }

        [Test]
        public void Capture_RejectsUninitializedCurrentPhase()
        {
            ISaveParticipant<TimeServiceSnapshot> participant =
                CreateTimeService();

            Assert.Throws<InvalidOperationException>(() => participant.Capture());
        }

        [Test]
        public void Restore_RejectsNullSnapshot()
        {
            ISaveParticipant<TimeServiceSnapshot> participant =
                CreateTimeService();

            Assert.Throws<ArgumentNullException>(
                () => participant.Restore(null));
        }

        [Test]
        public void Restore_RejectsUnknownPhaseKeyWithoutChangingCurrentPhase()
        {
            TimeService time = CreateTimeService();
            time.ResetToFirstPhase();
            ISaveParticipant<TimeServiceSnapshot> participant = time;
            TimeServiceSnapshot snapshot = new TimeServiceSnapshot
            {
                CurrentPhaseKey = "MissingPhase"
            };

            Assert.Throws<InvalidOperationException>(
                () => participant.Restore(snapshot));
            Assert.That(time.CurrentPhase.Key, Is.EqualTo("Morning"));
        }

        [Test]
        public void Restore_PublishesCurrentPhaseChangedMessage()
        {
            TimeService time = CreateTimeService();
            time.ResetToFirstPhase();
            time.SetPhase("Night");
            ISaveParticipant<TimeServiceSnapshot> participant = time;
            TimeServiceSnapshot snapshot = participant.Capture();
            time.SetPhase("Morning");
            TimePhaseChangedEvent received = null;
            MessageBus.Subscribe<TimePhaseChangedEvent>(message => received = message);

            participant.Restore(snapshot);

            Assert.That(received, Is.Not.Null);
            Assert.That(received.Phase.Key, Is.EqualTo("Night"));
            Assert.That(received.Day, Is.EqualTo(1));
        }

        private static TimeService CreateTimeService()
        {
            TextAsset phases = new TextAsset(
                "[{\"ID\":1,\"Key\":\"Morning\",\"DisplayName\":\"早晨\",\"NextID\":2,\"IsNewDay\":1}," +
                "{\"ID\":2,\"Key\":\"Night\",\"DisplayName\":\"晚上\",\"NextID\":1,\"IsNewDay\":0}]")
            {
                name = nameof(TimePhaseData)
            };
            GameStaticDataManager staticData = new GameStaticDataManager();
            staticData.Add<TimePhaseData>(
                new TextAssetJsonStaticDataHandler(new[] { phases }));
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int(
                    "Day",
                    "天數",
                    initialValue: 1,
                    minValue: 1,
                    maxValue: 999)
            });
            return new TimeService(staticData, parameters);
        }
    }
}
