using System;
using NUnit.Framework;
using Zenject;

namespace KahaGameCore.Tests
{
    [TestFixture]
    public class TestGameDataManager
    {
        private class TestData : GameData.IGameData
        {
            public int ID { get; private set; }
            public string TestStringField { get; private set; }
            public float TestFloatField { get; private set; }

            public TestData() { }
            public TestData(int id, string testString, float testFloat)
            {
                ID = id;
                TestStringField = testString;
                TestFloatField = testFloat;
            }
        }

        [Test]
        public void LoadDataTest()
        {
            GameData.GameStaticDataManager gameDataManager = new GameData.GameStaticDataManager();

            gameDataManager.Load<TestData>(new TestData[]
            {
                new TestData(1, "Test String", 3.14f),
                new TestData(2, "Test String 2", 6.28f)
            });

            Assert.AreEqual(2, gameDataManager.GetAllGameData<TestData>().Length);

            gameDataManager.Load<TestData>(new TestData[]
            {
                new TestData(1, "Test String", 3.14f),
                new TestData(2, "Test String 2", 6.28f),
                new TestData(3, "Test String 3", 9.42f)
            });

            Assert.AreEqual(2, gameDataManager.GetAllGameData<TestData>().Length);

            gameDataManager.Load<TestData>(new TestData[]
            {
                new TestData(1, "Test String", 3.14f),
                new TestData(2, "Test String 2", 6.28f),
                new TestData(3, "Test String 3", 9.42f)
            }, true);

            Assert.AreEqual(3, gameDataManager.GetAllGameData<TestData>().Length);
        }

        [Test]
        public void GetDataTest()
        {
            GameData.GameStaticDataManager gameDataManager = new GameData.GameStaticDataManager();

            gameDataManager.Load<TestData>(new TestData[]
            {
                new TestData(1, "Test String", 3.14f),
                new TestData(2, "Test String 2", 6.28f)
            });

            Assert.AreEqual(2, gameDataManager.GetAllGameData<TestData>().Length);

            TestData testData = gameDataManager.GetGameData<TestData>(1);
            Assert.IsNotNull(testData);
            Assert.AreEqual("Test String", testData.TestStringField);
            Assert.IsTrue(UnityEngine.Mathf.Approximately(testData.TestFloatField, 3.14f));

            testData = gameDataManager.GetGameData<TestData>(2);
            Assert.IsNotNull(testData);
            Assert.AreEqual("Test String 2", testData.TestStringField);
            Assert.IsTrue(UnityEngine.Mathf.Approximately(testData.TestFloatField, 6.28f));
        }

        [Test]
        public void UnloadDataTest()
        {
            GameData.GameStaticDataManager gameDataManager = new GameData.GameStaticDataManager();

            Assert.IsNull(gameDataManager.GetAllGameData<TestData>());

            gameDataManager.Load<TestData>(new TestData[]
            {
                new TestData(1, "Test String", 3.14f),
                new TestData(2, "Test String 2", 6.28f)
            });

            gameDataManager.Unload<TestData>();
            Assert.IsNull(gameDataManager.GetAllGameData<TestData>());
        }
    }
}
