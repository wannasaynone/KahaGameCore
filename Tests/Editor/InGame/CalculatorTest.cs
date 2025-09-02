using NUnit.Framework;
using NSubstitute;
using UnityEngine;
using UnityEngine.TestTools;
using KahaGameCore.ValueContainer;

namespace KahaGameCore.Tests
{
    public class CalculatorTest
    {
        private IValueContainer mockCaster;
        private IValueContainer mockTarget;
        private Calculator.CalculateData calculateData;

        [SetUp]
        public void SetUp()
        {
            // Create mock objects
            mockCaster = Substitute.For<IValueContainer>();
            mockTarget = Substitute.For<IValueContainer>();

            // Initialize calculate data
            calculateData = new Calculator.CalculateData
            {
                caster = mockCaster,
                target = mockTarget,
                formula = "",
                useBaseValue = false
            };
        }

        [TearDown]
        public void TearDown()
        {
            // Clear any remembered values to avoid test interference
            Calculator.Remember("TestValue", 0);
            Calculator.Remember("BaseValue", 0);
            Calculator.Remember("Multiplier", 0);
            Calculator.Remember("Min", 0);
            Calculator.Remember("Max", 0);
            Calculator.Remember("CritMultiplier", 0);
        }

        #region Basic Calculation Tests

        [Test]
        public void Calculate_SimpleInteger_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "42";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(42f, result);
        }

