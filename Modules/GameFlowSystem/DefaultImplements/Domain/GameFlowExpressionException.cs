using System;
using KahaGameCore.Expressions;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    public sealed class GameFlowExpressionException : Exception
    {
        public GameFlowExpressionException(string expression, ExpressionError error)
            : base($"Expression '{expression}' failed: {error}")
        {
            Expression = expression;
            Error = error;
        }

        public string Expression { get; }
        public ExpressionError Error { get; }
    }
}
