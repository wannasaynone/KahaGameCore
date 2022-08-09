using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public class GameDataSerializerImplementTest
    {
        public class TestData
        {
            public int SomeValue;
        }

        [Test]
        public void serialize()
        {
            GameData.Implement.GameStaticDataSerializer serializer = new GameData.Implement.GameStaticDataSerializer();

            string json = serializer.Write(new TestData { SomeValue = 100 });

            Assert.AreEqual("{\"SomeValue\":100}", json.Trim());
        }
    }
}
