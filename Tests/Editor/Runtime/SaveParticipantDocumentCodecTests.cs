using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.Persistence;
using KahaGameCore.Serialization;
using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public sealed class SaveParticipantDocumentCodecTests
    {
        [Test]
        public void EncodeDecode_RoundTripsDifferentSnapshotTypesAndOrdersBySaveKey()
        {
            SaveParticipantRegistry source = new SaveParticipantRegistry();
            source.Register(new NumberParticipant("Zulu", 12));
            source.Register(new TextParticipant("Alpha", "ready"));
            SaveParticipantDocumentCodec codec =
                new SaveParticipantDocumentCodec();

            SaveParticipantDocument[] encoded = codec.Encode(source.Capture());

            Assert.That(
                encoded.Select(document => document.SaveKey),
                Is.EqualTo(new[] { "Alpha", "Zulu" }));
            string json = new GameStaticDataSerializer().Write(encoded);
            Assert.That(json, Does.Not.Contain(nameof(NumberSnapshot)));
            Assert.That(json, Does.Not.Contain(nameof(TextSnapshot)));
            SaveParticipantDocument[] documents =
                new GameStaticDataDeserializer()
                    .Read<SaveParticipantDocument[]>(json);
            Assert.That(documents[0].Snapshot, Is.Not.TypeOf<string>());
            NumberParticipant number = new NumberParticipant("Zulu", 0);
            TextParticipant text = new TextParticipant("Alpha", "changed");
            SaveParticipantRegistry target = new SaveParticipantRegistry();
            target.Register(number);
            target.Register(text);

            SaveParticipantSnapshotSet decoded = codec.Decode(documents, target);
            target.Restore(decoded);

            Assert.That(number.Value, Is.EqualTo(12));
            Assert.That(text.Value, Is.EqualTo("ready"));
        }

        [Test]
        public void Decode_RejectsDuplicateSaveKeyAsPersistenceError()
        {
            SaveParticipantRegistry registry = new SaveParticipantRegistry();
            registry.Register(new NumberParticipant("Player", 0));
            SaveParticipantDocument[] documents =
            {
                new SaveParticipantDocument
                {
                    SaveKey = "Player",
                    Snapshot = new NumberSnapshot { Value = 1 }
                },
                new SaveParticipantDocument
                {
                    SaveKey = "Player",
                    Snapshot = new NumberSnapshot { Value = 2 }
                }
            };

            Assert.Throws<InvalidOperationException>(
                () => new SaveParticipantDocumentCodec().Decode(
                    documents,
                    registry));
        }

        [Test]
        public void Decode_RejectsNullDocumentEntry()
        {
            SaveParticipantRegistry registry = new SaveParticipantRegistry();
            registry.Register(new NumberParticipant("Player", 0));

            Assert.Throws<InvalidOperationException>(
                () => new SaveParticipantDocumentCodec().Decode(
                    new SaveParticipantDocument[] { null },
                    registry));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Decode_RejectsMissingSaveKey(string saveKey)
        {
            SaveParticipantRegistry registry = new SaveParticipantRegistry();
            registry.Register(new NumberParticipant("Player", 0));
            SaveParticipantDocument document = new SaveParticipantDocument
            {
                SaveKey = saveKey,
                Snapshot = new NumberSnapshot { Value = 1 }
            };

            Assert.Throws<InvalidOperationException>(
                () => new SaveParticipantDocumentCodec().Decode(
                    new[] { document },
                    registry));
        }

        [Test]
        public void Decode_RejectsMissingSnapshot()
        {
            SaveParticipantRegistry registry = new SaveParticipantRegistry();
            registry.Register(new NumberParticipant("Player", 0));
            SaveParticipantDocument document = new SaveParticipantDocument
            {
                SaveKey = "Player",
                Snapshot = null
            };

            Assert.Throws<InvalidOperationException>(
                () => new SaveParticipantDocumentCodec().Decode(
                    new[] { document },
                    registry));
        }

        [Test]
        public void Decode_RejectsUnknownSaveKey()
        {
            SaveParticipantRegistry registry = new SaveParticipantRegistry();
            registry.Register(new NumberParticipant("Player", 0));
            SaveParticipantDocument document = new SaveParticipantDocument
            {
                SaveKey = "Unknown",
                Snapshot = new NumberSnapshot { Value = 1 }
            };

            Assert.Throws<InvalidOperationException>(
                () => new SaveParticipantDocumentCodec().Decode(
                    new[] { document },
                    registry));
        }

        [Test]
        public void Decode_RejectsMissingRegisteredSaveKey()
        {
            SaveParticipantRegistry registry = new SaveParticipantRegistry();
            registry.Register(new NumberParticipant("Player", 0));
            registry.Register(new TextParticipant("Inventory", "empty"));
            SaveParticipantDocument document = new SaveParticipantDocument
            {
                SaveKey = "Player",
                Snapshot = new NumberSnapshot { Value = 1 }
            };

            Assert.Throws<InvalidOperationException>(
                () => new SaveParticipantDocumentCodec().Decode(
                    new[] { document },
                    registry));
        }

        [Test]
        public void Restore_UsesRegistrationOrderInsteadOfDocumentOrder()
        {
            List<string> restoredKeys = new List<string>();
            SaveParticipantRegistry registry = new SaveParticipantRegistry();
            registry.Register(new OrderedParticipant("Zulu", restoredKeys));
            registry.Register(new OrderedParticipant("Alpha", restoredKeys));
            SaveParticipantDocument[] documents =
            {
                new SaveParticipantDocument
                {
                    SaveKey = "Alpha",
                    Snapshot = new NumberSnapshot { Value = 1 }
                },
                new SaveParticipantDocument
                {
                    SaveKey = "Zulu",
                    Snapshot = new NumberSnapshot { Value = 2 }
                }
            };
            SaveParticipantDocumentCodec codec =
                new SaveParticipantDocumentCodec();

            registry.Restore(codec.Decode(documents, registry));

            Assert.That(restoredKeys, Is.EqualTo(new[] { "Zulu", "Alpha" }));
        }

        [Test]
        public void Encode_RejectsMissingParticipantSnapshot()
        {
            SaveParticipantRegistry registry = new SaveParticipantRegistry();
            registry.Register(new NullSnapshotParticipant());

            Assert.Throws<InvalidOperationException>(
                () => new SaveParticipantDocumentCodec().Encode(
                    registry.Capture()));
        }

        public sealed class NumberSnapshot
        {
            public int Value;
        }

        public sealed class TextSnapshot
        {
            public string Value;
        }

        private sealed class NumberParticipant : ISaveParticipant<NumberSnapshot>
        {
            public NumberParticipant(string saveKey, int value)
            {
                SaveKey = saveKey;
                Value = value;
            }

            public string SaveKey { get; }
            public int Value { get; set; }

            public NumberSnapshot Capture()
            {
                return new NumberSnapshot { Value = Value };
            }

            public void Restore(NumberSnapshot snapshot)
            {
                Value = snapshot.Value;
            }
        }

        private sealed class TextParticipant : ISaveParticipant<TextSnapshot>
        {
            public TextParticipant(string saveKey, string value)
            {
                SaveKey = saveKey;
                Value = value;
            }

            public string SaveKey { get; }
            public string Value { get; set; }

            public TextSnapshot Capture()
            {
                return new TextSnapshot { Value = Value };
            }

            public void Restore(TextSnapshot snapshot)
            {
                Value = snapshot.Value;
            }
        }

        private sealed class OrderedParticipant : ISaveParticipant<NumberSnapshot>
        {
            private readonly List<string> restoredKeys;

            public OrderedParticipant(
                string saveKey,
                List<string> restoredKeys)
            {
                SaveKey = saveKey;
                this.restoredKeys = restoredKeys;
            }

            public string SaveKey { get; }

            public NumberSnapshot Capture()
            {
                return new NumberSnapshot();
            }

            public void Restore(NumberSnapshot snapshot)
            {
                restoredKeys.Add(SaveKey);
            }
        }

        private sealed class NullSnapshotParticipant :
            ISaveParticipant<NumberSnapshot>
        {
            public string SaveKey => "Null";

            public NumberSnapshot Capture()
            {
                return null;
            }

            public void Restore(NumberSnapshot snapshot)
            {
            }
        }
    }
}
