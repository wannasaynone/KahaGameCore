using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public class TestGameDataSerializer 
    {
        public class TestData
        {
            public int SomeValue;
        }

        [Test]
        public void GameDataSerializerTest()
        {
            Common.GameStaticDataSerializer serializer = new Common.GameStaticDataSerializer();

            string json = serializer.Write(new TestData { SomeValue = 100 });

            Assert.AreEqual("{\"SomeValue\":100}", json.Trim());
        }
    }
}
