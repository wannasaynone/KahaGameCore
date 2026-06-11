using KahaGameCore.Package.GameFlowSystem;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 時間流動服務。階段順序與換日規則完全由 TimePhaseData 表定義。
    /// 異動時發佈 TimePhaseChangedEvent，並同步寫入 $CurrentPhase / $Day 供條件式引用。
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
