using System.Threading;
using Cysharp.Threading.Tasks;

namespace KahaGameCore.Package.GameFlowSystem
{
    /// <summary>
    /// 在指定時機點檢查事件表並依序執行命中的事件。
    /// 時機字串由 <see cref="GameFlowTimings"/> 產生（GameStart、PhaseStart:{Key} 等）。
    /// </summary>
    public interface IGameFlowEventTriggerService
    {
        /// <param name="cancellationToken">
        /// 流程中止訊號（如返回標題）。取消後不再執行佇列中剩餘的事件。
        /// </param>
        UniTask RaiseTimingAsync(string timing, CancellationToken cancellationToken = default);
    }
}
