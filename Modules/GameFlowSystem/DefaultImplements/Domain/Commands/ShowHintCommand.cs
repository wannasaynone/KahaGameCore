using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.GameFlowSystem.DefaultImplements;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>ShowHint(文字ID)：以提示視窗顯示 GameTextData 表中的文字並等待玩家確認。</summary>
    public class ShowHintCommand : IEffectCommand
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

        public async UniTask ExecuteAsync(
            EffectExecutionContext context,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int textId = expressions.CalculateInt(arguments[0]);
            await hintPresenter.ShowAsync(textProvider.GetText(textId))
                .AttachExternalCancellation(cancellationToken);
        }
    }
}
