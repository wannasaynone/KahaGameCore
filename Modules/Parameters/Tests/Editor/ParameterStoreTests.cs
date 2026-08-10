using System.Collections.Generic;
using NUnit.Framework;

namespace KahaGameCore.Parameters.Tests
{
    public class ParameterStoreTests
    {
        [Test]
        public void GetInt_ReturnsDefinitionInitialValue()
        {
            ParameterDefinition supplies = ParameterDefinition.Int(
                "Supplies",
                "物資",
                initialValue: 60,
                minValue: 0,
                maxValue: 9999);
            ParameterStore parameters = new ParameterStore(new[] { supplies });

            int value = parameters.GetInt("Supplies");

            Assert.That(value, Is.EqualTo(60));
        }

        [Test]
        public void Add_IntClampsAndPublishesActualChange()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Satiety", "飽腹", initialValue: 90, minValue: 0, maxValue: 100)
            });
            List<ParameterChanged> changes = new List<ParameterChanged>();
            parameters.Changed += changes.Add;

            parameters.Add("Satiety", 20);

            Assert.That(parameters.GetInt("Satiety"), Is.EqualTo(100));
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Key, Is.EqualTo("Satiety"));
            Assert.That(changes[0].OldValue.AsInt(), Is.EqualTo(90));
            Assert.That(changes[0].NewValue.AsInt(), Is.EqualTo(100));
        }

        [Test]
        public void TypedValues_RoundTripThroughTypedMethods()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Float("Speed", "速度", initialValue: 1.5f, minValue: 0f, maxValue: 2f),
                ParameterDefinition.Bool("OutingUnlocked", "外出解鎖", initialValue: false),
                ParameterDefinition.String("PlayerName", "玩家名稱", initialValue: "Mira")
            });

            parameters.Set("Speed", 3f);
            parameters.Set("OutingUnlocked", true);
            parameters.Set("PlayerName", "Nora");

            Assert.That(parameters.GetFloat("Speed"), Is.EqualTo(2f));
            Assert.That(parameters.GetBool("OutingUnlocked"), Is.True);
            Assert.That(parameters.GetString("PlayerName"), Is.EqualTo("Nora"));
            Assert.That(parameters.TryGetValue("OutingUnlocked", out ParameterValue value), Is.True);
            Assert.That(value.AsBool(), Is.True);
        }

        [Test]
        public void Get_UnknownKeyThrowsExplicitError()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Supplies", "物資", initialValue: 60, minValue: 0, maxValue: 9999)
            });

            UnknownParameterException error = Assert.Throws<UnknownParameterException>(
                () => parameters.GetInt("Missing"));

            Assert.That(error.Key, Is.EqualTo("Missing"));
        }

        [Test]
        public void Set_WrongTypeThrowsExplicitErrorWithoutChangingValue()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Bool("OutingUnlocked", "外出解鎖", initialValue: false)
            });

            ParameterTypeMismatchException error = Assert.Throws<ParameterTypeMismatchException>(
                () => parameters.Set("OutingUnlocked", 1));

            Assert.That(error.Key, Is.EqualTo("OutingUnlocked"));
            Assert.That(error.ExpectedType, Is.EqualTo(ParameterType.Bool));
            Assert.That(error.ActualType, Is.EqualTo(ParameterType.Int));
            Assert.That(parameters.GetBool("OutingUnlocked"), Is.False);
        }

        [Test]
        public void TypedGetter_WrongTypeReportsExpectedAndActualTypes()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Bool("OutingUnlocked", "外出解鎖", initialValue: false)
            });

            ParameterTypeMismatchException error = Assert.Throws<ParameterTypeMismatchException>(
                () => parameters.GetInt("OutingUnlocked"));

            Assert.That(error.ExpectedType, Is.EqualTo(ParameterType.Int));
            Assert.That(error.ActualType, Is.EqualTo(ParameterType.Bool));
            StringAssert.Contains("expects Int, but received Bool", error.Message);
        }

        [Test]
        public void Definition_RejectsInitialValueOutsideBounds()
        {
            InvalidParameterDefinitionException error = Assert.Throws<InvalidParameterDefinitionException>(
                () => ParameterDefinition.Int(
                    "Satiety",
                    "飽腹",
                    initialValue: 120,
                    minValue: 0,
                    maxValue: 100));

            Assert.That(error.Key, Is.EqualTo("Satiety"));
        }

        [Test]
        public void Store_RejectsDuplicateKeys()
        {
            ParameterDefinition first = ParameterDefinition.Int("Supplies", "物資", 10, 0, 100);
            ParameterDefinition duplicate = ParameterDefinition.Int("Supplies", "另一個物資", 20, 0, 100);

            InvalidParameterDefinitionException error = Assert.Throws<InvalidParameterDefinitionException>(
                () => new ParameterStore(new[] { first, duplicate }));

            Assert.That(error.Key, Is.EqualTo("Supplies"));
        }

        [Test]
        public void Restore_ResetsToInitialThenOverlaysSnapshotValues()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Supplies", "物資", initialValue: 60, minValue: 0, maxValue: 9999),
                ParameterDefinition.Bool("OutingUnlocked", "外出解鎖", initialValue: false)
            });
            parameters.Set("Supplies", 5);
            parameters.Set("OutingUnlocked", true);
            ParameterSnapshot snapshot = new ParameterSnapshot(
                ParameterSnapshot.CurrentSchemaVersion,
                new Dictionary<string, ParameterValue>
                {
                    ["Supplies"] = ParameterValue.FromInt(20)
                });

            parameters.Restore(snapshot);

            Assert.That(parameters.GetInt("Supplies"), Is.EqualTo(20));
            Assert.That(parameters.GetBool("OutingUnlocked"), Is.False);
        }
    }
}
