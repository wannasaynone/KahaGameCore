using System;
using KahaGameCore.EffectCommand;
using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public class TestEffectCommand 
    {
        private class DebugLogEffectCommandFatory : EffectCommandFactoryBase
        {
            public override EffectCommandBase Create()
            {
                return new DebugLogEffectCommand();
            }
        }

        private class DebugLogEffectCommand : EffectCommandBase
        {
            public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
            {
                UnityEngine.Debug.Log(vars[0]);
            }
        }

        [Test]
        public void EffectProcesserInitialTest()
        {
            Zenject.DiContainer container = new Zenject.DiContainer();
            Zenject.SignalBusInstaller.Install(container);

            container.Resolve<Zenject.SignalBus>().DeclareSignal<EffectTimingTriggedSignal>();

            EffectCommandFactoryContainer effectCommandFactoryContainer = new EffectCommandFactoryContainer();
            effectCommandFactoryContainer.RegisterFactory("DebugLog", new DebugLogEffectCommandFatory());

            EffectProcesser testEffectProcesser = new EffectProcesser(container.Resolve<Zenject.SignalBus>(), effectCommandFactoryContainer);

            string testData = "Test" +
                              " { " +
                              "    DebugLog(this-is-some-debug-message-in-test-data);" +
                              " } " +
                              "Test2" +
                              " { " +
                              "    DebugLog(this-is-some-debug-message-in-test-data-in-test-2-step);" +
                              " } ";

            testEffectProcesser.Initial(testData);
            testEffectProcesser.Start(new EffectTimingTriggedSignal(new EffectProcesser.ProcessData
            {
                caster = null,
                target = null,
                skipIfCount = 0,
                timing = "Test"
            }));

            testEffectProcesser.Start(new EffectTimingTriggedSignal(new EffectProcesser.ProcessData
            {
                caster = null,
                target = null,
                skipIfCount = 0,
                timing = "Test2"
            }));

            testEffectProcesser.Start(new EffectTimingTriggedSignal(new EffectProcesser.ProcessData
            {
                caster = null,
                target = null,
                skipIfCount = 0,
                timing = "Test3"
            }));
        }
    }
}