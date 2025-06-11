using System.Collections.Generic;
using Assets.Scripts.StateMachine;
using KahaGameCore.Package.EffectProcessor.ValueContainer;
using NUnit.Framework;
using NSubstitute;
using UnityEngine;
using UnityEngine.TestTools;

namespace KahaGameCore.Tests
{
    public class ConditionDefinitionTest
    {
        private ConditionDefinition conditionDefinition;
        private IValueContainer mockSelf;
        private IValueContainer mockHero;
        private List<IValueContainer> mockTargets;
        private StateDefinition mockStateDefinition;

        [SetUp]
        public void SetUp()
        {
            // 創建 ConditionDefinition 實例
            conditionDefinition = ScriptableObject.CreateInstance<ConditionDefinition>();

            // 創建 Mock 物件
            mockSelf = Substitute.For<IValueContainer>();
            mockHero = Substitute.For<IValueContainer>();
            mockTargets = new List<IValueContainer> { mockHero };

            // 創建 StateDefinition 實例
            mockStateDefinition = ScriptableObject.CreateInstance<StateDefinition>();
            mockStateDefinition.stateTimer = 5.0f;
        }

        [TearDown]
        public void TearDown()
        {
            if (conditionDefinition != null)
                Object.DestroyImmediate(conditionDefinition);
            if (mockStateDefinition != null)
                Object.DestroyImmediate(mockStateDefinition);
        }

        #region Basic Functionality Tests

