using System.Collections.Generic;
using KahaGameCore.Package.GameFlowSystem;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 地點服務。目前地點寫入 $CurrentLocation，解鎖狀態由 $LocationUnlocked_{ID} 旗標控制。
    /// CurrentLocationID 繼承自 IGameFlowLocationService。
    /// </summary>
    public interface ILocationService : IGameFlowLocationService
    {
        LocationData CurrentLocation { get; }

        void MoveTo(int locationId);
        /// <summary>取得可在移動選單顯示的地點（ShowInMenu=1、條件成立、且非目前地點）。</summary>
        IReadOnlyList<LocationData> GetSelectableLocations();
    }
}
