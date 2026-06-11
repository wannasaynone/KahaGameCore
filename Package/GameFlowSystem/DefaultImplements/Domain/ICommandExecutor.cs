using System;
using KahaGameCore.Package.GameFlowSystem;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 執行表格中的效果指令串（KahaGameCore EffectProcessor 語法）。
    /// 可省略時機區塊：寫「AddValue(Satiety,30);AdvanceTime()」會自動包成 Execute{...}。
    /// ExecuteAsync 繼承自 IGameFlowCommandExecutor。
    /// </summary>
    public interface ICommandExecutor : IGameFlowCommandExecutor
    {
        void Execute(string rawCommands, Action onCompleted);
    }
}
