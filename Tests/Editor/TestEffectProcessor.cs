using KahaGameCore.Combat.Processor.EffectProcessor;
using NUnit.Framework;
using System;
using Zenject;

namespace KahaGameCore.Tests
{
    public class TestEffectProcessor 
    {
        private class DebugLogEffectCommand : EffectCommandBase
        {
            public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
            {
                UnityEngine.Debug.Log(vars[0]);
                if (vars.Length >= 2 && vars[1] == "0")
                {
                    onCompleted?.Invoke();
                }
                else
                {
                    onForceQuit?.Invoke();
                }
            }
        }

        [Test]
        public void EffectProcessorTest()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.Resolve<SignalBus>().DeclareSignal<EffectTimingTriggedSignal>();

            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<EffectProcessor.EffectData>> timingToEffectDatas = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<EffectProcessor.EffectData>>
            {
                { "Test", new System.Collections.Generic.List<EffectProcessor.EffectData> { new EffectProcessor.EffectData(new DebugLogEffectCommand(), new string[] { "TestMsg", "0" }) } }
            };

            EffectProcessor effectProcessor = new EffectProcessor(container.Resolve<SignalBus>());
            effectProcessor.OnProcessEnded += EffectProcessor_OnProcessEnded;
            effectProcessor.OnProcessQuitted += EffectProcessor_OnProcessQuitted;
            effectProcessor.Start(new EffectTimingTriggedSignal(null, null, "Test"));
            effectProcessor.SetUp(timingToEffectDatas);
            effectProcessor.Start(new EffectTimingTriggedSignal(null, null, "Test"));
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Log, "TestMsg");
        }

        private void EffectProcessor_OnProcessQuitted()
        {
            Assert.IsTrue(false);
        }

        private void EffectProcessor_OnProcessEnded()
        {
            Assert.IsTrue(true);
        }

        [Test]
        public void EffectProcessorQuitTest()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.Resolve<SignalBus>().DeclareSignal<EffectTimingTriggedSignal>();

            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<EffectProcessor.EffectData>> timingToEffectDatas = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<EffectProcessor.EffectData>>
            {
                { "Test", new System.Collections.Generic.List<EffectProcessor.EffectData> { new EffectProcessor.EffectData(new DebugLogEffectCommand(), new string[] { "TestMsg" }) } }
            };

            EffectProcessor effectProcessor = new EffectProcessor(container.Resolve<SignalBus>());
            effectProcessor.OnProcessEnded += EffectProcessor_OnProcessEnded1;
            effectProcessor.OnProcessQuitted += EffectProcessor_OnProcessQuitted1; ;

            effectProcessor.SetUp(timingToEffectDatas);
            effectProcessor.Start(new EffectTimingTriggedSignal(null, null, "Test"));
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Log, "TestMsg");
        }

        private void EffectProcessor_OnProcessQuitted1()
        {
            Assert.IsTrue(true);
        }

        private void EffectProcessor_OnProcessEnded1()
        {
            Assert.IsTrue(false);
        }
    }
}