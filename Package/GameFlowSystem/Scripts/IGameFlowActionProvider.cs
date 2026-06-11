using System.Collections.Generic;

namespace KahaGameCore.Package.GameFlowSystem
{
    /// <summary>提供目前地點與條件下可顯示的玩家行動清單。</summary>
    public interface IGameFlowActionProvider
    {
        IReadOnlyList<IGameFlowAction> GetVisibleActions(int locationId);
        bool IsEnabled(IGameFlowAction action);
    }
}
