using System;
using System.Collections.Generic;

namespace KahaGameCore.Expressions.Internal
{
    internal enum ExpressionEntryPoint { Calculation, Condition }

    internal sealed class CompiledExpression
    {
        public CompiledExpression(ExpressionSyntax root, ExpressionError error) { Root = root; Error = error; }
        public ExpressionSyntax Root { get; }
        public ExpressionError Error { get; }
        public bool IsSuccess => Error == null;
    }

    internal sealed class ExpressionCache
    {
        private readonly Dictionary<string, CompiledExpression> calculations = new Dictionary<string, CompiledExpression>();
        private readonly Dictionary<string, CompiledExpression> conditions = new Dictionary<string, CompiledExpression>();

        public CompiledExpression GetOrCompile(string source, ExpressionEntryPoint entryPoint)
        {
            Dictionary<string, CompiledExpression> cache = entryPoint == ExpressionEntryPoint.Calculation ? calculations : conditions;
            lock (cache)
            {
                if (cache.TryGetValue(source, out CompiledExpression compiled)) return compiled;
                ExpressionParseResult parsed = ExpressionParser.Parse(source);
                compiled = new CompiledExpression(parsed.Root, parsed.Error);
                cache[source] = compiled;
                return compiled;
            }
        }
    }
}
