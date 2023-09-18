using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public class CombatUnitTest
    {
        [Test]
        public void Add_stats_and_get_total_value()
        {
            Combat.CombatUnit combatUnit = new Combat.CombatUnit(new Combat.ValueObject[]
                {
                    new Combat.ValueObject("Attack", 100)
                });

            combatUnit.Add(new Combat.ValueObject("Attack", 100));

            Assert.AreEqual(200, combatUnit.Stats.GetTotal("Attack", false));
            Assert.AreEqual(100, combatUnit.Stats.GetTotal("Attack", true));
        }

        [Test]
        public void Remove_stats()
        {
            Combat.CombatUnit combatUnit = new Combat.CombatUnit(new Combat.ValueObject[]
                {
                    new Combat.ValueObject("Attack", 100)
                });
            Combat.ValueObject temp = new Combat.ValueObject("Attack", 100);
            combatUnit.Add(temp);

            Assert.AreEqual(200, combatUnit.Stats.GetTotal("Attack", false));
            combatUnit.Remove(temp);
            Assert.AreEqual(100, combatUnit.Stats.GetTotal("Attack", false));
        }
    }
}