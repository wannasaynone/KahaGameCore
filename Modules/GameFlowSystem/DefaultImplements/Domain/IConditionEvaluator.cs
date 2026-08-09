namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 表格條件式求值。語法（詳見 Docs/資料表規格.md）：
    ///   比較式：$Supplies >= 200、$Day >= 2
    ///   邏輯組合：支援括號、!、&& 與 ||（&& 優先）。
    ///   空字串視為恆成立；語法或未知符號錯誤不得默認為 false。
    /// </summary>
    public interface IConditionEvaluator
    {
        bool Evaluate(string condition);
    }
}
