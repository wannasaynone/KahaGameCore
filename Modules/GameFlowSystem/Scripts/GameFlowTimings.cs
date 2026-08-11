namespace KahaGameCore.GameFlowSystem
{
    /// <summary>
    /// 事件觸發表 Timing 欄位字串。
    /// 流程系統在這些時機點呼叫 IGameFlowEventTriggerService.RaiseTimingAsync。
    /// </summary>
    public static class GameFlowTimings
    {
        /// <summary>開新遊戲後、進入第一個時間階段前（開場劇情）。</summary>
        public const string GameStart = "GameStart";

        /// <summary>每個階段都會先觸發的共通時機。</summary>
        public const string PhaseStart = "PhaseStart";

        /// <summary>進入時間階段時，例如 PhaseStart:Morning。</summary>
        public static string PhaseStartFor(string phaseKey) => "PhaseStart:" + phaseKey;

        /// <summary>玩家行動自己的 TriggerTiming 完成後觸發的共通時機。</summary>
        public const string AfterAction = "AfterAction";

        /// <summary>移動到新地點後，例如 EnterLocation:2。</summary>
        public static string EnterLocation(int locationId) => "EnterLocation:" + locationId;
    }
}
