using System;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>AddValue(標籤, 公式)：將數值加上公式結果（可為負）。</summary>
    public class AddValueCommand : KahaGameCore.Package.EffectProcessor.EffectCommandBase
    {
        private readonly IGameState gameState;

        public AddValueCommand(IGameState gameState)
        {
            this.gameState = gameState;
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            gameState.Add(vars[0], FormulaPreprocessor.EvaluateInt(gameState, vars[1]));
            onCompleted?.Invoke();
        }
    }
}
