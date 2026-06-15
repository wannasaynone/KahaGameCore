using KahaGameCore.GameData;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data
{
    /// <summary>
    /// 地點表（Google Sheet: LocationData）。
    /// 地點解鎖透過 `SetValue(LocationUnlocked_{ID},1)` 寫入數值，再由 VisibleCondition 判斷。
    /// </summary>
    public class LocationData : IGameData
    {
        public int ID { get; private set; }
        /// <summary>地點名稱。</summary>
        public string Name { get; private set; }
        /// <summary>地點說明。</summary>
        public string Description { get; private set; }
        /// <summary>出現在移動選單中的條件（條件式語法，空白 = 永遠顯示）。</summary>
        public string VisibleCondition { get; private set; }
        /// <summary>1 = 顯示於移動選單；0 = 不顯示（如「家中」需透過「返回家中」行動回家以推進時間）。</summary>
        public int ShowInMenu { get; private set; }
        /// <summary>此地點的背景貼圖 Addressables address（空字串 = 不切換背景，維持目前畫面）。</summary>
        public string Background { get; private set; }
        /// <summary>清單排序（小到大）。</summary>
        public int SortOrder { get; private set; }
        /// <summary>備註欄，僅供企劃閱讀。</summary>
        public string Note { get; private set; }
    }
}
