using System;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>SetValue(標籤, 公式)：將數值設為公式結果。</summary>
    public class SetValueCommand : KahaGameCore.Package.EffectProcessor.EffectCommandBase
    {
        private readonly IGameState gameState;

        public SetValueCommand(IGameState gameState)
        {
            this.gameState = gameState;
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            gameState.Set(vars[0], FormulaPreprocessor.EvaluateInt(gameState, vars[1]));
            onCompleted?.Invoke();
        }
    }
}
