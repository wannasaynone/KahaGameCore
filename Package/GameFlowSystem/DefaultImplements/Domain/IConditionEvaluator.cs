namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 表格條件式求值。語法（詳見 Docs/資料表規格.md）：
    ///   比較式：$Supplies >= 200、$CurrentPhase == 5
    ///   邏輯組合：以 && 與 || 串接（&& 優先），不支援括號
    ///   空字串視為恆成立。
    /// </summary>
    public interface IConditionEvaluator
    {
        bool Evaluate(string condition);
    }
}
