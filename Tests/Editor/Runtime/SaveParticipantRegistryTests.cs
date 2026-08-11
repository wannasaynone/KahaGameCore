using KahaGameCore.Persistence;
using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public sealed class SaveParticipantRegistryTests
    {
        [Test]
        public void CaptureAndRestore_SupportsDifferentTypedSnapshots()
        {
            PlayerParticipant player = new PlayerParticipant(position: 12);
            InventoryParticipant inventory = new InventoryParticipant(itemCount: 3);
            SaveParticipantRegistry registry = new SaveParticipantRegistry();
            registry.Register(player);
            registry.Register(inventory);

            SaveParticipantSnapshotSet snapshot = registry.Capture();
            player.Position = 99;
            inventory.ItemCount = 0;

            registry.Restore(snapshot);

            Assert.That(player.Position, Is.EqualTo(12));
            Assert.That(inventory.ItemCount, Is.EqualTo(3));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Register_RejectsMissingSaveKey(string saveKey)
        {
            SaveParticipantRegistry registry = new SaveParticipantRegistry();
            PlayerParticipant participant =
                new PlayerParticipant(position: 12, saveKey: saveKey);

            Assert.Throws<System.ArgumentException>(() => registry.Register(participant));
        }

        [Test]
        public void Register_RejectsDuplicateSaveKey()
        {
            SaveParticipantRegistry registry = new SaveParticipantRegistry();
            registry.Register(new PlayerParticipant(position: 12));

            Assert.Throws<System.InvalidOperationException>(() =>
                registry.Register(new PlayerParticipant(position: 99)));
        }

        [Test]
        public void Restore_MissingRegisteredKeyFailsBeforeAnyParticipantChanges()
        {
            SaveParticipantRegistry source = new SaveParticipantRegistry();
            source.Register(new PlayerParticipant(position: 12));
            SaveParticipantSnapshotSet snapshot = source.Capture();

            PlayerParticipant player = new PlayerParticipant(position: 99);
            InventoryParticipant inventory = new InventoryParticipant(itemCount: 9);
            SaveParticipantRegistry target = new SaveParticipantRegistry();
            target.Register(player);
            target.Register(inventory);

            Assert.Throws<System.InvalidOperationException>(() => target.Restore(snapshot));
            Assert.That(player.Position, Is.EqualTo(99));
            Assert.That(inventory.ItemCount, Is.EqualTo(9));
        }

        [Test]
        public void Restore_UnknownSnapshotKeyFailsBeforeAnyParticipantChanges()
        {
            SaveParticipantRegistry source = new SaveParticipantRegistry();
            source.Register(new PlayerParticipant(position: 12));
            source.Register(new InventoryParticipant(itemCount: 3));
            SaveParticipantSnapshotSet snapshot = source.Capture();

            PlayerParticipant player = new PlayerParticipant(position: 99);
            SaveParticipantRegistry target = new SaveParticipantRegistry();
            target.Register(player);

            Assert.Throws<System.InvalidOperationException>(() => target.Restore(snapshot));
            Assert.That(player.Position, Is.EqualTo(99));
        }

        [Test]
        public void Restore_SnapshotTypeMismatchFailsBeforeAnyParticipantChanges()
        {
            SaveParticipantRegistry source = new SaveParticipantRegistry();
            source.Register(new PlayerParticipant(position: 12));
            source.Register(new InventoryParticipant(itemCount: 3));
            SaveParticipantSnapshotSet snapshot = source.Capture();

            PlayerParticipant player = new PlayerParticipant(position: 99);
            PlayerParticipant wrongInventoryType =
                new PlayerParticipant(position: 88, saveKey: "Inventory");
            SaveParticipantRegistry target = new SaveParticipantRegistry();
            target.Register(player);
            target.Register(wrongInventoryType);

            Assert.Throws<System.InvalidOperationException>(() => target.Restore(snapshot));
            Assert.That(player.Position, Is.EqualTo(99));
            Assert.That(wrongInventoryType.Position, Is.EqualTo(88));
        }

        private sealed class PlayerSnapshot
        {
            public PlayerSnapshot(int position)
            {
                Position = position;
            }

            public int Position { get; }
        }

        private sealed class PlayerParticipant : ISaveParticipant<PlayerSnapshot>
        {
            public PlayerParticipant(int position, string saveKey = "Player")
            {
                Position = position;
                SaveKey = saveKey;
            }

            public string SaveKey { get; }
            public int Position { get; set; }

            public PlayerSnapshot Capture()
            {
                return new PlayerSnapshot(Position);
            }

            public void Restore(PlayerSnapshot snapshot)
            {
                Position = snapshot.Position;
            }
        }

        private sealed class InventorySnapshot
        {
            public InventorySnapshot(int itemCount)
            {
                ItemCount = itemCount;
            }

            public int ItemCount { get; }
        }

        private sealed class InventoryParticipant : ISaveParticipant<InventorySnapshot>
        {
            public InventoryParticipant(int itemCount)
            {
                ItemCount = itemCount;
            }

            public string SaveKey => "Inventory";
            public int ItemCount { get; set; }

            public InventorySnapshot Capture()
            {
                return new InventorySnapshot(ItemCount);
            }

            public void Restore(InventorySnapshot snapshot)
            {
                ItemCount = snapshot.ItemCount;
            }
        }
    }
}
