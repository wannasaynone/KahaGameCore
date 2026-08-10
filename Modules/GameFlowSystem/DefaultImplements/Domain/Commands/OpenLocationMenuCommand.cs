using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.GameFlowSystem.DefaultImplements;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>
    /// OpenLocationMenu()：開啟移動選單讓玩家選擇地點；取消則不移動。
    /// 實際的 EnterLocation 事件由流程層在指令串結束後統一觸發。
    /// </summary>
    public class OpenLocationMenuCommand : IEffectCommand
    {
        private readonly ILocationService locationService;
        private readonly ILocationMenuPresenter locationMenuPresenter;

        public OpenLocationMenuCommand(ILocationService locationService, ILocationMenuPresenter locationMenuPresenter)
        {
            this.locationService = locationService;
            this.locationMenuPresenter = locationMenuPresenter;
        }

        public async UniTask ExecuteAsync(
            EffectExecutionContext context,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LocationData selected = await locationMenuPresenter
                .SelectLocationAsync(locationService.GetSelectableLocations())
                .AttachExternalCancellation(cancellationToken);
            if (selected != null)
            {
                locationService.MoveTo(selected.ID);
            }
        }
    }
}
