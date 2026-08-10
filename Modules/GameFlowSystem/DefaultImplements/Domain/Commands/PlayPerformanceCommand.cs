using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>PlayPerformance(演出ID)：播放一段已註冊的 UGUI 演出並等待結束。</summary>
    public class PlayPerformanceCommand : IEffectCommand
    {
        private readonly IPerformancePlayer performancePlayer;

        public PlayPerformanceCommand(IPerformancePlayer performancePlayer)
        {
            this.performancePlayer = performancePlayer;
        }

        public async UniTask ExecuteAsync(
            EffectExecutionContext context,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await performancePlayer.PlayAsync(arguments[0])
                .AttachExternalCancellation(cancellationToken);
        }
    }
}
