using System;
using NUnit.Framework;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class GameEventDocumentTests
    {
        private static readonly Guid ValidDocumentGuid =
            new Guid("40000000-0000-0000-0000-000000000004");

        [Test]
        public void Constructor_EmptyDocumentGuidFailsExplicitly()
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                CreateDocument(documentGuid: Guid.Empty));

            Assert.That(exception.ParamName, Is.EqualTo("documentGuid"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Constructor_MissingDisplayNameFailsExplicitly(string displayName)
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                CreateDocument(displayName: displayName));

            Assert.That(exception.ParamName, Is.EqualTo("displayName"));
        }

        [Test]
        public void Constructor_NullTextFieldsFailExplicitly()
        {
            ArgumentNullException triggerTimingException = Assert.Throws<ArgumentNullException>(() =>
                CreateDocument(triggerTiming: null));
            ArgumentNullException conditionException = Assert.Throws<ArgumentNullException>(() =>
                CreateDocument(condition: null));
            ArgumentNullException commandsException = Assert.Throws<ArgumentNullException>(() =>
                CreateDocument(commands: null));

            Assert.That(triggerTimingException.ParamName, Is.EqualTo("triggerTiming"));
            Assert.That(conditionException.ParamName, Is.EqualTo("condition"));
            Assert.That(commandsException.ParamName, Is.EqualTo("commands"));
        }

        [Test]
        public void Constructor_EmptyTextFieldsRemainValid()
        {
            GameEventDocument document = CreateDocument(
                triggerTiming: string.Empty,
                condition: string.Empty,
                commands: string.Empty);

            Assert.That(document.TriggerTiming, Is.Empty);
            Assert.That(document.Condition, Is.Empty);
            Assert.That(document.Commands, Is.Empty);
        }

        private static GameEventDocument CreateDocument(
            Guid? documentGuid = null,
            string displayName = "Valid Document",
            string triggerTiming = "tick",
            string condition = "$Gate == 0",
            string commands = "Record(ok);")
        {
            return new GameEventDocument(
                GameEventDocumentJsonCodec.CurrentSchemaVersion,
                documentGuid ?? ValidDocumentGuid,
                displayName,
                triggerTiming,
                condition,
                commands);
        }
    }

    public sealed class GameEventDocumentJsonCodecTests
    {
        [Test]
        public void Read_VersionOneFailsExplicitly()
        {
            const string json = @"{
  ""SchemaVersion"": 1,
  ""DocumentGuid"": ""40000000-0000-0000-0000-000000000001"",
  ""DisplayName"": ""Old Schema"",
  ""TriggerTiming"": """",
  ""Condition"": """",
  ""Commands"": """"
}";

            GameEventException exception = Assert.Throws<GameEventException>(() =>
                new GameEventDocumentJsonCodec().Read(json));

            Assert.That(exception.Code, Is.EqualTo("UnsupportedSchemaVersion"));
        }

        [Test]
        public void Read_MissingTriggerTimingFailsExplicitly()
        {
            const string json = @"{
  ""SchemaVersion"": 2,
  ""DocumentGuid"": ""40000000-0000-0000-0000-000000000003"",
  ""DisplayName"": ""Missing Trigger Timing"",
  ""Condition"": """",
  ""Commands"": """"
}";

            GameEventException exception = Assert.Throws<GameEventException>(() =>
                new GameEventDocumentJsonCodec().Read(json));

            Assert.That(exception.Code, Is.EqualTo("MissingField"));
            StringAssert.Contains("TriggerTiming", exception.Message);
        }

        [Test]
        public void Read_NullTriggerTimingFailsExplicitly()
        {
            const string json = @"{
  ""SchemaVersion"": 2,
  ""DocumentGuid"": ""40000000-0000-0000-0000-000000000005"",
  ""DisplayName"": ""Null Trigger Timing"",
  ""TriggerTiming"": null,
  ""Condition"": """",
  ""Commands"": """"
}";

            GameEventException exception = Assert.Throws<GameEventException>(() =>
                new GameEventDocumentJsonCodec().Read(json));

            Assert.That(exception.Code, Is.EqualTo("MissingTriggerTiming"));
        }

        [Test]
        public void Write_UnsupportedSchemaVersionFailsExplicitly()
        {
            GameEventDocument document = new GameEventDocument(
                schemaVersion: 1,
                documentGuid: new Guid("40000000-0000-0000-0000-000000000006"),
                displayName: "Unsupported Schema",
                triggerTiming: string.Empty,
                condition: string.Empty,
                commands: string.Empty);

            GameEventException exception = Assert.Throws<GameEventException>(() =>
                new GameEventDocumentJsonCodec().Write(document));

            Assert.That(exception.Code, Is.EqualTo("UnsupportedSchemaVersion"));
        }

        [Test]
        public void ReadAndWrite_PreservesSchemaIdentityAndCommands()
        {
            const string json = @"{
  ""SchemaVersion"": 2,
  ""DocumentGuid"": ""40000000-0000-0000-0000-000000000002"",
  ""DisplayName"": ""Round Trip"",
  ""TriggerTiming"": ""tick"",
  ""Condition"": ""$Gate == 0"",
  ""Commands"": ""Record(ok);""
}";
            GameEventDocumentJsonCodec codec = new GameEventDocumentJsonCodec();

            GameEventDocument first = codec.Read(json);
            GameEventDocument second = codec.Read(codec.Write(first));

            Assert.That(second.SchemaVersion, Is.EqualTo(2));
            Assert.That(second.DocumentGuid, Is.EqualTo(first.DocumentGuid));
            Assert.That(second.TriggerTiming, Is.EqualTo("tick"));
            Assert.That(second.Commands, Is.EqualTo("Record(ok);"));
        }
    }
}
