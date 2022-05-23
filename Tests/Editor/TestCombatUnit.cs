using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public class TestCombatUnit
    {
        [Test]
        public void GetTotalTest()
        {
            Combat.CombatUnit combatUnit = new Combat.CombatUnit(new Combat.ValueObject[]
                {
                    new Combat.ValueObject("Attack", 100)
                });

            combatUnit.Add(new Combat.ValueObject("Attack", 100));

            Assert.AreEqual(200, combatUnit.GetTotal("Attack"));
            Assert.AreEqual(100, combatUnit.GetTotal("Attack", true));
        }

        [Test]
        public void RemoveStatsTest()
        {
            Combat.CombatUnit combatUnit = new Combat.CombatUnit(new Combat.ValueObject[]
                {
                    new Combat.ValueObject("Attack", 100)
                });
            Combat.ValueObject temp = new Combat.ValueObject("Attack", 100);
            combatUnit.Add(temp);

            Assert.AreEqual(200, combatUnit.GetTotal("Attack"));
            combatUnit.Remove(temp);
            Assert.AreEqual(100, combatUnit.GetTotal("Attack"));
        }
    }
}