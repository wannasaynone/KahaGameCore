using NUnit.Framework;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class GameEventDocumentJsonCodecTests
    {
        [Test]
        public void Read_MissingPriorityFailsExplicitly()
        {
            const string json = @"{
  ""SchemaVersion"": 1,
  ""DocumentGuid"": ""40000000-0000-0000-0000-000000000001"",
  ""DisplayName"": ""Missing Priority"",
  ""Condition"": """",
  ""Commands"": """"
}";

            GameEventException exception = Assert.Throws<GameEventException>(() =>
                new GameEventDocumentJsonCodec().Read(json));

            Assert.That(exception.Code, Is.EqualTo("MissingField"));
            StringAssert.Contains("Priority", exception.Message);
        }

        [Test]
        public void ReadAndWrite_PreservesSchemaIdentityAndCommands()
        {
            const string json = @"{
  ""SchemaVersion"": 1,
  ""DocumentGuid"": ""40000000-0000-0000-0000-000000000002"",
  ""DisplayName"": ""Round Trip"",
  ""TriggerTiming"": ""tick"",
  ""Condition"": ""$Gate == 0"",
  ""Priority"": 42,
  ""Commands"": ""Record(ok);""
}";
            GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();

            GameEventDocument first = codec.Read(json);
            GameEventDocument second = codec.Read(codec.Write(first));

            Assert.That(second.SchemaVersion, Is.EqualTo(1));
            Assert.That(second.DocumentGuid, Is.EqualTo(first.DocumentGuid));
            Assert.That(second.TriggerTiming, Is.EqualTo("tick"));
            Assert.That(second.Priority, Is.EqualTo(42));
            Assert.That(second.Commands, Is.EqualTo("Record(ok);"));
        }
    }
}
