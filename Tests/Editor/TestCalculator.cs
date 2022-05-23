using NUnit.Framework;
using KahaGameCore.Combat;

namespace KahaGameCore.Tests
{
    public class TestCalculator 
    {
        [Test]
        public void CalculatorTest()
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
        public void CalculatorTypoTest()
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
        public void CalculatorRandomTest()
        {
            float result = Calculator.Calculate(new Calculator.CalculateData
            {
                formula = "Random(1, 2)"
            });

            Assert.IsTrue(result - 1f <= 0.0001f);
        }

        [Test]
        public void CalculatorCommandTypoTest()
        {
            float result = Calculator.Calculate(new Calculator.CalculateData
            {
                formula = "Rando(1, 2)"
            });

            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "[CombatUtility][GetValueByCommand] Invaild command=Rando");
        }

        [Test]
        public void CalculatorRememberTest()
        {
            Calculator.Remember("Test", 100f);
            float result = Calculator.Calculate(new Calculator.CalculateData
            {
                formula = "Read(Test)"
            });

            Assert.IsTrue(result - 100f <= 0.0001f);
        }

        [Test]
        public void CalculatorDoubleRememberTest()
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