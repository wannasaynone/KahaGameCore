namespace KahaGameCore.Package.GameFlowSystem
{
    /// <summary>時間流動服務。階段順序與換日規則由實作方（通常是表格）定義。</summary>
    public interface IGameFlowTimeService
    {
        IGameFlowTimePhase CurrentPhase { get; }

        /// <summary>重設到第一個階段（開新遊戲）。</summary>
        void ResetToFirstPhase();
        /// <summary>推進到下一個階段。</summary>
        void AdvanceTime();
    }
}
