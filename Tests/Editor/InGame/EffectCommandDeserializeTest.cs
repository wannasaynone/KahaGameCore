using System;
using KahaGameCore.Combat.Processor.EffectProcessor;
using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public class EffectCommandDeserializeTest
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
                onCompleted?.Invoke();
            }
        }

        [Test]
        public void Deserialize()
        {
            string testData = 
                  "Test" +
                  " { " +
                  "    DebugLog(this-is-some-debug-message-in-test-data1);" +
                  "    DebugLog(this-is-some-debug-message-in-test-data2);" +
                  " } " +
                  "Test2" +
                  " { " +
                  "    DebugLog(this-is-some-debug-message-in-test-data-in-test-2-step);" +
                  " } ";

            EffectCommandFactoryContainer effectCommandFactoryContainer = new EffectCommandFactoryContainer();
            effectCommandFactoryContainer.RegisterFactory("DebugLog", new DebugLogEffectCommandFatory());

            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<EffectProcessor.EffectData>> timingToEffectDatas
                = new EffectCommandDeserializer(effectCommandFactoryContainer).Deserialize(testData);

            Assert.AreEqual(2, timingToEffectDatas.Count);
            Assert.AreEqual(2, timingToEffectDatas["Test"].Count);
        }

        [Test]
        public void Deserialize_if_miss_block()
        {
            string testData = 
                  "Test" +
                  " { " +
                  "    DebugLog(this-is-some-debug-message-in-test-data);" +
                  "  " +
                  "Test2" +
                  " { " +
                  "    DebugLog(this-is-some-debug-message-in-test-data-in-test-2-step);" +
                  " } ";

            EffectCommandFactoryContainer effectCommandFactoryContainer = new EffectCommandFactoryContainer();
            effectCommandFactoryContainer.RegisterFactory("DebugLog", new DebugLogEffectCommandFatory());

            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<EffectProcessor.EffectData>> timingToEffectDatas
                = new EffectCommandDeserializer(effectCommandFactoryContainer).Deserialize(testData);

            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "[EffectProcesser][GetEffectCommand] Invaild command=Test2{DebugLog");
            Assert.AreEqual(1, timingToEffectDatas.Count);
            Assert.AreEqual(1, timingToEffectDatas["Test"].Count);
        }

        [Test]
        public void Deserialize_if_miss_semicolon()
        {
            string testData =
                  "Test" +
                  " { " +
                  "    DebugLog(this-is-some-debug-message-in-test-data)" +
                  "    DebugLog(this-is-some-debug-message-in-test-data2);" +
                  " } ";

            EffectCommandFactoryContainer effectCommandFactoryContainer = new EffectCommandFactoryContainer();
            effectCommandFactoryContainer.RegisterFactory("DebugLog", new DebugLogEffectCommandFatory());

            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<EffectProcessor.EffectData>> timingToEffectDatas
                = new EffectCommandDeserializer(effectCommandFactoryContainer).Deserialize(testData);

            Assert.AreEqual(1, timingToEffectDatas["Test"].Count);
            Assert.AreEqual(1, timingToEffectDatas["Test"][0].GetVarsLength());
        }
    }
}