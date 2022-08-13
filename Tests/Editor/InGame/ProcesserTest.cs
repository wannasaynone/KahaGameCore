using System;
using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public class ProcesserTest
    {
        private class TestProcesserTarget
        {
            public int value;
        }

        // make a processser that add a value each
        // if value reaches 0, force quit
        private class TestProcessStep : Processor.IProcessable
        {
            private TestProcesserTarget m_target;
            private readonly int m_add;

            public TestProcessStep(TestProcesserTarget target, int add)
            {
                m_target = target;
                m_add += add;
            }

            public void Process(Action onCompleted, Action onForceQuit)
            {
                if (m_target == null)
                {
                    onForceQuit?.Invoke();
                    return;
                }

                m_target.value += m_add;
                if (m_target.value <= 0)
                {
                    onForceQuit?.Invoke();
                    return;
                }

                onCompleted?.Invoke();
            }
        }

        [Test]
        public void Process()
        {
            TestProcesserTarget target = new TestProcesserTarget { value = 100 };
            TestProcessStep[] steps = new TestProcessStep[]
                {
                    new TestProcessStep(target, 100),
                    new TestProcessStep(target, 100),
                    new TestProcessStep(target, 100)
                };

            Processor.Processor<TestProcessStep> processer = new Processor.Processor<TestProcessStep>(steps);
            processer.Start(delegate { Assert.AreEqual(400, target.value); }, delegate { UnityEngine.Debug.LogError("should not go here"); });

            target = new TestProcesserTarget { value = 100 };
            steps = new TestProcessStep[]
                {
                    new TestProcessStep(target, -100),
                    new TestProcessStep(target, -100),
                    new TestProcessStep(target, -100)
                };

            processer = new Processor.Processor<TestProcessStep>(steps);
            processer.Start(delegate { UnityEngine.Debug.LogError("should not go here"); }, delegate { Assert.AreEqual(0, target.value); });
        }
    }

}
