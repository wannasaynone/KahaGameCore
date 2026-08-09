using System.Collections.Generic;
using KahaGameCore.GameFlowSystem;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 地點服務自持目前地點；可見性條件可讀取 LocationUnlocked_{ID} Parameter。
    /// CurrentLocationID 繼承自 IGameFlowLocationService。
    /// </summary>
    public interface ILocationService : IGameFlowLocationService
    {
        LocationData CurrentLocation { get; }

        void ResetToInitial();
        void MoveTo(int locationId);
        /// <summary>取得可在移動選單顯示的地點（ShowInMenu=1、條件成立、且非目前地點）。</summary>
        IReadOnlyList<LocationData> GetSelectableLocations();
    }
}
