using KahaGameCore.Expressions;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    internal sealed class GameStateExpressionContext : IExpressionContext
    {
        private readonly IGameState gameState;

        public GameStateExpressionContext(IGameState gameState)
        {
            this.gameState = gameState;
        }

        public bool TryResolve(string symbol, out ExpressionValue value)
        {
            if (gameState.TryGet(symbol, out int resolved))
            {
                value = ExpressionValue.FromNumber(resolved);
                return true;
            }

            value = default;
            return false;
        }
    }
}
