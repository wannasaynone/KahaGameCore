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
    }
}