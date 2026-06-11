namespace KahaGameCore.Package.GameFlowSystem
{
    /// <summary>一個時間階段。實際定義（表格欄位等）由各專案的資料類別實作。</summary>
    public interface IGameFlowTimePhase
    {
        int ID { get; }
        /// <summary>程式內部識別字（如 Morning），事件 Timing 以 PhaseStart:{Key} 引用。</summary>
        string Key { get; }
        /// <summary>true = 此階段開放玩家選擇行動；false = 觸發完事件後自動推進。</summary>
        bool AllowAction { get; }
    }
}
