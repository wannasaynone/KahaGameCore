using System;
using Cysharp.Threading.Tasks;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>PlayPerformance(演出ID)：播放一段已註冊的 UGUI 演出並等待結束。</summary>
    public class PlayPerformanceCommand : KahaGameCore.Package.EffectProcessor.EffectCommandBase
    {
        private readonly IPerformancePlayer performancePlayer;

        public PlayPerformanceCommand(IPerformancePlayer performancePlayer)
        {
            this.performancePlayer = performancePlayer;
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            PlayAsync(vars[0], onCompleted).Forget();
        }

        private async UniTaskVoid PlayAsync(string performanceId, Action onCompleted)
        {
            await performancePlayer.PlayAsync(performanceId);
            onCompleted?.Invoke();
        }
    }
}
