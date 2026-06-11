using KahaGameCore.GameData;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data
{
    /// <summary>
    /// 數值定義表（Google Sheet: GameValueData）。
    /// 定義遊戲中所有具名數值（物資、飽腹、精神、旗標……）的初始值與上下限。
    /// 條件式與指令中以 $Tag 引用這些數值。
    /// </summary>
    public class GameValueData : IGameData
    {
        public int ID { get; private set; }
        /// <summary>數值標籤（英文，條件式中以 $Tag 引用）。</summary>
        public string Tag { get; private set; }
        /// <summary>顯示名稱（HUD 用）。</summary>
        public string DisplayName { get; private set; }
        /// <summary>開新遊戲時的初始值。</summary>
        public int InitialValue { get; private set; }
        /// <summary>數值下限（含）。</summary>
        public int MinValue { get; private set; }
        /// <summary>數值上限（含）。</summary>
        public int MaxValue { get; private set; }
        /// <summary>1 = 顯示於 HUD 狀態列。</summary>
        public int ShowInHUD { get; private set; }
        /// <summary>每次自然推進時段（AdvanceTime）時扣除的量；0 = 不消耗。SetPhase 跳轉不扣。</summary>
        public int PhaseDecay { get; private set; }
        /// <summary>備註欄，僅供企劃閱讀。</summary>
        public string Note { get; private set; }
    }
}
