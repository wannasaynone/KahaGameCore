using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KahaGameCore.Samples.GameSaveTest.Tests
{
    public sealed class GameSaveTestControllerTests
    {
        private GameObject host;
        private GameSaveTestController controller;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            host = new GameObject("GameSaveTestController");
            controller = host.AddComponent<GameSaveTestController>();
            yield return null;
            controller.DeleteSave();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (controller != null)
            {
                controller.DeleteSave();
            }

            Object.Destroy(host);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SaveThenLoad_RestoresParametersParticipantAndVisualBinding()
        {
            controller.MutateState();
            Assert.That(controller.MachineStage, Is.EqualTo(1));
            Assert.That(controller.CurrentPhaseKey, Is.EqualTo("Night"));
            Assert.That(controller.PlayerPosition.x, Is.EqualTo(3f));
            Assert.That(controller.StateAActive, Is.False);
            Assert.That(controller.StateBActive, Is.True);

            yield return controller.SaveAsync().ToCoroutine();
            Assert.That(controller.SaveExists, Is.True);

            controller.MutateState();
            Assert.That(controller.MachineStage, Is.EqualTo(2));
            Assert.That(controller.CurrentPhaseKey, Is.EqualTo("Morning"));
            Assert.That(controller.PlayerPosition.x, Is.EqualTo(-3f));

            controller.Load();

            Assert.That(controller.MachineStage, Is.EqualTo(1));
            Assert.That(controller.CurrentPhaseKey, Is.EqualTo("Night"));
            Assert.That(controller.PlayerPosition.x, Is.EqualTo(3f));
            Assert.That(controller.StateAActive, Is.False);
            Assert.That(controller.StateBActive, Is.True);
        }
    }
}
