﻿using System.Threading.Tasks;
using KahaGameCore.GameData;
using NUnit.Framework;

namespace KahaGameCore.Tests
{
    [TestFixture]
    public class TestGameDataManager
    {
        private class TestData : IGameData
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
            GameStaticDataManager gameDataManager = new GameStaticDataManager();

            gameDataManager.Add<TestData>(new TestData[]
            {
                new TestData(1, "Test String", 3.14f),
                new TestData(2, "Test String 2", 6.28f)
            });

            Assert.AreEqual(2, gameDataManager.GetAllGameData<TestData>().Length);

            gameDataManager.Add<TestData>(new TestData[]
            {
                new TestData(1, "Test String", 3.14f),
                new TestData(2, "Test String 2", 6.28f),
                new TestData(3, "Test String 3", 9.42f)
            });

            Assert.AreEqual(2, gameDataManager.GetAllGameData<TestData>().Length);
        }

        [Test]
        public void LoadDataWithForceUpdateTest()
        {
            GameStaticDataManager gameDataManager = new GameStaticDataManager();

            gameDataManager.Add<TestData>(new TestData[]
            {
                new TestData(1, "Test String", 3.14f),
                new TestData(2, "Test String 2", 6.28f)
            });

            Assert.AreEqual(2, gameDataManager.GetAllGameData<TestData>().Length);

            gameDataManager.Add<TestData>(new TestData[]
            {
                new TestData(1, "Test String", 3.14f),
                new TestData(2, "Test String 2", 6.28f),
                new TestData(3, "Test String 3", 9.42f)
            }, true);

            Assert.AreEqual(3, gameDataManager.GetAllGameData<TestData>().Length);
        }

        public class TestLoadHandler : IGameStaticDataHandler
        {
            public T[] Load<T>() where T : IGameData
            {
                return new TestData[]
                {
                    new TestData(1, "Test String", 3.14f),
                    new TestData(2, "Test String 2", 6.28f),
                    new TestData(3, "Test String 3", 9.42f)
                } as T[];
            }

            public async Task<T[]> LoadAsync<T>() where T : IGameData
            {
                await Task.Delay(100);

                return (new TestData[]
                {
                    new TestData(1, "Test String", 3.14f),
                    new TestData(2, "Test String 2", 6.28f),
                    new TestData(3, "Test String 3", 9.42f)
                } as T[]);
            }
        }

        [Test]
        public void LoadWithHandlerTest()
        {
            GameStaticDataManager gameDataManager = new GameStaticDataManager();
            TestLoadHandler testLoadHandler = new TestLoadHandler();

            gameDataManager.Add<TestData>(testLoadHandler);
            Assert.AreEqual(3, gameDataManager.GetAllGameData<TestData>().Length);
        }

        [Test]
        public async void LoadWithHandlerAsyncTest()
        {
            GameStaticDataManager gameDataManager = new GameStaticDataManager();
            TestLoadHandler testLoadHandler = new TestLoadHandler();

            await gameDataManager.AddAsync<TestData>(testLoadHandler);
            Assert.AreEqual(3, gameDataManager.GetAllGameData<TestData>().Length);
        }

        [Test]
        public void GetDataTest()
        {
            GameStaticDataManager gameDataManager = new GameStaticDataManager();

            gameDataManager.Add<TestData>(new TestData[]
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

            testData = gameDataManager.GetGameData<TestData>(3);
            Assert.IsNull(testData);
        }

        [Test]
        public void UnloadDataTest()
        {
            GameStaticDataManager gameDataManager = new GameStaticDataManager();

            Assert.IsNull(gameDataManager.GetAllGameData<TestData>());

            gameDataManager.Add<TestData>(new TestData[]
            {
                new TestData(1, "Test String", 3.14f),
                new TestData(2, "Test String 2", 6.28f)
            });

            gameDataManager.Remove<TestData>();
            Assert.IsNull(gameDataManager.GetAllGameData<TestData>());
        }
    }
}
