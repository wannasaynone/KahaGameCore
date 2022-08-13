using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public class GameDataDeserializerImplementTest
    {
        private readonly string testJson = "[\n" +
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

        private class TestData
        {
            public int ID { get; private set; }
            public string TestStringField { get; private set; }
            public float TestFloatField { get; private set; }

            public TestData() { }
        }

        [Test]
        public void Deserialize()
        {
            GameData.Implemented.GameStaticDataDeserializer deserializer = new GameData.Implemented.GameStaticDataDeserializer();
            TestData[] datas = deserializer.Read<TestData[]>(testJson);

            Assert.IsNotNull(datas);
            Assert.AreEqual(2, datas.Length);
            Assert.AreEqual(2, datas[1].ID);
            Assert.AreEqual("Test String", datas[0].TestStringField);
        }
    }
}