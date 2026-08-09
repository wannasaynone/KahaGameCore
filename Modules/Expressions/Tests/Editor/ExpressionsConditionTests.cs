using System.Collections.Generic;
using NUnit.Framework;

namespace KahaGameCore.Expressions.Tests
{
    public class ExpressionsConditionTests
    {
        private sealed class DictionaryContext : IExpressionContext
        {
            private readonly Dictionary<string, ExpressionValue> values;

            public DictionaryContext(Dictionary<string, ExpressionValue> values = null)
            {
                this.values = values ?? new Dictionary<string, ExpressionValue>();
            }

            public bool TryResolve(string symbol, out ExpressionValue value)
            {
                return values.TryGetValue(symbol, out value);
            }
        }

        [Test]
        public void EvaluateCondition_EmptyConditionIsTrue()
        {
            ExpressionResult<bool> result = new Expressions().EvaluateCondition("", new DictionaryContext());

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.True);
        }

        [Test]
        public void EvaluateCondition_ComparisonAndLogicalPrecedenceAreApplied()
        {
            ExpressionResult<bool> result = new Expressions().EvaluateCondition(
                "2 > 1 || 3 > 2 && false",
                new DictionaryContext());

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.True);
        }

        [Test]
        public void EvaluateCondition_ParenthesesAndNotAreApplied()
        {
            ExpressionResult<bool> result = new Expressions().EvaluateCondition(
                "!(2 > 1 || false)",
                new DictionaryContext());

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.False);
        }

        [Test]
        public void EvaluateCondition_ResolvesNumericAndBooleanSymbols()
        {
            DictionaryContext context = new DictionaryContext(new Dictionary<string, ExpressionValue>
            {
                ["Supplies"] = ExpressionValue.FromNumber(15f),
                ["Caster.Alive"] = ExpressionValue.FromBoolean(true)
            });

            ExpressionResult<bool> result = new Expressions().EvaluateCondition(
                "$Supplies >= 10 && Caster.Alive",
                context);

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.True);
        }

        [Test]
        public void EvaluateCondition_RandomIsRejected()
        {
            ExpressionResult<bool> result = new Expressions().EvaluateCondition(
                "Random(0, 1) > 0.5",
                new DictionaryContext());

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.Message, Does.Contain("Random"));
        }

        [Test]
        public void EvaluateCondition_NestedBooleanSymbolsFollowPrecedence()
        {
            DictionaryContext context = new DictionaryContext(new Dictionary<string, ExpressionValue>
            {
                ["aa"] = ExpressionValue.FromBoolean(true),
                ["bb"] = ExpressionValue.FromBoolean(false),
                ["cc"] = ExpressionValue.FromBoolean(true)
            });

            ExpressionResult<bool> result = new Expressions().EvaluateCondition("$aa && ($bb || $cc)", context);

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.True);
        }

        [Test]
        public void EvaluateCondition_RandomIsRejectedEvenInsideDeadLogicalBranch()
        {
            ExpressionResult<bool> result = new Expressions().EvaluateCondition(
                "true || Random(0, 1) > 0.5",
                new DictionaryContext());

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo(ExpressionErrorCode.FunctionNotAllowed));
        }
    }
}
