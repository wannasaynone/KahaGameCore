using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace KahaGameCore.Parameters.Tests
{
    public sealed class ParameterSnapshotDocumentCodecTests
    {
        [Test]
        public void EncodeDecode_RoundTripsAllTypesAndOrdersValuesByKey()
        {
            ParameterSnapshot snapshot = new ParameterSnapshot(
                ParameterSnapshot.CurrentSchemaVersion,
                new Dictionary<string, ParameterValue>
                {
                    ["StringValue"] = ParameterValue.FromString("001"),
                    ["BoolValue"] = ParameterValue.FromBool(true),
                    ["IntValue"] = ParameterValue.FromInt(-12),
                    ["FloatValue"] = ParameterValue.FromFloat(1.25f)
                });
            ParameterSnapshotDocumentCodec codec =
                new ParameterSnapshotDocumentCodec();

            ParameterSnapshotDocument document = codec.Encode(snapshot);

            Assert.That(document.SchemaVersion, Is.EqualTo(1));
            Assert.That(
                document.Values.Select(value => value.Key),
                Is.EqualTo(new[]
                {
                    "BoolValue",
                    "FloatValue",
                    "IntValue",
                    "StringValue"
                }));
            Assert.That(document.Values[0].Type, Is.EqualTo("Bool"));
            Assert.That(document.Values[0].Value, Is.EqualTo("true"));
            Assert.That(document.Values[1].Type, Is.EqualTo("Float"));
            Assert.That(document.Values[1].Value, Is.EqualTo("1.25"));
            Assert.That(document.Values[2].Type, Is.EqualTo("Int"));
            Assert.That(document.Values[2].Value, Is.EqualTo("-12"));
            Assert.That(document.Values[3].Type, Is.EqualTo("String"));
            Assert.That(document.Values[3].Value, Is.EqualTo("001"));

            ParameterSnapshot decoded = codec.Decode(document);

            AssertSnapshotValue(decoded, "BoolValue", ParameterValue.FromBool(true));
            AssertSnapshotValue(decoded, "FloatValue", ParameterValue.FromFloat(1.25f));
            AssertSnapshotValue(decoded, "IntValue", ParameterValue.FromInt(-12));
            AssertSnapshotValue(decoded, "StringValue", ParameterValue.FromString("001"));
        }

        [Test]
        public void Decode_RejectsUnsupportedSchemaVersion()
        {
            ParameterSnapshotDocument document = new ParameterSnapshotDocument
            {
                SchemaVersion = ParameterSnapshot.CurrentSchemaVersion + 1,
                Values = new ParameterSnapshotValueDocument[0]
            };
            ParameterSnapshotDocumentCodec codec =
                new ParameterSnapshotDocumentCodec();

            Assert.Throws<ParameterSnapshotException>(() => codec.Decode(document));
        }

        [Test]
        public void Decode_RejectsDuplicateKeysAsSnapshotError()
        {
            ParameterSnapshotDocument document = new ParameterSnapshotDocument
            {
                SchemaVersion = ParameterSnapshot.CurrentSchemaVersion,
                Values = new[]
                {
                    new ParameterSnapshotValueDocument
                    {
                        Key = "Score",
                        Type = "Int",
                        Value = "1"
                    },
                    new ParameterSnapshotValueDocument
                    {
                        Key = "Score",
                        Type = "Int",
                        Value = "2"
                    }
                }
            };
            ParameterSnapshotDocumentCodec codec =
                new ParameterSnapshotDocumentCodec();

            Assert.Throws<ParameterSnapshotException>(() => codec.Decode(document));
        }

        [Test]
        public void Decode_RejectsMissingValuesCollection()
        {
            ParameterSnapshotDocument document = new ParameterSnapshotDocument
            {
                SchemaVersion = ParameterSnapshot.CurrentSchemaVersion,
                Values = null
            };

            Assert.Throws<ParameterSnapshotException>(
                () => new ParameterSnapshotDocumentCodec().Decode(document));
        }

        [Test]
        public void Decode_RejectsNullValueEntry()
        {
            ParameterSnapshotDocument document = new ParameterSnapshotDocument
            {
                SchemaVersion = ParameterSnapshot.CurrentSchemaVersion,
                Values = new ParameterSnapshotValueDocument[] { null }
            };

            Assert.Throws<ParameterSnapshotException>(
                () => new ParameterSnapshotDocumentCodec().Decode(document));
        }

        [TestCase(null, "Int", "1")]
        [TestCase("", "Int", "1")]
        [TestCase(" ", "Int", "1")]
        [TestCase("Score", null, "1")]
        [TestCase("Score", "Int", null)]
        public void Decode_RejectsMissingValueFields(
            string key,
            string type,
            string value)
        {
            ParameterSnapshotDocument document = new ParameterSnapshotDocument
            {
                SchemaVersion = ParameterSnapshot.CurrentSchemaVersion,
                Values = new[]
                {
                    new ParameterSnapshotValueDocument
                    {
                        Key = key,
                        Type = type,
                        Value = value
                    }
                }
            };

            Assert.Throws<ParameterSnapshotException>(
                () => new ParameterSnapshotDocumentCodec().Decode(document));
        }

        [Test]
        public void Decode_RejectsUnknownType()
        {
            Assert.Throws<ParameterSnapshotException>(
                () => new ParameterSnapshotDocumentCodec().Decode(
                    CreateDocument("Score", "Number", "1")));
        }

        [TestCase("Int", "1.5")]
        [TestCase("Float", "abc")]
        [TestCase("Bool", "yes")]
        public void Decode_RejectsInvalidTypedValueAsSnapshotError(
            string type,
            string value)
        {
            Assert.Throws<ParameterSnapshotException>(
                () => new ParameterSnapshotDocumentCodec().Decode(
                    CreateDocument("Score", type, value)));
        }

        [Test]
        public void Encode_RejectsUnsupportedSchemaVersion()
        {
            ParameterSnapshot snapshot = new ParameterSnapshot(
                ParameterSnapshot.CurrentSchemaVersion + 1,
                new Dictionary<string, ParameterValue>());

            Assert.Throws<ParameterSnapshotException>(
                () => new ParameterSnapshotDocumentCodec().Encode(snapshot));
        }

        [Test]
        public void Encode_RejectsBlankKey()
        {
            ParameterSnapshot snapshot = new ParameterSnapshot(
                ParameterSnapshot.CurrentSchemaVersion,
                new Dictionary<string, ParameterValue>
                {
                    [""] = ParameterValue.FromInt(1)
                });

            Assert.Throws<ParameterSnapshotException>(
                () => new ParameterSnapshotDocumentCodec().Encode(snapshot));
        }

        [Test]
        public void Encode_RejectsNullStringValue()
        {
            ParameterSnapshot snapshot = new ParameterSnapshot(
                ParameterSnapshot.CurrentSchemaVersion,
                new Dictionary<string, ParameterValue>
                {
                    ["Name"] = ParameterValue.FromString(null)
                });

            Assert.Throws<ParameterSnapshotException>(
                () => new ParameterSnapshotDocumentCodec().Encode(snapshot));
        }

        private static ParameterSnapshotDocument CreateDocument(
            string key,
            string type,
            string value)
        {
            return new ParameterSnapshotDocument
            {
                SchemaVersion = ParameterSnapshot.CurrentSchemaVersion,
                Values = new[]
                {
                    new ParameterSnapshotValueDocument
                    {
                        Key = key,
                        Type = type,
                        Value = value
                    }
                }
            };
        }

        private static void AssertSnapshotValue(
            ParameterSnapshot snapshot,
            string key,
            ParameterValue expected)
        {
            Assert.That(snapshot.TryGetValue(key, out ParameterValue actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
