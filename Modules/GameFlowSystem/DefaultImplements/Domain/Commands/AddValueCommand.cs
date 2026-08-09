using System;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>AddValue(標籤, 公式)：將數值加上公式結果（可為負）。</summary>
    public class AddValueCommand : KahaGameCore.Effects.EffectCommandBase
    {
        private readonly IGameState gameState;
        private readonly GameFlowExpressions expressions;

        public AddValueCommand(IGameState gameState, GameFlowExpressions expressions)
        {
            this.gameState = gameState;
            this.expressions = expressions;
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            gameState.Add(vars[0], expressions.CalculateInt(vars[1]));
            onCompleted?.Invoke();
        }
    }
}
