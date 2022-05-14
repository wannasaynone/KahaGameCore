using System;
using NUnit.Framework;
using Zenject;

namespace KahaGameCore.Tests
{
    [TestFixture]
    public class KahaGameCoreTester
    {
        DiContainer Container;

        [SetUp]
        public virtual void Setup()
        {
            Container = new DiContainer(StaticContext.Container);
            Container.Bind<Main.Core>().AsSingle();
            Container.Bind<Common.GameDataManager>().AsSingle();
        }

        [TearDown]
        public virtual void Teardown()
        {
            StaticContext.Clear();
        }

        private class TestData : Common.IGameData
        {
            public int ID { get; private set; }
            public string TestStringField { get; private set; }
            public float TestFloatField { get; private set; }

            public TestData() { }
        }

        [Test]
        public void DeserializeGameDataTest()
        {
            string testJson = "[\n" +
                                  "{ " +
                                  "  \"ID\" : 1,\n" +
                                  "  \"TestStringField\" : \"Test String\",\n" +
                                  "  \"TestFloatField\" : 3.14\n" +
                                  "},\n" +
                                  "{ " +
                                  "  \"ID\" : 2,\n" +
                                  "  \"TestStringField\" : \"Test String 2\",\n" +
                                  "  \"TestFloatField\" : 6.28\n" +
                                  "}\n" +
                              "]";

            Assert.IsNull(Container.Resolve<Main.Core>().GameDataManager.GetAllGameData<TestData>());
            Assert.IsNull(Container.Resolve<Main.Core>().GameDataManager.GetGameData<TestData>(1));

            Container.Resolve<Main.Core>().GameDataManager.DeserializeGameData<TestData>(testJson);
            Assert.AreEqual(2, Container.Resolve<Main.Core>().GameDataManager.GetAllGameData<TestData>().Length);

            TestData testData = Container.Resolve<Main.Core>().GameDataManager.GetGameData<TestData>(1);
            Assert.IsNotNull(testData);
            Assert.AreEqual("Test String", testData.TestStringField);
            Assert.IsTrue(UnityEngine.Mathf.Approximately(testData.TestFloatField, 3.14f));

            testData = Container.Resolve<Main.Core>().GameDataManager.GetGameData<TestData>(2);
            Assert.IsNotNull(testData);
            Assert.AreEqual("Test String 2", testData.TestStringField);
            Assert.IsTrue(UnityEngine.Mathf.Approximately(testData.TestFloatField, 6.28f));

            Container.Resolve<Main.Core>().GameDataManager.Unload<TestData>();
            Assert.IsNull(Container.Resolve<Main.Core>().GameDataManager.GetAllGameData<TestData>());
            Assert.IsNull(Container.Resolve<Main.Core>().GameDataManager.GetGameData<TestData>(1));
        }

        private class TestGameSave
        {
            public string UID { get; private set; }
            public int Day { get; private set; }
            public int Money { get; private set; }
            public NestGameSave[] NestGameSaves { get; private set; }

            public class NestGameSave
            {
                public int CharacterID { get; private set; }
                public int CharacterLevel { get; private set; }

                public NestGameSave() { }
                public NestGameSave(int characterID, int characterLevel)
                {
                    CharacterID = characterID;
                    CharacterLevel = characterLevel;
                }
            }

            public TestGameSave() { }
            public TestGameSave(string uid, int day, int money, NestGameSave[] nestSaves)
            {
                UID = uid;
                Day = day;
                Money = money;
                NestGameSaves = nestSaves;
            }
        }

        [Test]
        public void SaveAndLoadTest()
        {
            TestGameSave testSave = new TestGameSave(System.Guid.NewGuid().ToString(), 33, 50000, new TestGameSave.NestGameSave[]
            {
                new TestGameSave.NestGameSave(1, 100),
                new TestGameSave.NestGameSave(2, 1),
                new TestGameSave.NestGameSave(3, 50)
            });

            TestGameSave loadSave = Container.Resolve<Main.Core>().GameDataManager.LoadSave<TestGameSave>();
            Assert.IsNull(loadSave);

            Container.Resolve<Main.Core>().GameDataManager.Save(testSave);
            loadSave = Container.Resolve<Main.Core>().GameDataManager.LoadSave<TestGameSave>();

            Assert.IsNotNull(loadSave);
            Assert.AreEqual(testSave.UID, loadSave.UID);
            Assert.AreEqual(testSave.Day, loadSave.Day);
            Assert.AreEqual(testSave.Money, loadSave.Money);
            Assert.AreEqual(testSave.NestGameSaves.Length, loadSave.NestGameSaves.Length);
            Assert.AreEqual(testSave.NestGameSaves[0].CharacterID, loadSave.NestGameSaves[0].CharacterID);
            Assert.AreEqual(testSave.NestGameSaves[2].CharacterLevel, loadSave.NestGameSaves[2].CharacterLevel);

            Assert.IsTrue(Container.Resolve<Main.Core>().GameDataManager.DeleteSave<TestGameSave>());
            loadSave = Container.Resolve<Main.Core>().GameDataManager.LoadSave<TestGameSave>();
            Assert.IsNull(loadSave);
        }
        
        // make a processser that add a value each
        // if value reaches 0, force quit
        private class TestProcessStep : Processer.IProcessable
        {
            private TestProcesserTarget m_target;
            private int m_add;

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

        private class TestProcesserTarget
        {
            public int value;
        }

        [Test]
        public void ProcesserTest()
        {
            TestProcesserTarget target = new TestProcesserTarget { value = 100 };
            TestProcessStep[] steps = new TestProcessStep[]
                {
                    new TestProcessStep(target, 100),
                    new TestProcessStep(target, 100),
                    new TestProcessStep(target, 100)
                };

            Processer.Processer<TestProcessStep> processer = new Processer.Processer<TestProcessStep>(steps);
            processer.Start(delegate { Assert.AreEqual(400, target.value); }, delegate { UnityEngine.Debug.LogError("should not go here"); });

            target = new TestProcesserTarget { value = 100 };
            steps = new TestProcessStep[]
                {
                    new TestProcessStep(target, -100),
                    new TestProcessStep(target, -100),
                    new TestProcessStep(target, -100)
                };

            processer = new Processer.Processer<TestProcessStep>(steps);
            processer.Start(delegate { UnityEngine.Debug.LogError("should not go here"); }, delegate { Assert.AreEqual(0, target.value); });
        }
    }
}
