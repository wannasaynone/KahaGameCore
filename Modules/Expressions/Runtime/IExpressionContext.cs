namespace KahaGameCore.Expressions
{
    public interface IExpressionContext
    {
        bool TryResolve(string symbol, out ExpressionValue value);
    }
}
