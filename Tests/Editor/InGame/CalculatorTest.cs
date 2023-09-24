using NUnit.Framework;
using KahaGameCore.Combat;
using System;

namespace KahaGameCore.Tests
{
    public class CalculatorTest 
    {
        private class TestCombatUnit : IActor
        {
            private class TestValueContainer : IValueContainer
            {
                public void AddBase(string tag, int value)
                {
                }

                public Guid Add(string tag, int value)
                {
                    return Guid.Empty;
                }

                public int GetTotal(string tag, bool baseOnly)
                {
                    if (tag == "Attack")
                        return 100;
                    else
                        return 0;
                }

                public void SetBase(string tag, int value)
                {
                }

                public void SetTemp(Guid guid, int value)
                {
                }

                public void AddToTemp(Guid guid, int value)
                {
                }
            }

            public IValueContainer Stats { get; private set; } = new TestValueContainer();
            public ISkillTrigger SkillTrigger => throw new NotImplementedException();
        }

        [Test]
        public void Calculate()
        {
            TestCombatUnit caster = new TestCombatUnit();
            TestCombatUnit target = new TestCombatUnit();

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
            TestCombatUnit caster = new TestCombatUnit();
            TestCombatUnit target = new TestCombatUnit();

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