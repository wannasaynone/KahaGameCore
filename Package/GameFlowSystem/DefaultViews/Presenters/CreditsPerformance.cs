using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements;

namespace KahaGameCore.Package.GameFlowSystem.DefaultViews
{
    /// <summary>
    /// 演出「Credits」的實作：顯示製作人員名單畫面並等待玩家結束。
    /// 這是 IStagePerformance 的串接範例——其他演出（換日、丈夫外出……）依同樣方式
    /// 實作後註冊到 IPerformancePlayer 即可被表格引用。
    /// </summary>
    public class CreditsPerformance : IStagePerformance
    {
        private readonly CreditsView view;
        private readonly IGameTextProvider textProvider;
        private readonly int creditsTextId;

        public CreditsPerformance(CreditsView view, IGameTextProvider textProvider, int creditsTextId)
        {
            this.view = view ? view : throw new ArgumentNullException(nameof(view));
            this.textProvider = textProvider ?? throw new ArgumentNullException(nameof(textProvider));
            this.creditsTextId = creditsTextId;
        }

        public async UniTask PlayAsync()
        {
            UniTaskCompletionSource finishedSource = new UniTaskCompletionSource();

            view.Bind(textProvider.GetText(creditsTextId), () => finishedSource.TrySetResult());
            await view.Show(CancellationToken.None);

            await finishedSource.Task;

            await view.Hide(CancellationToken.None);
        }
    }
}
