using System.Collections.Generic;
using Assets.Scripts.StateMachine;
using KahaGameCore.Package.EffectProcessor.ValueContainer;
using NUnit.Framework;
using NSubstitute;
using UnityEngine;
using UnityEngine.TestTools;

namespace KahaGameCore.Tests
{
    public class ValueResolverFactoryTest
    {
        private IValueContainer mockSelf;
        private IValueContainer mockHero;
        private List<IValueContainer> mockTargets;
        private StateDefinition mockStateDefinition;

        [SetUp]
        public void SetUp()
        {
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
            if (mockStateDefinition != null)
                Object.DestroyImmediate(mockStateDefinition);
        }

        #region ResolveValue - Direct Number Tests

        [Test]
        public void ResolveValue_DirectInteger_ReturnsCorrectValue()
        {
            // Act
            bool result = ValueResolverFactory.ResolveValue("42", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(42f, resolvedValue);
        }

        [Test]
        public void ResolveValue_DirectFloat_ReturnsCorrectValue()
        {
            // Act
            bool result = ValueResolverFactory.ResolveValue("3.14", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(3.14f, resolvedValue, 0.001f);
        }

        [Test]
        public void ResolveValue_NegativeNumber_ReturnsCorrectValue()
        {
            // Act
            bool result = ValueResolverFactory.ResolveValue("-25", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(-25f, resolvedValue);
        }

        [Test]
        public void ResolveValue_Zero_ReturnsCorrectValue()
        {
            // Act
            bool result = ValueResolverFactory.ResolveValue("0", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        [Test]
        public void ResolveValue_NullOrEmpty_ReturnsFalse()
        {
            // Test null
            bool result1 = ValueResolverFactory.ResolveValue(null, mockSelf, mockTargets, mockStateDefinition, out float resolvedValue1);
            Assert.IsFalse(result1);
            Assert.AreEqual(0f, resolvedValue1);

            // Test empty string
            bool result2 = ValueResolverFactory.ResolveValue("", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue2);
            Assert.IsFalse(result2);
            Assert.AreEqual(0f, resolvedValue2);
        }

        #endregion

        #region ResolveValue - Container Property Tests

        [Test]
        public void ResolveValue_SelfProperty_ReturnsCorrectValue()
        {
            // Arrange
            mockSelf.GetTotal("Health", false).Returns(100);

            // Act
            bool result = ValueResolverFactory.ResolveValue("Self.Health", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(100f, resolvedValue);
        }

        [Test]
        public void ResolveValue_HeroProperty_ReturnsCorrectValue()
        {
            // Arrange
            mockHero.GetTotal("AttackPower", false).Returns(75);

            // Act
            bool result = ValueResolverFactory.ResolveValue("Hero.AttackPower", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(75f, resolvedValue);
        }

        [Test]
        public void ResolveValue_HeroProperty_NoTargets_ReturnsZero()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Hero target not available");

            // Act
            bool result = ValueResolverFactory.ResolveValue("Hero.AttackPower", mockSelf, null, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        [Test]
        public void ResolveValue_HeroProperty_EmptyTargets_ReturnsZero()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Hero target not available");

            // Act
            bool result = ValueResolverFactory.ResolveValue("Hero.AttackPower", mockSelf, new List<IValueContainer>(), mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        [Test]
        public void ResolveValue_UnknownContainer_ReturnsZero()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Container not recognized: Unknown");

            // Act
            bool result = ValueResolverFactory.ResolveValue("Unknown.Property", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        [Test]
        public void ResolveValue_InvalidContainerPropertyFormat_ReturnsZero()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Invalid container.property format: Self.");

            // Act
            bool result = ValueResolverFactory.ResolveValue("Self.", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        [Test]
        public void ResolveValue_TooManyDots_ReturnsZero()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Invalid container.property format: Self.Health.Extra");

            // Act
            bool result = ValueResolverFactory.ResolveValue("Self.Health.Extra", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        #endregion

        #region ResolveValue - Special Value Tests

        [Test]
        public void ResolveValue_StateTime_ReturnsCorrectValue()
        {
            // Arrange
            mockStateDefinition.stateTimer = 7.5f;

            // Act
            bool result = ValueResolverFactory.ResolveValue("StateTime", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(7.5f, resolvedValue);
        }

        [Test]
        public void ResolveValue_UnknownSpecialValue_ReturnsFalse()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Value not recognized: UnknownValue");

            // Act
            bool result = ValueResolverFactory.ResolveValue("UnknownValue", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        #endregion

        #region ResolveValue - Random Function Tests

        [Test]
        public void ResolveValue_ValidRandomFunction_ReturnsValueInRange()
        {
            // Act & Assert - 測試多次以確保隨機值在範圍內
            for (int i = 0; i < 10; i++)
            {
                bool result = ValueResolverFactory.ResolveValue("Random(1,10)", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);
                Assert.IsTrue(result);
                Assert.GreaterOrEqual(resolvedValue, 1f);
                Assert.LessOrEqual(resolvedValue, 10f);
            }
        }

        [Test]
        public void ResolveValue_RandomWithSameMinMax_ReturnsConstantValue()
        {
            // Act
            bool result = ValueResolverFactory.ResolveValue("Random(5,5)", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(5f, resolvedValue);
        }

        [Test]
        public void ResolveValue_RandomWithVariables_WorksCorrectly()
        {
            // Arrange
            mockSelf.GetTotal("MinValue", false).Returns(2);
            mockSelf.GetTotal("MaxValue", false).Returns(8);

            // Act
            bool result = ValueResolverFactory.ResolveValue("Random(Self.MinValue,Self.MaxValue)", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.GreaterOrEqual(resolvedValue, 2f);
            Assert.LessOrEqual(resolvedValue, 8f);
        }

        [Test]
        public void ResolveValue_RandomWithNegativeValues_WorksCorrectly()
        {
            // Act
            bool result = ValueResolverFactory.ResolveValue("Random(-5,5)", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.GreaterOrEqual(resolvedValue, -5f);
            Assert.LessOrEqual(resolvedValue, 5f);
        }

        [Test]
        public void ResolveValue_RandomWithFloats_WorksCorrectly()
        {
            // Act
            bool result = ValueResolverFactory.ResolveValue("Random(1.5,2.5)", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.GreaterOrEqual(resolvedValue, 1.5f);
            Assert.LessOrEqual(resolvedValue, 2.5f);
        }

        #endregion

        #region Random Function Error Handling Tests

        [Test]
        public void ResolveValue_RandomWithNoParameters_ReturnsZero()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Random function has empty parameters: Random()");

            // Act
            bool result = ValueResolverFactory.ResolveValue("Random()", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        [Test]
        public void ResolveValue_RandomWithOneParameter_ReturnsZero()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Random function requires exactly 2 parameters separated by comma. Got 1 parameters in: Random(5)");

            // Act
            bool result = ValueResolverFactory.ResolveValue("Random(5)", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        [Test]
        public void ResolveValue_RandomWithThreeParameters_ReturnsZero()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Random function requires exactly 2 parameters separated by comma. Got 3 parameters in: Random(1,5,10)");

            // Act
            bool result = ValueResolverFactory.ResolveValue("Random(1,5,10)", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        [Test]
        public void ResolveValue_RandomWithEmptyParameter_ReturnsZero()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Random function has empty parameters: min='', max='5' in Random(,5)");

            // Act
            bool result = ValueResolverFactory.ResolveValue("Random(,5)", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        [Test]
        public void ResolveValue_RandomWithInvalidSyntax_ReturnsZero()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Invalid Random function syntax: Random(");

            // Act
            bool result = ValueResolverFactory.ResolveValue("Random(", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        [Test]
        public void ResolveValue_RandomWithInvalidParameters_ReturnsZero()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Failed to resolve Random function parameters: min='InvalidValue', max='5' in Random(InvalidValue,5)");

            // Act
            bool result = ValueResolverFactory.ResolveValue("Random(InvalidValue,5)", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        [Test]
        public void ResolveValue_RandomWithMinGreaterThanMax_ReturnsZero()
        {
            // Arrange
            LogAssert.Expect(LogType.Warning, "Random function: min value (10) is greater than max value (5) in Random(10,5). Swapping values.");

            // Act
            bool result = ValueResolverFactory.ResolveValue("Random(10,5)", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        #endregion

        #region Unknown Function Tests

        [Test]
        public void ResolveValue_UnknownFunction_ReturnsZero()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Unknown function call: UnknownFunction(1,2)");

            // Act
            bool result = ValueResolverFactory.ResolveValue("UnknownFunction(1,2)", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        [Test]
        public void ResolveValue_MalformedFunction_ReturnsZero()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Unknown function call: SomeFunction(");

            // Act
            bool result = ValueResolverFactory.ResolveValue("SomeFunction(", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Test]
        public void ResolveValue_WhitespaceInInput_HandledCorrectly()
        {
            // Arrange
            mockSelf.GetTotal("Health", false).Returns(100);

            // Act
            bool result = ValueResolverFactory.ResolveValue(" Self.Health ", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(100f, resolvedValue);
        }

        [Test]
        public void ResolveValue_RandomWithWhitespace_WorksCorrectly()
        {
            // Act
            bool result = ValueResolverFactory.ResolveValue("Random( 1 , 5 )", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.GreaterOrEqual(resolvedValue, 1f);
            Assert.LessOrEqual(resolvedValue, 5f);
        }

        [Test]
        public void ResolveValue_NestedRandomInParameters_HandledCorrectly()
        {
            // This should fail as nested Random functions are not supported
            // The error message might vary depending on which part fails first
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*"));

            // Act
            bool result = ValueResolverFactory.ResolveValue("Random(Random(1,2),5)", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        [Test]
        public void ResolveValue_ComplexExpression_WithStateTime()
        {
            // Arrange
            mockStateDefinition.stateTimer = 3.5f;

            // Act
            bool result = ValueResolverFactory.ResolveValue("StateTime", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(3.5f, resolvedValue);
        }

        [Test]
        public void ResolveValue_CaseInsensitive_ContainerNames()
        {
            // The current implementation is case-sensitive, so this should fail
            LogAssert.Expect(LogType.Error, "Container not recognized: self");

            // Act
            bool result = ValueResolverFactory.ResolveValue("self.Health", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        [Test]
        public void ResolveValue_RandomWithZeroRange_WorksCorrectly()
        {
            // Act
            bool result = ValueResolverFactory.ResolveValue("Random(0,0)", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, resolvedValue);
        }

        [Test]
        public void ResolveValue_VeryLargeNumbers_WorksCorrectly()
        {
            // Act
            bool result = ValueResolverFactory.ResolveValue("999999", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(999999f, resolvedValue);
        }

        [Test]
        public void ResolveValue_VerySmallNumbers_WorksCorrectly()
        {
            // Act
            bool result = ValueResolverFactory.ResolveValue("0.001", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0.001f, resolvedValue, 0.0001f);
        }

        #endregion

        #region Performance and Stress Tests

        [Test]
        public void ResolveValue_MultipleRandomCalls_PerformanceTest()
        {
            // Act & Assert - 測試多次調用的性能
            for (int i = 0; i < 100; i++)
            {
                bool result = ValueResolverFactory.ResolveValue("Random(1,100)", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue);
                Assert.IsTrue(result);
                Assert.GreaterOrEqual(resolvedValue, 1f);
                Assert.LessOrEqual(resolvedValue, 100f);
            }
        }

        [Test]
        public void ResolveValue_MultipleContainerAccess_PerformanceTest()
        {
            // Arrange
            mockSelf.GetTotal("Health", false).Returns(100);
            mockHero.GetTotal("AttackPower", false).Returns(50);

            // Act & Assert
            for (int i = 0; i < 50; i++)
            {
                bool result1 = ValueResolverFactory.ResolveValue("Self.Health", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue1);
                bool result2 = ValueResolverFactory.ResolveValue("Hero.AttackPower", mockSelf, mockTargets, mockStateDefinition, out float resolvedValue2);

                Assert.IsTrue(result1);
                Assert.IsTrue(result2);
                Assert.AreEqual(100f, resolvedValue1);
                Assert.AreEqual(50f, resolvedValue2);
            }
        }

        #endregion
    }
}
