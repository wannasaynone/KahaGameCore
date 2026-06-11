using KahaGameCore.Package.GameFlowSystem;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 在指定時機點檢查 GameEventTriggerData 表，依優先度依序執行命中的事件
    /// （前演出 → 劇情對話 → 效果指令 → 後演出）。
    /// 表中 Timing 可寫精確時機（如 PhaseStart:Morning），或以保留字 Any
    /// （如 PhaseStart:Any、AfterAction:Any）命中同類別的所有時機。
    /// RaiseTimingAsync 繼承自 IGameFlowEventTriggerService。
    /// </summary>
    public interface IGameEventTriggerService : IGameFlowEventTriggerService
    {
    }
}
