using KahaGameCore.GameFlowSystem;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 時間流動服務。階段順序與換日規則完全由 TimePhaseData 表定義。
    /// CurrentPhase 由本服務持有；換日時只遞增 Day Parameter，並發佈 TimePhaseChangedEvent。
    /// ResetToFirstPhase / AdvanceTime 繼承自 IGameFlowTimeService。
    /// </summary>
    public interface ITimeService : IGameFlowTimeService
    {
        /// <summary>以具體表格型別覆蓋 IGameFlowTimeService.CurrentPhase，供 HUD 等取得完整欄位。</summary>
        new TimePhaseData CurrentPhase { get; }
        int CurrentDay { get; }

        /// <summary>直接跳到指定階段（依 Key），不換日。</summary>
        void SetPhase(string phaseKey);
    }
}
