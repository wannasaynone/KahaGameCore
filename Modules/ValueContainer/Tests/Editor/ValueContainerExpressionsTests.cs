using System;
using System.Collections.Generic;
using KahaGameCore.Expressions;
using NUnit.Framework;

namespace KahaGameCore.ValueContainer.Tests
{
    public class ValueContainerExpressionsTests
    {
        private sealed class FakeValueContainer : IValueContainer
        {
            private readonly Dictionary<string, int> values;
            private readonly Dictionary<string, int> baseValues;

            public FakeValueContainer(
                Dictionary<string, int> values,
                Dictionary<string, int> baseValues = null)
            {
                this.values = values;
                this.baseValues = baseValues ?? values;
            }

            public bool? LastBaseOnly { get; private set; }

            public int GetTotal(string tag, bool baseOnly)
            {
                LastBaseOnly = baseOnly;
                Dictionary<string, int> source = baseOnly ? baseValues : values;
                return source.TryGetValue(tag, out int value) ? value : 0;
            }

            public Guid Add(string tag, int value) => throw new NotSupportedException();
            public void AddToTemp(Guid guid, int value) => throw new NotSupportedException();
            public void SetTemp(Guid guid, int value) => throw new NotSupportedException();
            public void AddBase(string tag, int value) => throw new NotSupportedException();
            public void SetBase(string tag, int value) => throw new NotSupportedException();
            public void Remove(Guid guid) => throw new NotSupportedException();
            public string GetStringKeyValue(string key) => throw new NotSupportedException();
            public void RemoveStringKeyValue(string key) => throw new NotSupportedException();
            public void SetStringKeyValue(string key, string value) => throw new NotSupportedException();
            public Dictionary<string, string> GetAllStringKeyValuePairs() => throw new NotSupportedException();
        }

        [Test]
        public void Calculate_ResolvesCasterAndTarget()
        {
            IValueContainer caster = new FakeValueContainer(
                new Dictionary<string, int> { ["HP"] = 20 });
            IValueContainer target = new FakeValueContainer(
                new Dictionary<string, int> { ["Defense"] = 7 });
            ValueContainerExpressions expressions =
                new ValueContainerExpressions(caster, target);

            ExpressionResult<float> result =
                expressions.Calculate("Caster.HP - Target.Defense");

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.EqualTo(13f));
        }

        [Test]
        public void Calculate_BaseOnlyReadsBaseValues()
        {
            FakeValueContainer caster = new FakeValueContainer(
                new Dictionary<string, int> { ["Attack"] = 15 },
                new Dictionary<string, int> { ["Attack"] = 10 });
            ValueContainerExpressions expressions = new ValueContainerExpressions(
                caster,
                target: null,
                baseOnly: true);

            ExpressionResult<float> result = expressions.Calculate("Caster.Attack");

            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            Assert.That(result.Value, Is.EqualTo(10f));
            Assert.That(caster.LastBaseOnly, Is.True);
        }
    }
}
