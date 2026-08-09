using System.Collections.Generic;
using NUnit.Framework;

namespace KahaGameCore.Expressions.Tests
{
    public class ExpressionsCalculationTests
    {
        private sealed class EmptyContext : IExpressionContext
        {
            public bool TryResolve(string symbol, out ExpressionValue value)
            {
                value = default;
                return false;
            }
        }

        private sealed class DictionaryContext : IExpressionContext
        {
            private readonly Dictionary<string, ExpressionValue> values;

            public DictionaryContext(Dictionary<string, ExpressionValue> values)
            {
                this.values = values;
            }

            public bool TryResolve(string symbol, out ExpressionValue value)
            {
                return values.TryGetValue(symbol, out value);
            }
        }

        [Test]
        public void Calculate_MultiplicationPrecedesAddition()
        {
            Expressions expressions = new Expressions();

            ExpressionResult<float> result = expressions.Calculate("10 + 5 * 2", new EmptyContext());

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.EqualTo(20f));
        }

        [Test]
        public void Calculate_ParenthesesChangePrecedence()
        {
            Expressions expressions = new Expressions();

            ExpressionResult<float> result = expressions.Calculate("(10 + 5) * 2", new EmptyContext());

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.EqualTo(30f));
        }

        [Test]
        public void Calculate_DivisionSharesMultiplicationPrecedence()
        {
            Expressions expressions = new Expressions();

            ExpressionResult<float> result = expressions.Calculate("20 / 5 * 3", new EmptyContext());

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.EqualTo(12f));
        }

        [Test]
        public void Calculate_SubtractionSharesAdditionPrecedence()
        {
            Expressions expressions = new Expressions();

            ExpressionResult<float> result = expressions.Calculate("20 - 5 + 2", new EmptyContext());

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.EqualTo(17f));
        }

        [Test]
        public void Calculate_UnaryNegativeAppliesBeforeArithmetic()
        {
            Expressions expressions = new Expressions();

            ExpressionResult<float> result = expressions.Calculate("-5 * 2 + 3", new EmptyContext());

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.EqualTo(-7f));
        }

        [Test]
        public void Calculate_ResolvesParameterSymbolThroughContext()
        {
            Expressions expressions = new Expressions();
            DictionaryContext context = new DictionaryContext(new Dictionary<string, ExpressionValue>
            {
                ["Supplies"] = ExpressionValue.FromNumber(15f)
            });

            ExpressionResult<float> result = expressions.Calculate("$Supplies + 5", context);

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.EqualTo(20f));
        }

        [Test]
        public void Calculate_UnknownSymbolReturnsItsSourceRange()
        {
            Expressions expressions = new Expressions();

            ExpressionResult<float> result = expressions.Calculate("$Missing + 1", new EmptyContext());

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.Position, Is.EqualTo(0));
            Assert.That(result.Error.Length, Is.EqualTo(8));
        }

        [Test]
        public void Calculate_DivisionByZeroReturnsFailure()
        {
            Expressions expressions = new Expressions();

            ExpressionResult<float> result = expressions.Calculate("10 / 0", new EmptyContext());

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.Message, Does.Contain("zero").IgnoreCase);
        }

        [Test]
        public void Calculate_RandomReturnsValueInsideRequestedRange()
        {
            Expressions expressions = new Expressions();

            ExpressionResult<float> result = expressions.Calculate("Random(2, 3)", new EmptyContext());

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.InRange(2f, 3f));
        }

        [Test]
        public void Calculate_WhitespaceIsIgnored()
        {
            ExpressionResult<float> result = new Expressions().Calculate(" 10\t+\n5 ", new EmptyContext());

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.EqualTo(15f));
        }

        [Test]
        public void Calculate_EmptyCalculationReturnsStructuredFailure()
        {
            ExpressionResult<float> result = new Expressions().Calculate("   ", new EmptyContext());

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo(ExpressionErrorCode.EmptyExpression));
            Assert.That(result.Error.Position, Is.EqualTo(0));
            Assert.That(result.Error.Length, Is.EqualTo(0));
        }
    }
}
