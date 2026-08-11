using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 執行表格中的效果指令串（KahaGameCore EffectRuntime 語法）。
    /// 可省略時機區塊：寫「AddParameter(Satiety,30);AdvancePhase()」會自動包成 Execute{...}。
    /// 供 Dialogue bridge 與其他 DefaultImplements integration 重用同一個 EffectRuntime。
    /// </summary>
    public interface ICommandExecutor
    {
        void Execute(string rawCommands, Action onCompleted);
        UniTask ExecuteAsync(string rawCommands, CancellationToken cancellationToken);
    }
}
