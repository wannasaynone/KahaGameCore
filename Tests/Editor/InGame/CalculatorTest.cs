using NUnit.Framework;
using KahaGameCore.Combat;

namespace KahaGameCore.Tests
{
    public class CalculatorTest 
    {
        [Test]
        public void Calculate()
        {
            CombatUnit caster = new CombatUnit(new ValueObject[]
            {
                new ValueObject("Attack", 100)
            });

            CombatUnit target = new CombatUnit(new ValueObject[]
            {
                new ValueObject("Attack", 100)
            });

            float result = Calculator.Calculate(new Calculator.CalculateData
            {
                caster = caster,
                target = target,
                formula = "Caster.Attack - Target.Attack",
                useBaseValue = false
            });

            Assert.IsTrue(result - 0f <= 0.0001f);
        }

        [Test]
        public void Calculate_if_typo()
        {
            CombatUnit caster = new CombatUnit(new ValueObject[]
            {
                new ValueObject("Attack", 100)
            });

            CombatUnit target = new CombatUnit(new ValueObject[]
            {
                new ValueObject("Attack", 100)
            });

            float result = Calculator.Calculate(new Calculator.CalculateData
            {
                caster = caster,
                target = target,
                formula = "Caster.Attac - Target.Attack",
                useBaseValue = false
            });

            Assert.IsTrue(result <= -100f);
        }

        [Test]
        public void Special_command_random()
        {
            float result = Calculator.Calculate(new Calculator.CalculateData
            {
                formula = "Random(1, 2)"
            });

            Assert.IsTrue(result - 1f <= 0.0001f);
        }

        [Test]
        public void Spciaial_command_random_when_typo()
        {
            float result = Calculator.Calculate(new Calculator.CalculateData
            {
                formula = "Rando(1, 2)"
            });

            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "[CombatUtility][GetValueByCommand] Invaild command=Rando");
        }

        [Test]
        public void Special_command_remember_read()
        {
            Calculator.Remember("Test", 100f);
            float result = Calculator.Calculate(new Calculator.CalculateData
            {
                formula = "Read(Test)"
            });

            Assert.IsTrue(result - 100f <= 0.0001f);
        }

        [Test]
        public void Special_command_remember_twice_then_read()
        {
            Calculator.Remember("Test", 0f);
            Calculator.Remember("Test", 100f);
            float result = Calculator.Calculate(new Calculator.CalculateData
            {
                formula = "Read(Test)"
            });

            Assert.IsTrue(result - 100f <= 0.0001f);
        }
    }
}