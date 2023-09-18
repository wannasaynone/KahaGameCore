using KahaGameCore.Combat.Processor.EffectProcessor;
using NUnit.Framework;
using System;

namespace KahaGameCore.Tests
{
    public class EffectProcessorTest
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
        public void Process()
        {
            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<EffectProcessor.EffectData>> timingToEffectDatas = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<EffectProcessor.EffectData>>
            {
                { "Test", new System.Collections.Generic.List<EffectProcessor.EffectData> { new EffectProcessor.EffectData(new DebugLogEffectCommand(), new string[] { "TestMsg", "0" }) } }
            };

            EffectProcessor effectProcessor = new EffectProcessor();
            effectProcessor.OnProcessEnded += EffectProcessor_OnProcessEnded;
            effectProcessor.OnProcessQuitted += EffectProcessor_OnProcessQuitted;
            effectProcessor.Start(new Combat.ProcessData { caster = null, skipIfCount = 0, targets = null, timing = "Test" });
            effectProcessor.SetUp(timingToEffectDatas);
            effectProcessor.Start(new Combat.ProcessData { caster = null, skipIfCount = 0, targets = null, timing = "Test" });
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
        public void Quit()
        {
            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<EffectProcessor.EffectData>> timingToEffectDatas = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<EffectProcessor.EffectData>>
            {
                { "Test", new System.Collections.Generic.List<EffectProcessor.EffectData> { new EffectProcessor.EffectData(new DebugLogEffectCommand(), new string[] { "TestMsg" }) } }
            };

            EffectProcessor effectProcessor = new EffectProcessor();
            effectProcessor.OnProcessEnded += EffectProcessor_OnProcessEnded1;
            effectProcessor.OnProcessQuitted += EffectProcessor_OnProcessQuitted1; ;

            effectProcessor.SetUp(timingToEffectDatas);
            effectProcessor.Start(new Combat.ProcessData { caster = null, skipIfCount = 0, targets = null, timing = "Test" });
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

        private class TestIfEffectCommand : EffectCommandBase
        {
            public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
            {
                processData.skipIfCount++;
                onCompleted?.Invoke();
            }
        }

        [Test]
        public void Process_with_special_command_if()
        {
            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<EffectProcessor.EffectData>> timingToEffectDatas = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<EffectProcessor.EffectData>>
            {
                {
                    "Test", new System.Collections.Generic.List<EffectProcessor.EffectData>
                            {
                                new EffectProcessor.EffectData(new TestIfEffectCommand(), new string[0]),
                                new EffectProcessor.EffectData(new DebugLogEffectCommand(), new string[] { "TestMsg", "0" })
                            }
                }
            };

            EffectProcessor effectProcessor = new EffectProcessor();
            effectProcessor.OnProcessEnded += EffectProcessor_OnProcessEnded2;

            effectProcessor.SetUp(timingToEffectDatas);
            effectProcessor.Start(new Combat.ProcessData { caster = null, skipIfCount = 0, targets = null, timing = "Test" });
        }

        private void EffectProcessor_OnProcessEnded2()
        {
            UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
        }
    }
}