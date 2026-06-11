namespace KahaGameCore.Package.GameFlowSystem
{
    /// <summary>
    /// 事件觸發表 Timing 欄位字串。
    /// 流程系統在這些時機點呼叫 IGameFlowEventTriggerService.RaiseTimingAsync。
    /// </summary>
    public static class GameFlowTimings
    {
        /// <summary>開新遊戲後、進入第一個時間階段前（開場劇情）。</summary>
        public const string GameStart = "GameStart";

        /// <summary>進入時間階段時，例如 PhaseStart:Morning。</summary>
        public static string PhaseStart(string phaseKey) => "PhaseStart:" + phaseKey;

        /// <summary>玩家行動執行完畢後，例如 AfterAction:106。</summary>
        public static string AfterAction(int actionId) => "AfterAction:" + actionId;

        /// <summary>移動到新地點後，例如 EnterLocation:2。</summary>
        public static string EnterLocation(int locationId) => "EnterLocation:" + locationId;
    }
}