        [Test]
        public void Calculate_SimpleFloat_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "3.14";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(3.14f, result, 0.001f);
        }

        [Test]
        public void Calculate_NegativeNumber_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "-25";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(-25f, result);
        }

        #endregion

        #region Arithmetic Operation Tests

        [Test]
        public void Calculate_Addition_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "10 + 5";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(15f, result);
        }

        [Test]
        public void Calculate_Subtraction_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "10 - 5";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(5f, result);
        }

        [Test]
        public void Calculate_Multiplication_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "10 * 5";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(50f, result);
        }

        [Test]
        public void Calculate_Division_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "10 / 5";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(2f, result);
        }

        [Test]
        public void Calculate_MultipleOperations_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "10 + 5 * 2";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(20f, result);
        }

        [Test]
        public void Calculate_ComplexExpression_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "10 + 5 * 2 - 8 / 4";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(18f, result);
        }

        [Test]
        public void Calculate_NegativeValuesInExpression_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "-10 + 5";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(-5f, result);
        }

        [Test]
        public void Calculate_NegativeValuesWithMultiplication_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "-10 * 2";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(-20f, result);
        }

        [Test]
        public void Calculate_NegativeValuesWithParentheses_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "(-10 + 5) * 2";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(-10f, result);
        }

        [Test]
        public void Calculate_MultipleNegativeValues_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "-10 + -5";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(-15f, result);
        }

        #endregion

        #region Parentheses Tests

        [Test]
        public void Calculate_SimpleParentheses_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "(10 + 5)";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(15f, result);
        }

        [Test]
        public void Calculate_ParenthesesPrecedence_ReturnsCorrectValue()
        {
            // Arrange - With parentheses: (10 + 5) * 2 = 15 * 2 = 30
            calculateData.formula = "(10 + 5) * 2";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(30f, result);
        }

        [Test]
        public void Calculate_NestedParentheses_ReturnsCorrectValue()
        {
            // Arrange - (5 + (3 * 2)) * 4 = (5 + 6) * 4 = 11 * 4 = 44
            calculateData.formula = "(5 + (3 * 2)) * 4";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(44f, result);
        }

        [Test]
        public void Calculate_MultipleParenthesesGroups_ReturnsCorrectValue()
        {
            // Arrange - (10 + 5) * (8 - 3) = 15 * 5 = 75
            calculateData.formula = "(10 + 5) * (8 - 3)";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(75f, result);
        }

        #endregion

        #region Value Container Tests

        [Test]
        public void Calculate_CasterProperty_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "Caster.Health";
            mockCaster.GetTotal("Health", false).Returns(100);

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(100f, result);
        }

        [Test]
        public void Calculate_TargetProperty_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "Target.Attack";
            mockTarget.GetTotal("Attack", false).Returns(75);

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(75f, result);
        }

        [Test]
        public void Calculate_NegativeProperty_ReturnsNegativeValue()
        {
            // Arrange
            calculateData.formula = "-Caster.Health";
            mockCaster.GetTotal("Health", false).Returns(100);

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(-100f, result);
        }

        [Test]
        public void Calculate_UseBaseValue_UsesBaseValueFlag()
        {
            // Arrange
            calculateData.formula = "Caster.Health";
            calculateData.useBaseValue = true;
            mockCaster.GetTotal("Health", true).Returns(80);

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(80f, result);
            mockCaster.Received(1).GetTotal("Health", true);
        }

        [Test]
        public void Calculate_ExpressionWithProperties_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "Caster.Health + Target.Attack";
            mockCaster.GetTotal("Health", false).Returns(60);
            mockTarget.GetTotal("Attack", false).Returns(40);

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(100f, result);
        }

        [Test]
        public void Calculate_ComplexExpressionWithProperties_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "(Caster.Health * 2) - (Target.Defense / 2)";
            mockCaster.GetTotal("Health", false).Returns(50);
            mockTarget.GetTotal("Defense", false).Returns(20);

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(90f, result); // (50 * 2) - (20 / 2) = 100 - 10 = 90
        }

        #endregion

        #region Random Function Tests

        [Test]
        public void Calculate_RandomFunction_ReturnsValueInRange()
        {
            // Arrange
            calculateData.formula = "Random(1,10)";

            // Act & Assert - Test multiple times to ensure random values are within range
            for (int i = 0; i < 10; i++)
            {
                float result = Calculator.Calculate(calculateData);
                Assert.GreaterOrEqual(result, 1f);
                Assert.LessOrEqual(result, 10f);
            }
        }

        [Test]
        public void Calculate_RandomWithSameMinMax_ReturnsConstantValue()
        {
            // Arrange
            calculateData.formula = "Random(5,5)";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(5f, result);
        }

        [Test]
        public void Calculate_RandomFunctionWithExpressions_ReturnsValueInRange()
        {
            // Arrange
            calculateData.formula = "Random(1+2, 8+2)";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.GreaterOrEqual(result, 3f); // 1+2 = 3
            Assert.LessOrEqual(result, 10f); // 8+2 = 10
        }

        [Test]
        public void Calculate_RandomFunctionWithVariables_ReturnsValueInRange()
        {
            // Arrange
            mockCaster.GetTotal("MinValue", false).Returns(1);
            mockCaster.GetTotal("MaxValue", false).Returns(10);
            calculateData.formula = "Random(Caster.MinValue, Caster.MaxValue)";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.GreaterOrEqual(result, 1f);
            Assert.LessOrEqual(result, 10f);
        }

        [Test]
        public void Calculate_NegativeRandom_ReturnsNegativeValue()
        {
            // Arrange
            calculateData.formula = "-Random(1,1)";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(-1f, result);
        }

        [Test]
        public void Calculate_RandomInExpression_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "Random(1,1) + 5";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(6f, result); // Random(1,1) always returns 1, so 1 + 5 = 6
        }

        #endregion

        #region Remember and Read Tests

        [Test]
        public void Remember_StoresValue_CanBeRetrieved()
        {
            // Arrange
            Calculator.Remember("TestValue", 42);
            calculateData.formula = "Read(TestValue)";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(42f, result);
        }

        [Test]
        public void Remember_OverwritesExistingValue_ReturnsUpdatedValue()
        {
            // Arrange
            Calculator.Remember("TestValue", 10);
            calculateData.formula = "Read(TestValue)";
            float result1 = Calculator.Calculate(calculateData);

            // Act - Overwrite the value
            Calculator.Remember("TestValue", 20);
            float result2 = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(10f, result1);
            Assert.AreEqual(20f, result2);
        }

        [Test]
        public void Calculate_ReadInExpression_ReturnsCorrectValue()
        {
            // Arrange
            Calculator.Remember("BaseValue", 10);
            calculateData.formula = "Read(BaseValue) * 2 + 5";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(25f, result); // 10 * 2 + 5 = 25
        }

        [Test]
        public void Calculate_MultipleRememberedValues_ReturnsCorrectValues()
        {
            // Arrange
            Calculator.Remember("Value1", 10);
            Calculator.Remember("Value2", 20);
            Calculator.Remember("Value3", 30);

            // Act
            calculateData.formula = "Read(Value1) + Read(Value2) + Read(Value3)";
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(60f, result); // 10 + 20 + 30 = 60
        }

        [Test]
        public void Calculate_ReadWithinRandom_ReturnsValueInRange()
        {
            // Arrange
            Calculator.Remember("Min", 1);
            Calculator.Remember("Max", 10);
            calculateData.formula = "Random(Read(Min), Read(Max))";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.GreaterOrEqual(result, 1f);
            Assert.LessOrEqual(result, 10f);
        }

        #endregion

        #region Whitespace and Formatting Tests

        [Test]
        public void Calculate_FormulaWithExtraWhitespace_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "  10   +    5  ";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(15f, result);
        }

        [Test]
        public void Calculate_FormulaWithoutWhitespace_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "10+5*2";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(20f, result);
        }

        [Test]
        public void Calculate_FormulaWithTabsAndNewlines_ReturnsCorrectValue()
        {
            // Arrange
            calculateData.formula = "10 +\t5\n* 2";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(20f, result);
        }

        #endregion

        #region Decimal Precision and Rounding Tests

        [Test]
        public void Calculate_DecimalPrecision_ReturnsRoundedValue()
        {
            // Arrange
            calculateData.formula = "10 / 3";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            // The Calculator implementation rounds to 2 decimal places
            Assert.AreEqual(3.33f, result, 0.01f);
        }

        [Test]
        public void Calculate_MultipleDecimalOperations_MaintainsPrecision()
        {
            // Arrange
            calculateData.formula = "(10 / 3) * 3";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            // Due to rounding, this might not be exactly 10
            Assert.AreEqual(10f, result, 0.1f);
        }

        [Test]
        public void Calculate_ComplexDecimalCalculation_HandlesRoundingCorrectly()
        {
            // Arrange
            calculateData.formula = "(10.5 + 20.3) * 0.5";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(15.4f, result, 0.1f);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Test]
        public void Calculate_EmptyFormula_ReturnsZero()
        {
            // Arrange
            calculateData.formula = "";

            // Act & Assert
            LogAssert.Expect(LogType.Error, "[Error] Formula is empty");
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Calculate_MissingClosingParenthesis_HandlesGracefully()
        {
            // Arrange
            calculateData.formula = "(10 + 5";

            // Act & Assert
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*"));
            float result = Calculator.Calculate(calculateData);

            // The exact behavior might depend on implementation, but it should not throw an exception
            Assert.That(result, Is.Not.NaN);
        }

        [Test]
        public void Calculate_DivisionByZero_HandlesGracefully()
        {
            // Arrange
            calculateData.formula = "10 / 0";

            // Act & Assert
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*"));
            float result = Calculator.Calculate(calculateData);

            // The exact behavior might depend on implementation, but it should not throw an exception
            Assert.That(result, Is.Not.NaN);
        }

        [Test]
        public void Calculate_InvalidCommand_HandlesGracefully()
        {
            // Arrange
            calculateData.formula = "InvalidCommand(10)";

            // Act & Assert
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*"));
            float result = Calculator.Calculate(calculateData);

            // The exact behavior might depend on implementation, but it should not throw an exception
            Assert.That(result, Is.Not.NaN);
        }

        [Test]
        public void Calculate_ReadNonExistentTag_HandlesGracefully()
        {
            // Arrange
            calculateData.formula = "Read(NonExistentTag)";

            // Act & Assert
            LogAssert.Expect(LogType.Error, "The given key 'NonExistentTag' was not present in the dictionary.");
            float result = Calculator.Calculate(calculateData);

            // Should return 0 for non-existent keys
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Calculate_InvalidValueContainer_HandlesGracefully()
        {
            // Arrange
            calculateData.caster = null;
            calculateData.formula = "Caster.Health";

            // Act & Assert
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*"));
            float result = Calculator.Calculate(calculateData);

            // The exact behavior might depend on implementation, but it should not throw an exception
            Assert.That(result, Is.Not.NaN);
        }

        [Test]
        public void Calculate_InvalidPropertyAccess_HandlesGracefully()
        {
            // Arrange
            calculateData.formula = "InvalidTarget.Property";

            // Act & Assert
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*"));
            float result = Calculator.Calculate(calculateData);

            // The exact behavior might depend on implementation, but it should not throw an exception
            Assert.That(result, Is.Not.NaN);
        }

        #endregion

        #region Command-Specific Tests

        [Test]
        public void Calculate_RandomCommandWithVariables_ReturnsValueInRange()
        {
            // Arrange
            mockCaster.GetTotal("MinValue", false).Returns(5);
            mockCaster.GetTotal("MaxValue", false).Returns(10);
            calculateData.formula = "Random(Caster.MinValue, Caster.MaxValue)";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.GreaterOrEqual(result, 5f);
            Assert.LessOrEqual(result, 10f);
        }

        [Test]
        public void Calculate_ReadCommandWithMultipleValues_ReturnsCorrectValues()
        {
            // Arrange
            Calculator.Remember("Value1", 10);
            Calculator.Remember("Value2", 20);
            calculateData.formula = "Read(Value1) + Read(Value2)";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(30f, result);
        }

        [Test]
        public void Calculate_ReadCommandInNestedExpression_ReturnsCorrectValue()
        {
            // Arrange
            Calculator.Remember("BaseValue", 5);
            Calculator.Remember("Multiplier", 2);
            calculateData.formula = "(Read(BaseValue) * Read(Multiplier)) + 3";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(13f, result); // (5 * 2) + 3 = 13
        }

        #endregion

        #region Target-Specific Tests

        [Test]
        public void Calculate_CasterAndTargetInSameExpression_ReturnsCorrectValue()
        {
            // Arrange
            mockCaster.GetTotal("Attack", false).Returns(30);
            mockTarget.GetTotal("Defense", false).Returns(10);
            calculateData.formula = "Caster.Attack - Target.Defense";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(20f, result); // 30 - 10 = 20
        }

        [Test]
        public void Calculate_MultiplePropertiesFromSameTarget_ReturnsCorrectValue()
        {
            // Arrange
            mockCaster.GetTotal("Attack", false).Returns(30);
            mockCaster.GetTotal("AttackBonus", false).Returns(5);
            calculateData.formula = "Caster.Attack + Caster.AttackBonus";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(35f, result); // 30 + 5 = 35
        }

        [Test]
        public void Calculate_NestedPropertiesInComplexExpression_ReturnsCorrectValue()
        {
            // Arrange
            mockCaster.GetTotal("Attack", false).Returns(30);
            mockCaster.GetTotal("CritRate", false).Returns(20);
            mockTarget.GetTotal("Defense", false).Returns(15);
            calculateData.formula = "(Caster.Attack * (1 + Caster.CritRate / 100)) - Target.Defense";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(21f, result, 0.1f); // (30 * (1 + 20/100)) - 15 = (30 * 1.2) - 15 = 36 - 15 = 21
        }

        #endregion

        #region Complex Integration Tests

        [Test]
        public void Calculate_ComplexExpressionWithMultipleFeatures_ReturnsCorrectValue()
        {
            // Arrange
            mockCaster.GetTotal("Health", false).Returns(50);
            mockTarget.GetTotal("Attack", false).Returns(20);
            Calculator.Remember("Multiplier", 2);
            calculateData.formula = "(Caster.Health + Random(1,1)) * Read(Multiplier) - Target.Attack";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(82f, result); // (50 + 1) * 2 - 20 = 102 - 20 = 82
        }

        [Test]
        public void Calculate_NestedFunctions_ReturnsCorrectValue()
        {
            // Arrange
            Calculator.Remember("BaseValue", 5);
            calculateData.formula = "Random(Read(BaseValue), Read(BaseValue) * 2)";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.GreaterOrEqual(result, 5f);
            Assert.LessOrEqual(result, 10f); // 5 * 2 = 10
        }

        [Test]
        public void Calculate_ComplexNestedExpression_ReturnsCorrectValue()
        {
            // Arrange
            mockCaster.GetTotal("Health", false).Returns(50);
            mockTarget.GetTotal("Defense", false).Returns(20);
            Calculator.Remember("Multiplier", 2);
            calculateData.formula = "((Caster.Health / 10) * Read(Multiplier) + (Random(1,1) * 4)) / 2";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(7f, result); // ((50 / 10) * 2 + (1 * 4)) / 2 = (5 * 2 + 4) / 2 = 14 / 2 = 7
        }

        [Test]
        public void Calculate_MixedOperationsAndFunctions_ReturnsCorrectValue()
        {
            // Arrange
            mockCaster.GetTotal("Attack", false).Returns(30);
            mockTarget.GetTotal("Defense", false).Returns(15);
            Calculator.Remember("CritMultiplier", 1.5f);
            calculateData.formula = "(Caster.Attack - Target.Defense) * Read(CritMultiplier) + Random(1,1)";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(23.5f, result, 0.001f); // (30 - 15) * 1.5 + 1 = 15 * 1.5 + 1 = 22.5 + 1 = 23.5
        }

        [Test]
        public void Calculate_ComplexFormulasWithMultipleParentheses_ReturnsCorrectValue()
        {
            // Arrange
            mockCaster.GetTotal("Attack", false).Returns(40);
            mockCaster.GetTotal("CritRate", false).Returns(20);
            mockTarget.GetTotal("Defense", false).Returns(25);
            mockTarget.GetTotal("Resistance", false).Returns(10);
            Calculator.Remember("BaseDamage", 100);
            calculateData.formula = "(Read(BaseDamage) + (Caster.Attack * 2)) * (1 + (Caster.CritRate / 100)) - (Target.Defense + Target.Resistance)";

            // Act
            float result = Calculator.Calculate(calculateData);

            // Assert
            Assert.AreEqual(181f, result, 0.001f);
            // (100 + (40 * 2)) * (1 + (20 / 100)) - (25 + 10)
            // (100 + 80) * (1 + 0.2) - 35
            // 180 * 1.2 - 35
            // 216 - 35 = 181
            // Note: Due to rounding in the Calculator implementation, the result might be slightly different
        }

        #endregion
    }
}
