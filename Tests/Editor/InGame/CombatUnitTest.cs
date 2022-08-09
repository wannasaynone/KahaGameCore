using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public class CombatUnitTest
    {
        [Test]
        public void add_stats_and_get_total_value()
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
        public void remove_stats()
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