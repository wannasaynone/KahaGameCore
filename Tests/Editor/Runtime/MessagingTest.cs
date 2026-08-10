using KahaGameCore.Foundation.Messaging;
using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public class MessagingTest
    {
        public class TestMessage : MessageBase { }

        [Test]
        public void normal_publish()
        {
            MessageBus.Subscribe<TestMessage>(OnTestMessageReceived);
            MessageBus.Publish(new TestMessage());
        }

        private void OnTestMessageReceived(TestMessage message)
        {
            MessageBus.Unsubscribe<TestMessage>(OnTestMessageReceived);
            Assert.Pass();
        }

        [Test]
        public void publish_and_resub()
        {
            MessageBus.Subscribe<TestMessage>(OnTestMessageReceivedResubscribe);
            MessageBus.Publish(new TestMessage());
        }

        private void OnTestMessageReceivedResubscribe(TestMessage message)
        {
            MessageBus.Unsubscribe<TestMessage>(OnTestMessageReceivedResubscribe);
            MessageBus.Subscribe<TestMessage>(OnTestMessageReceived);
            MessageBus.Publish(new TestMessage());
        }

        public class TestMessage2 : MessageBase { }
        public class TestMessage3 : MessageBase { }
        public int count = 0;

        [Test]
        public void publish_multiple()
        {
            MessageBus.ForceClearAll();
            count = 0;
            MessageBus.Subscribe<TestMessage2>(OnTestMessageReceived2);
            MessageBus.Subscribe<TestMessage3>(OnTestMessageReceived3);
            MessageBus.Publish(new TestMessage2());
        }

        private void OnTestMessageReceived2(TestMessage2 message)
        {
            count++;
            MessageBus.Unsubscribe<TestMessage2>(OnTestMessageReceived2);
            MessageBus.Unsubscribe<TestMessage3>(OnTestMessageReceived3);
            MessageBus.Subscribe<TestMessage2>(OnTestMessageReceived2);
            MessageBus.Subscribe<TestMessage3>(OnTestMessageReceived3);
            MessageBus.Publish(new TestMessage3());
        }

        private void OnTestMessageReceived3(TestMessage3 message)
        {
            count++;
            MessageBus.Unsubscribe<TestMessage2>(OnTestMessageReceived2);
            MessageBus.Unsubscribe<TestMessage3>(OnTestMessageReceived3);
            Assert.AreEqual(2, count);
        }
    }
}
