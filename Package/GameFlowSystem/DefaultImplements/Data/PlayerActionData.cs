using KahaGameCore.GameData;
using KahaGameCore.Package.GameFlowSystem;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data
{
    /// <summary>
    /// 玩家行動表（Google Sheet: PlayerActionData）。
    /// 行動清單依目前地點與條件動態產生，新增行動只需加表，不需改程式。
    /// </summary>
    public class PlayerActionData : IGameData, IGameFlowAction
    {
        public int ID { get; private set; }
        /// <summary>按鈕顯示名稱。</summary>
        public string Name { get; private set; }
        /// <summary>按鈕說明文字。</summary>
        public string Description { get; private set; }
        /// <summary>可出現的地點 ID，逗號分隔（如 "1" 或 "2,3,4"）。</summary>
        public string Locations { get; private set; }
        /// <summary>顯示條件（條件式語法，空白 = 永遠顯示）。</summary>
        public string VisibleCondition { get; private set; }
        /// <summary>可點擊條件（不符合時按鈕反灰）。</summary>
        public string EnableCondition { get; private set; }
        /// <summary>執行的效果指令串（EffectProcessor 語法）。</summary>
        public string Commands { get; private set; }
        /// <summary>清單排序（小到大）。</summary>
        public int SortOrder { get; private set; }
        /// <summary>備註欄，僅供企劃閱讀。</summary>
        public string Note { get; private set; }
    }
}
