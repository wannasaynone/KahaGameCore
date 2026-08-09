using KahaGameCore.Parameters;
using NUnit.Framework;

namespace KahaGameCore.Expressions.Tests
{
    public class ParameterExpressionContextTests
    {
        [Test]
        public void Calculate_ResolvesIntParametersFromRealStore()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Day", "天數", initialValue: 1, minValue: 1, maxValue: 999),
                ParameterDefinition.Int("Supplies", "物資", initialValue: 15, minValue: 0, maxValue: 9999)
            });
            ParameterExpressionContext context = new ParameterExpressionContext(parameters);

            ExpressionResult<float> result = new Expressions().Calculate(
                "$Day + $Supplies * 2",
                context);

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.EqualTo(31f));
        }

        [Test]
        public void EvaluateCondition_ResolvesBoolAndFloatParametersFromRealStore()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Bool("OutingUnlocked", "外出解鎖", initialValue: true),
                ParameterDefinition.Float("Speed", "速度", initialValue: 1.5f, minValue: 0f, maxValue: 2f)
            });
            ParameterExpressionContext context = new ParameterExpressionContext(parameters);

            ExpressionResult<bool> result = new Expressions().EvaluateCondition(
                "$OutingUnlocked && $Speed > 1.25",
                context);

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
            ParameterExpressionContext context = new ParameterExpressionContext(parameters);

            ExpressionResult<float> result = new Expressions().Calculate("$Missing", context);

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
            ParameterExpressionContext context = new ParameterExpressionContext(parameters);

            ExpressionResult<float> result = new Expressions().Calculate("$PlayerName", context);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo(ExpressionErrorCode.UnknownSymbol));
        }
    }
}
