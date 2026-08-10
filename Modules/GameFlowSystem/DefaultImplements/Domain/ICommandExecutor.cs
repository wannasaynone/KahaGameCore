using System;
using KahaGameCore.GameFlowSystem;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 執行表格中的效果指令串（KahaGameCore EffectRuntime 語法）。
    /// 可省略時機區塊：寫「AddParameter(Satiety,30);AdvanceTime()」會自動包成 Execute{...}。
    /// ExecuteAsync 繼承自 IGameFlowCommandExecutor。
    /// </summary>
    public interface ICommandExecutor : IGameFlowCommandExecutor
    {
        void Execute(string rawCommands, Action onCompleted);
    }
}
