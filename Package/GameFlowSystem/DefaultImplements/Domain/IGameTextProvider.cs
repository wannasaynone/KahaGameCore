using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>提供 GameTextData 表的查詢與隨機抽選。</summary>
    public interface IGameTextProvider
    {
        /// <summary>依 ID 取得文字，找不到時回傳錯誤標記字串。</summary>
        string GetText(int textId);
        /// <summary>依群組與條件加權隨機抽選一筆，無符合者回傳 null。</summary>
        GameTextData PickRandom(string group);
    }
}
