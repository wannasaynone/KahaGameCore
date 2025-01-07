using KahaGameCore.GameEvent;
using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public class GameEventTest
    {
        public class TestGameEvent : GameEventBase { }

        [Test]
        public void normal_publish()
        {
            EventBus.Subscribe<TestGameEvent>(OnTestGameEventReceived);
            EventBus.Publish(new TestGameEvent());
        }

        private void OnTestGameEventReceived(TestGameEvent e)
        {
            EventBus.Unsubscribe<TestGameEvent>(OnTestGameEventReceived);
            Assert.Pass();
        }

        [Test]
        public void publish_and_resub()
        {
            EventBus.Subscribe<TestGameEvent>(OnTestGameEventReceived_Resub);
            EventBus.Publish(new TestGameEvent());
        }

        private void OnTestGameEventReceived_Resub(TestGameEvent e)
        {
            EventBus.Unsubscribe<TestGameEvent>(OnTestGameEventReceived_Resub);
            EventBus.Subscribe<TestGameEvent>(OnTestGameEventReceived);
            EventBus.Publish(new TestGameEvent());
        }

        public class TestGameEvent2 : GameEventBase { }
        public class TestGameEvent3 : GameEventBase { }
        public int count = 0;

        [Test]
        public void publish_multiple()
        {
            EventBus.ForceClearAll();
            count = 0;
            EventBus.Subscribe<TestGameEvent2>(OnTestGameEventReceived2);
            EventBus.Subscribe<TestGameEvent3>(OnTestGameEventReceived3);
            EventBus.Publish(new TestGameEvent2());
        }

        private void OnTestGameEventReceived2(TestGameEvent2 e)
        {
            count++;
            EventBus.Unsubscribe<TestGameEvent2>(OnTestGameEventReceived2);
            EventBus.Unsubscribe<TestGameEvent3>(OnTestGameEventReceived3);
            EventBus.Subscribe<TestGameEvent2>(OnTestGameEventReceived2);
            EventBus.Subscribe<TestGameEvent3>(OnTestGameEventReceived3);
            EventBus.Publish(new TestGameEvent3());
        }

        private void OnTestGameEventReceived3(TestGameEvent3 e)
        {
            count++;
            EventBus.Unsubscribe<TestGameEvent2>(OnTestGameEventReceived2);
            EventBus.Unsubscribe<TestGameEvent3>(OnTestGameEventReceived3);
            Assert.AreEqual(2, count);
        }
    }
}