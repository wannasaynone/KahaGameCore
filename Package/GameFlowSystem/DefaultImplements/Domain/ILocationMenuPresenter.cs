using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>顯示移動選單並等待玩家選擇；回傳 null 代表取消（返回）。</summary>
    public interface ILocationMenuPresenter
    {
        UniTask<LocationData> SelectLocationAsync(IReadOnlyList<LocationData> locations);
    }
}
