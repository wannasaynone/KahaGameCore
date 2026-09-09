using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KahaGameCore.Persistence.Tests
{
    public sealed class SaveableObjectRegistryTests
    {
        private SaveableObjectRegistry registry;

        [SetUp]
        public void SetUp()
        {
            SaveableObjects.Reset(new SaveableObjectRegistry());
            registry = SaveableObjects.Registry;
        }

        [TearDown]
        public void TearDown()
        {
            SaveableObjects.Reset(new SaveableObjectRegistry());
        }

        /// <summary>
        /// Awake runs on activation, so a runtime-built saveable has to be
        /// configured while the object is still inactive.
        /// </summary>
        private static SaveableObject CreateSaveable(
            string resourcePath,
            Vector3 position)
        {
            GameObject host = new GameObject("Saveable");
            host.SetActive(false);
            host.transform.position = position;
            SaveableObject saveable = host.AddComponent<SaveableObject>();
            saveable.Configure(resourcePath);
            host.SetActive(true);
            return saveable;
        }

        [Test]
        public void Attach_RecordsWhatAndWhere()
        {
            SaveableObject corpse = CreateSaveable(
                "Prefabs/Corpse",
                new Vector3(12.5f, -4f, 1f));

            SaveableObjectRecord[] captured = registry.Capture();

            Assert.That(captured, Has.Length.EqualTo(1));
            Assert.That(captured[0].Id, Is.EqualTo(corpse.Id));
            Assert.That(captured[0].ResourcePath, Is.EqualTo("Prefabs/Corpse"));
            Assert.That(captured[0].X, Is.EqualTo(12.5f));
            Assert.That(captured[0].Y, Is.EqualTo(-4f));
            Assert.That(captured[0].Z, Is.EqualTo(1f));

            Object.DestroyImmediate(corpse.gameObject);
        }

        [Test]
        public void Destroying_KeepsRecord_SoSceneUnloadDoesNotEraseTheObject()
        {
            SaveableObject corpse = CreateSaveable("Prefabs/Corpse", Vector3.zero);
            string id = corpse.Id;

            Object.DestroyImmediate(corpse.gameObject);

            Assert.That(registry.Contains(id), Is.True);
            Assert.That(registry.Capture(), Has.Length.EqualTo(1));
        }

        [Test]
        public void Destroying_KeepsTheLastPosition()
        {
            SaveableObject bomb = CreateSaveable("Prefabs/Bomb", Vector3.zero);
            bomb.transform.position = new Vector3(3f, 7f, 0f);

            Object.DestroyImmediate(bomb.gameObject);

            SaveableObjectRecord record = registry.Capture().Single();
            Assert.That(record.X, Is.EqualTo(3f));
            Assert.That(record.Y, Is.EqualTo(7f));
        }

        [Test]
        public void Discard_RemovesRecordPermanently()
        {
            SaveableObject bomb = CreateSaveable("Prefabs/Bomb", Vector3.zero);
            string id = bomb.Id;

            bomb.Discard();
            Object.DestroyImmediate(bomb.gameObject);

            Assert.That(registry.Contains(id), Is.False);
            Assert.That(registry.Capture(), Is.Empty);

        }

        [Test]
        public void Capture_ReadsCurrentPositionOfLiveObject()
        {
            SaveableObject bomb = CreateSaveable("Prefabs/Bomb", Vector3.zero);

            bomb.transform.position = new Vector3(-8f, 2.5f, 0f);
            SaveableObjectRecord record = registry.Capture().Single();

            Assert.That(record.X, Is.EqualTo(-8f));
            Assert.That(record.Y, Is.EqualTo(2.5f));

            Object.DestroyImmediate(bomb.gameObject);
        }

        [Test]
        public void Restore_DropsObjectsSpawnedAfterTheSaveWasWritten()
        {
            SaveableObject saved = CreateSaveable("Prefabs/Corpse", Vector3.zero);
            SaveableObjectRecord[] savePoint = registry.Capture();
            SaveableObject afterSave = CreateSaveable("Prefabs/Bomb", Vector3.one);

            Assert.That(registry.Capture(), Has.Length.EqualTo(2));

            registry.Restore(savePoint);

            Assert.That(registry.Capture(), Has.Length.EqualTo(1));
            Assert.That(registry.Contains(saved.Id), Is.True);
            Assert.That(registry.Contains(afterSave.Id), Is.False);

            Object.DestroyImmediate(saved.gameObject);
            Object.DestroyImmediate(afterSave.gameObject);
        }

        [TestCase(null, "Prefabs/Corpse")]
        [TestCase("", "Prefabs/Corpse")]
        [TestCase("id-1", null)]
        [TestCase("id-1", "   ")]
        public void Restore_SkipsUnusableRecordsInsteadOfThrowing(
            string id,
            string resourcePath)
        {
            LogAssert.Expect(
                LogType.Warning,
                "[SaveableObjectRegistry] Skipped a save record with no Id or Resources path.");

            registry.Restore(new[]
            {
                new SaveableObjectRecord { Id = id, ResourcePath = resourcePath }
            });

            Assert.That(registry.Capture(), Is.Empty);
        }
    }
}
