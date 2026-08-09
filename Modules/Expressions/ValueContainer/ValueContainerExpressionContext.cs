using System;
using KahaGameCore.ValueContainer;

namespace KahaGameCore.Expressions
{
    public sealed class ValueContainerExpressionContext : IExpressionContext
    {
        private const string CasterPrefix = "Caster.";
        private const string TargetPrefix = "Target.";

        private readonly IValueContainer caster;
        private readonly IValueContainer target;
        private readonly bool baseOnly;

        public ValueContainerExpressionContext(
            IValueContainer caster,
            IValueContainer target,
            bool baseOnly = false)
        {
            this.caster = caster;
            this.target = target;
            this.baseOnly = baseOnly;
        }

        public bool TryResolve(string symbol, out ExpressionValue value)
        {
            if (TryResolveFrom(symbol, CasterPrefix, caster, baseOnly, out value)
                || TryResolveFrom(symbol, TargetPrefix, target, baseOnly, out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryResolveFrom(
            string symbol,
            string prefix,
            IValueContainer container,
            bool baseOnly,
            out ExpressionValue value)
        {
            if (container != null
                && symbol.StartsWith(prefix, StringComparison.Ordinal)
                && symbol.Length > prefix.Length)
            {
                string tag = symbol.Substring(prefix.Length);
                value = ExpressionValue.FromNumber(container.GetTotal(tag, baseOnly));
                return true;
            }

            value = default;
            return false;
        }
    }
}
