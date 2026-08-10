using System.Threading;
using Cysharp.Threading.Tasks;

namespace KahaGameCore.GameFlowSystem
{
    /// <summary>執行表格中的效果指令串（KahaGameCore EffectRuntime 語法）。</summary>
    public interface IGameFlowCommandExecutor
    {
        UniTask ExecuteAsync(string rawCommands, CancellationToken cancellationToken);
    }
}
