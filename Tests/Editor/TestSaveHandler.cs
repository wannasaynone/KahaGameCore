using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.Tests
{
    public class TestSaveHandler : MonoBehaviour
    {
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
            Common.SaveDataHandler saveDataHandler = new Common.SaveDataHandler(new Common.GameStaticDataSerializer(), new Common.GameStaticDataDeserializer());

            TestGameSave testSave = new TestGameSave(System.Guid.NewGuid().ToString(), 33, 50000, new TestGameSave.NestGameSave[]
            {
                new TestGameSave.NestGameSave(1, 100),
                new TestGameSave.NestGameSave(2, 1),
                new TestGameSave.NestGameSave(3, 50)
            });

            TestGameSave loadSave = saveDataHandler.LoadSave<TestGameSave>();
            Assert.IsNull(loadSave);

            saveDataHandler.Save(testSave);
            loadSave = saveDataHandler.LoadSave<TestGameSave>();

            Assert.IsNotNull(loadSave);
            Assert.AreEqual(testSave.UID, loadSave.UID);
            Assert.AreEqual(testSave.Day, loadSave.Day);
            Assert.AreEqual(testSave.Money, loadSave.Money);
            Assert.AreEqual(testSave.NestGameSaves.Length, loadSave.NestGameSaves.Length);
            Assert.AreEqual(testSave.NestGameSaves[0].CharacterID, loadSave.NestGameSaves[0].CharacterID);
            Assert.AreEqual(testSave.NestGameSaves[2].CharacterLevel, loadSave.NestGameSaves[2].CharacterLevel);

            Assert.IsTrue(saveDataHandler.DeleteSave<TestGameSave>());
            loadSave = saveDataHandler.LoadSave<TestGameSave>();
            Assert.IsNull(loadSave);
        }
    }
}
