using System;
using Cysharp.Threading.Tasks;
using KahaGameCore.GameFlowSystem.DefaultImplements;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>ShowHint(文字ID)：以提示視窗顯示 GameTextData 表中的文字並等待玩家確認。</summary>
    public class ShowHintCommand : KahaGameCore.Effects.EffectCommandBase
    {
        private readonly GameFlowExpressions expressions;
        private readonly IGameTextProvider textProvider;
        private readonly IHintPresenter hintPresenter;

        public ShowHintCommand(GameFlowExpressions expressions, IGameTextProvider textProvider, IHintPresenter hintPresenter)
        {
            this.expressions = expressions;
            this.textProvider = textProvider;
            this.hintPresenter = hintPresenter;
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            int textId = expressions.CalculateInt(vars[0]);
            ShowAsync(textId, onCompleted).Forget();
        }

        private async UniTaskVoid ShowAsync(int textId, Action onCompleted)
        {
            await hintPresenter.ShowAsync(textProvider.GetText(textId));
            onCompleted?.Invoke();
        }
    }
}