        [Test]
        public void Evaluate_EmptyConditionContent_ReturnsTrue()
        {
            // Arrange
            conditionDefinition.conditionContent = "";

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_NullConditionContent_ReturnsTrue()
        {
            // Arrange
            conditionDefinition.conditionContent = null;

            LogAssert.Expect(LogType.Warning, "Condition content is empty or null, will pass anyway.");
            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_WhitespaceAndNewlines_HandledCorrectly()
        {
            // Arrange
            conditionDefinition.conditionContent = "  \n  IsControlable  \n  ";
            mockSelf.GetTotal("IsControlable", false).Returns(1);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_MultipleConditionsWithSemicolon_AllMustPass()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsControlable;Self.Health>0";
            mockSelf.GetTotal("IsControlable", false).Returns(1);
            mockSelf.GetTotal("Health", false).Returns(50);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_MultipleConditions_OneFailsReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsControlable;Self.Health>100";
            mockSelf.GetTotal("IsControlable", false).Returns(1);
            mockSelf.GetTotal("Health", false).Returns(50);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Built-in Condition Tests

        [Test]
        public void Evaluate_IsControlable_WhenTrue_ReturnsTrue()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsControlable";
            mockSelf.GetTotal("IsControlable", false).Returns(1);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_IsControlable_WhenFalse_ReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsControlable";
            mockSelf.GetTotal("IsControlable", false).Returns(0);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Evaluate_IsHeroSameRoom_SameRoom_ReturnsTrue()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsHeroSameRoom";
            mockSelf.GetTotal("CurrentRoom", false).Returns(1);
            mockHero.GetTotal("CurrentRoom", false).Returns(1);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_IsHeroSameRoom_DifferentRoom_ReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsHeroSameRoom";
            mockSelf.GetTotal("CurrentRoom", false).Returns(1);
            mockHero.GetTotal("CurrentRoom", false).Returns(2);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Evaluate_IsHeroSameRoom_InvalidRoom_ReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsHeroSameRoom";
            mockSelf.GetTotal("CurrentRoom", false).Returns(-1);
            mockHero.GetTotal("CurrentRoom", false).Returns(1);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Evaluate_IsHeroSameRoom_NoTargets_ReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsHeroSameRoom";

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, null, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Evaluate_IsHeroInRange_WithinRange_ReturnsTrue()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsHeroInRange";
            mockSelf.GetStringKeyValue("PositionX").Returns("10.0");
            mockHero.GetStringKeyValue("PositionX").Returns("12.0");
            mockSelf.GetStringKeyValue("AttackRange").Returns("5.0");

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_IsHeroInRange_OutOfRange_ReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsHeroInRange";
            mockSelf.GetStringKeyValue("PositionX").Returns("10.0");
            mockHero.GetStringKeyValue("PositionX").Returns("20.0");
            mockSelf.GetStringKeyValue("AttackRange").Returns("5.0");

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Evaluate_IsHeroInRange_ExactRange_ReturnsTrue()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsHeroInRange";
            mockSelf.GetStringKeyValue("PositionX").Returns("10.0");
            mockHero.GetStringKeyValue("PositionX").Returns("15.0");
            mockSelf.GetStringKeyValue("AttackRange").Returns("5.0");

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_IsHeroLeft_HeroInDifferentRoom_ReturnsTrue()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsHeroLeft";
            mockSelf.GetTotal("CurrentRoom", false).Returns(1);
            mockHero.GetTotal("CurrentRoom", false).Returns(2);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_IsHeroLeft_HeroInSameRoom_ReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsHeroLeft";
            mockSelf.GetTotal("CurrentRoom", false).Returns(1);
            mockHero.GetTotal("CurrentRoom", false).Returns(1);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Comparison Operation Tests

        [Test]
        public void Evaluate_GreaterThan_True_ReturnsTrue()
        {
            // Arrange
            conditionDefinition.conditionContent = "Self.Health>50";
            mockSelf.GetTotal("Health", false).Returns(75);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_GreaterThan_False_ReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "Self.Health>50";
            mockSelf.GetTotal("Health", false).Returns(25);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Evaluate_LessThan_True_ReturnsTrue()
        {
            // Arrange
            conditionDefinition.conditionContent = "Self.Health<50";
            mockSelf.GetTotal("Health", false).Returns(25);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_GreaterThanOrEqual_Equal_ReturnsTrue()
        {
            // Arrange
            conditionDefinition.conditionContent = "Self.Health>=50";
            mockSelf.GetTotal("Health", false).Returns(50);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_GreaterThanOrEqual_Greater_ReturnsTrue()
        {
            // Arrange
            conditionDefinition.conditionContent = "Self.Health>=50";
            mockSelf.GetTotal("Health", false).Returns(75);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_LessThanOrEqual_Equal_ReturnsTrue()
        {
            // Arrange
            conditionDefinition.conditionContent = "Self.Health<=50";
            mockSelf.GetTotal("Health", false).Returns(50);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_Equal_True_ReturnsTrue()
        {
            // Arrange
            conditionDefinition.conditionContent = "Self.Health==50";
            mockSelf.GetTotal("Health", false).Returns(50);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_NotEqual_True_ReturnsTrue()
        {
            // Arrange
            conditionDefinition.conditionContent = "Self.Health!=50";
            mockSelf.GetTotal("Health", false).Returns(75);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_NotEqual_False_ReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "Self.Health!=50";
            mockSelf.GetTotal("Health", false).Returns(50);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Value Resolution Tests

        [Test]
        public void Evaluate_DirectNumber_WorksCorrectly()
        {
            // Arrange
            conditionDefinition.conditionContent = "Self.Health>25";
            mockSelf.GetTotal("Health", false).Returns(50);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_SelfProperty_WorksCorrectly()
        {
            // Arrange
            conditionDefinition.conditionContent = "Self.Health>Self.MinHealth";
            mockSelf.GetTotal("Health", false).Returns(50);
            mockSelf.GetTotal("MinHealth", false).Returns(25);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_HeroProperty_WorksCorrectly()
        {
            // Arrange
            conditionDefinition.conditionContent = "Self.Health>Hero.AttackPower";
            mockSelf.GetTotal("Health", false).Returns(50);
            mockHero.GetTotal("AttackPower", false).Returns(25);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_StateTime_WorksCorrectly()
        {
            // Arrange
            conditionDefinition.conditionContent = "StateTime>3";
            mockStateDefinition.stateTimer = 5.0f;

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_NegativeNumbers_WorksCorrectly()
        {
            // Arrange
            conditionDefinition.conditionContent = "Self.Health>-10";
            mockSelf.GetTotal("Health", false).Returns(0);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_DecimalNumbers_WorksCorrectly()
        {
            // Arrange
            conditionDefinition.conditionContent = "StateTime>2.5";
            mockStateDefinition.stateTimer = 3.0f;

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        #endregion

        #region Random Function Tests

        [Test]
        public void Evaluate_ValidRandomFunction_WorksCorrectly()
        {
            // Arrange
            conditionDefinition.conditionContent = "Random(1,10)>0";

            // Act & Assert - 隨機函數應該總是返回 1-10 之間的值，所以 >0 應該總是 true
            for (int i = 0; i < 10; i++)
            {
                bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);
                Assert.IsTrue(result);
            }
        }

        [Test]
        public void Evaluate_RandomWithSameMinMax_ReturnsConstantValue()
        {
            // Arrange
            conditionDefinition.conditionContent = "Random(5,5)==5";

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_RandomWithVariables_WorksCorrectly()
        {
            // Arrange
            conditionDefinition.conditionContent = "Random(Self.MinValue,Self.MaxValue)>=Self.MinValue";
            mockSelf.GetTotal("MinValue", false).Returns(1);
            mockSelf.GetTotal("MaxValue", false).Returns(10);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Evaluate_UnrecognizedCondition_LogsErrorAndReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "UnknownCondition";
            LogAssert.Expect(LogType.Error, "Condition not recognized compare symbol: UnknownCondition");

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Evaluate_InvalidComparisonOperator_LogsErrorAndReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "Self.Health<>50";
            LogAssert.Expect(LogType.Error, "Condition not recognized compare symbol: Self.Health<>50");

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Evaluate_InvalidContainerProperty_LogsErrorAndReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "Unknown.Property>0";
            LogAssert.Expect(LogType.Error, "Container not recognized: Unknown");

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Evaluate_HeroPropertyWithNoTargets_LogsErrorAndReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "Hero.Health>0";
            LogAssert.Expect(LogType.Error, "Hero target not available");

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, null, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Evaluate_InvalidRandomFunction_LogsErrorAndReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "Random()>0";
            LogAssert.Expect(LogType.Error, "Invalid Random function syntax: Random()");
            LogAssert.Expect(LogType.Error, "Unknown function call: Random()");

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Evaluate_RandomWithOneParameter_LogsErrorAndReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "Random(5)>0";
            LogAssert.Expect(LogType.Error, "Random function requires exactly 2 parameters separated by comma. Got 1 parameters in: Random(5)");

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Evaluate_ComplexConditionString_WorksCorrectly()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsControlable;Self.Health>50;Hero.AttackPower<100;StateTime>2";
            mockSelf.GetTotal("IsControlable", false).Returns(1);
            mockSelf.GetTotal("Health", false).Returns(75);
            mockHero.GetTotal("AttackPower", false).Returns(80);
            mockStateDefinition.stateTimer = 3.0f;

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Evaluate_MixedConditionTypes_OneFailsReturnsFalse()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsControlable;IsHeroSameRoom;Self.Health>100";
            mockSelf.GetTotal("IsControlable", false).Returns(1);
            mockSelf.GetTotal("CurrentRoom", false).Returns(1);
            mockHero.GetTotal("CurrentRoom", false).Returns(1);
            mockSelf.GetTotal("Health", false).Returns(50); // This will fail

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Evaluate_EmptyConditionParts_IgnoredCorrectly()
        {
            // Arrange
            conditionDefinition.conditionContent = "IsControlable;;Self.Health>0;";
            mockSelf.GetTotal("IsControlable", false).Returns(1);
            mockSelf.GetTotal("Health", false).Returns(50);

            // Act
            bool result = conditionDefinition.Evaluate(mockSelf, mockTargets, mockStateDefinition);

            // Assert
            Assert.IsTrue(result);
        }

        #endregion
    }
}
