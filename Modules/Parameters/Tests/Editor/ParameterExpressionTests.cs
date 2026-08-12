using KahaGameCore.Expressions;
using NUnit.Framework;

namespace KahaGameCore.Parameters.Tests
{
    public class ParameterExpressionTests
    {
        [Test]
        public void Calculate_ResolvesIntParameters()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Day", "天數", initialValue: 1, minValue: 1, maxValue: 999),
                ParameterDefinition.Int("Supplies", "物資", initialValue: 15, minValue: 0, maxValue: 9999)
            });

            ExpressionResult<float> result = parameters.Calculate(
                "$Day + $Supplies * 2");

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.EqualTo(31f));
        }

        [Test]
        public void EvaluateCondition_ResolvesBoolAndFloatParameters()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Bool("OutingUnlocked", "外出解鎖", initialValue: true),
                ParameterDefinition.Float("Speed", "速度", initialValue: 1.5f, minValue: 0f, maxValue: 2f)
            });

            ExpressionResult<bool> result = parameters.EvaluateCondition(
                "$OutingUnlocked && $Speed > 1.25");

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.True);
        }

        [Test]
        public void Calculate_UnknownParameter_ReturnsUnknownSymbol()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Day", "天數", initialValue: 1, minValue: 1, maxValue: 999)
            });

            ExpressionResult<float> result = parameters.Calculate("$Missing");

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo(ExpressionErrorCode.UnknownSymbol));
        }

        [Test]
        public void Calculate_StringParameter_ReturnsUnknownSymbol()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.String("PlayerName", "玩家名稱", initialValue: "Mia")
            });

            ExpressionResult<float> result = parameters.Calculate("$PlayerName");

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo(ExpressionErrorCode.UnknownSymbol));
        }
    }
}
