using System;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>SetValue(標籤, 公式)：將數值設為公式結果。</summary>
    public class SetValueCommand : KahaGameCore.Effects.EffectCommandBase
    {
        private readonly IGameState gameState;
        private readonly GameFlowExpressions expressions;

        public SetValueCommand(IGameState gameState, GameFlowExpressions expressions)
        {
            this.gameState = gameState;
            this.expressions = expressions;
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            gameState.Set(vars[0], expressions.CalculateInt(vars[1]));
            onCompleted?.Invoke();
        }
    }
}
