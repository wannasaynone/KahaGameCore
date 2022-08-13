using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.Tests
{
    public class SaveHandlerTest : MonoBehaviour
    {
        private class TestGameSave
        {
            public string UID { get; private set; }

            public TestGameSave() { }
            public TestGameSave(string uid)
            {
                UID = uid;
            }
        }

        [Test]
        public void Save_and_load()
        {
            // GameStaticDataSerializer and GameStaticDataDeserializer have its own test
            // make sure they are all correct before run this test
            GameData.Implemented.JsonSaveDataHandler saveDataHandler = new GameData.Implemented.JsonSaveDataHandler(new GameData.Implemented.GameStaticDataSerializer(), new GameData.Implemented.GameStaticDataDeserializer());

            TestGameSave testSave = new TestGameSave(System.Guid.NewGuid().ToString());

            TestGameSave loadSave = saveDataHandler.LoadSave<TestGameSave>();
            Assert.IsNull(loadSave);

            saveDataHandler.Save(testSave);
            loadSave = saveDataHandler.LoadSave<TestGameSave>();

            Assert.IsNotNull(loadSave);
            Assert.AreEqual(testSave.UID, loadSave.UID);

            Assert.IsTrue(saveDataHandler.DeleteSave<TestGameSave>());
            loadSave = saveDataHandler.LoadSave<TestGameSave>();
            Assert.IsNull(loadSave);
        }
    }
}
