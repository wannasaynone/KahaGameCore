using KahaGameCore.StaticData;
using KahaGameCore.GameFlowSystem;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Data
{
    /// <summary>
    /// 時間階段表（Google Sheet: TimePhaseData）。
    /// 定義一天內的時間流動順序，流程不寫死、完全由本表驅動。
    /// </summary>
    public class TimePhaseData : IGameData, IGameFlowTimePhase
    {
        public int ID { get; private set; }
        /// <summary>程式內部識別字（如 Morning），事件表 Timing 欄位以 PhaseStart:{Key} 引用。</summary>
        public string Key { get; private set; }
        /// <summary>顯示於 HUD 的名稱（如 早晨）。</summary>
        public string DisplayName { get; private set; }
        /// <summary>推進時間後進入的下一個階段 ID。</summary>
        public int NextID { get; private set; }
        /// <summary>1 = 進入此階段時天數 +1（換日）。</summary>
        public int IsNewDay { get; private set; }
        /// <summary>備註欄，僅供企劃閱讀。</summary>
        public string Note { get; private set; }

    }
}
