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
        public void Serialize()
        {
            KahaGameCore.Serialization.GameStaticDataSerializer serializer = new KahaGameCore.Serialization.GameStaticDataSerializer();

            string json = serializer.Write(new TestData { SomeValue = 100 });

            Assert.AreEqual("{\"SomeValue\":100}", json.Trim());
        }
    }
}
