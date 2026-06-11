using System;
using Cysharp.Threading.Tasks;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>ShowHint(文字ID)：以提示視窗顯示 GameTextData 表中的文字並等待玩家確認。</summary>
    public class ShowHintCommand : KahaGameCore.Package.EffectProcessor.EffectCommandBase
    {
        private readonly IGameState gameState;
        private readonly IGameTextProvider textProvider;
        private readonly IHintPresenter hintPresenter;

        public ShowHintCommand(IGameState gameState, IGameTextProvider textProvider, IHintPresenter hintPresenter)
        {
            this.gameState = gameState;
            this.textProvider = textProvider;
            this.hintPresenter = hintPresenter;
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            int textId = FormulaPreprocessor.EvaluateInt(gameState, vars[0]);
            ShowAsync(textId, onCompleted).Forget();
        }

        private async UniTaskVoid ShowAsync(int textId, Action onCompleted)
        {
            await hintPresenter.ShowAsync(textProvider.GetText(textId));
            onCompleted?.Invoke();
        }
    }
}
