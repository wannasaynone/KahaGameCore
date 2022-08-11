using System;
using KahaGameCore.Combat.InGameEvent;
using NUnit.Framework;


namespace KahaGameCore.Tests
{
    public class InGameEventCenterTest 
    {
        private class TestEvent : InGameEventCenter.InGameEvent { }

        [Test]
        public void register_and_publish()
        {
            int count = 0;
            InGameEventCenter.Register<TestEvent>(delegate
            {
                count++;
            });
            InGameEventCenter.Publish(new TestEvent());
            Assert.AreEqual(1, count);
        }

        [Test]
        public void unregister()
        {
            int count = 0;
            Action<TestEvent> testAction = delegate
            {
                count++;
            };
            InGameEventCenter.Register(testAction);
            InGameEventCenter.Unregister(testAction);
            InGameEventCenter.Publish(new TestEvent());
            Assert.AreEqual(0, count);
        }
    }
}
