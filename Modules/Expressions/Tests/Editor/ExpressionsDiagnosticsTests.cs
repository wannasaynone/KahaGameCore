using NUnit.Framework;

namespace KahaGameCore.Expressions.Tests
{
    public class ExpressionsDiagnosticsTests
    {
        private sealed class EmptyContext : IExpressionContext
        {
            public bool TryResolve(string symbol, out ExpressionValue value)
            {
                value = default;
                return false;
            }
        }

        [Test]
        public void Calculate_UnmatchedParenthesisPointsAtEndOfSource()
        {
            ExpressionResult<float> result = new Expressions().Calculate("(1 + 2", new EmptyContext());

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo(ExpressionErrorCode.UnexpectedToken));
            Assert.That(result.Error.Position, Is.EqualTo(6));
            Assert.That(result.Error.Length, Is.EqualTo(0));
        }

        [Test]
        public void Calculate_UnexpectedTokenPointsAtToken()
        {
            ExpressionResult<float> result = new Expressions().Calculate("1 + )", new EmptyContext());

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.Position, Is.EqualTo(4));
            Assert.That(result.Error.Length, Is.EqualTo(1));
        }

        [Test]
        public void Calculate_InvalidOperandPointsAtOperator()
        {
            ExpressionResult<float> result = new Expressions().Calculate("true + 1", new EmptyContext());

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo(ExpressionErrorCode.TypeMismatch));
            Assert.That(result.Error.Position, Is.EqualTo(5));
            Assert.That(result.Error.Length, Is.EqualTo(1));
        }
    }
}
