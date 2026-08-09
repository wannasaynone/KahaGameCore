using System.Linq;
using NUnit.Framework;

namespace KahaGameCore.Parameters.Tests
{
    public class ParameterTableJsonCodecTests
    {
        [Test]
        public void ReadWrite_RoundTripsTableWithMultipleTypedParameters()
        {
            const string json = @"{
  ""SchemaVersion"": 1,
  ""TableGuid"": ""28a2f269-173a-48d2-a8db-9cd6832ee2f3"",
  ""DisplayName"": ""Core Gameplay"",
  ""Parameters"": [
    {
      ""Key"": ""Supplies"",
      ""DisplayName"": ""物資"",
      ""Type"": ""Int"",
      ""InitialValue"": ""60"",
      ""MinValue"": ""0"",
      ""MaxValue"": ""9999""
    },
    {
      ""Key"": ""Speed"",
      ""DisplayName"": ""速度"",
      ""Type"": ""Float"",
      ""InitialValue"": ""1.5"",
      ""MinValue"": ""0"",
      ""MaxValue"": ""2.5""
    },
    {
      ""Key"": ""OutingUnlocked"",
      ""DisplayName"": ""外出解鎖"",
      ""Type"": ""Bool"",
      ""InitialValue"": ""false""
    },
    {
      ""Key"": ""PlayerName"",
      ""DisplayName"": ""玩家名稱"",
      ""Type"": ""String"",
      ""InitialValue"": ""Mia""
    }
  ]
}";
            ParameterTableJsonCodec codec = new ParameterTableJsonCodec();

            ParameterTable first = codec.Read(json);
            ParameterTable second = codec.Read(codec.Write(first));

            Assert.That(second.TableGuid, Is.EqualTo("28a2f269-173a-48d2-a8db-9cd6832ee2f3"));
            Assert.That(second.DisplayName, Is.EqualTo("Core Gameplay"));
            Assert.That(second.Definitions, Has.Count.EqualTo(4));
            Assert.That(second.Definitions.Single(x => x.Key == "Supplies").InitialValue.AsInt(), Is.EqualTo(60));
            Assert.That(second.Definitions.Single(x => x.Key == "Speed").InitialValue.AsFloat(), Is.EqualTo(1.5f));
            Assert.That(second.Definitions.Single(x => x.Key == "OutingUnlocked").InitialValue.AsBool(), Is.False);
            Assert.That(second.Definitions.Single(x => x.Key == "PlayerName").InitialValue.AsString(), Is.EqualTo("Mia"));
        }

        [Test]
        public void Read_RejectsDuplicateKeysWithinTable()
        {
            const string json = @"{
  ""SchemaVersion"": 1,
  ""TableGuid"": ""759c5425-8e06-4247-bb62-26daf7c38d31"",
  ""DisplayName"": ""Duplicate Table"",
  ""Parameters"": [
    { ""Key"": ""Supplies"", ""DisplayName"": ""物資"", ""Type"": ""Int"", ""InitialValue"": ""1"", ""MinValue"": ""0"", ""MaxValue"": ""9"" },
    { ""Key"": ""Supplies"", ""DisplayName"": ""另一個物資"", ""Type"": ""Int"", ""InitialValue"": ""2"", ""MinValue"": ""0"", ""MaxValue"": ""9"" }
  ]
}";

            Assert.Throws<InvalidParameterTableException>(
                () => new ParameterTableJsonCodec().Read(json));
        }

        [Test]
        public void Read_RejectsLegacySingleParameterDocument()
        {
            const string legacyJson = @"{
  ""SchemaVersion"": 1,
  ""DocumentGuid"": ""759c5425-8e06-4247-bb62-26daf7c38d31"",
  ""Key"": ""OutingUnlocked"",
  ""DisplayName"": ""外出解鎖"",
  ""Type"": ""Bool"",
  ""InitialValue"": ""false""
}";

            Assert.Throws<InvalidParameterTableException>(
                () => new ParameterTableJsonCodec().Read(legacyJson));
        }
    }
}
