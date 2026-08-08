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
            KahaGameCore.Persistence.JsonSaveDataHandler saveDataHandler = new KahaGameCore.Persistence.JsonSaveDataHandler(new KahaGameCore.Serialization.GameStaticDataSerializer(), new KahaGameCore.Serialization.GameStaticDataDeserializer());

            TestGameSave testSave = new TestGameSave(System.Guid.NewGuid().ToString());

            TestGameSave loadSave = saveDataHandler.LoadSave<TestGameSave>(0);
            Assert.IsNull(loadSave);

            saveDataHandler.Save(testSave, 0);
            loadSave = saveDataHandler.LoadSave<TestGameSave>(0);

            Assert.IsNotNull(loadSave);
            Assert.AreEqual(testSave.UID, loadSave.UID);

            Assert.IsTrue(saveDataHandler.DeleteSave<TestGameSave>(0));
            loadSave = saveDataHandler.LoadSave<TestGameSave>(0);
            Assert.IsNull(loadSave);
        }
    }
}
