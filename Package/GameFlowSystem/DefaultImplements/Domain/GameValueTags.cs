namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// 系統流程運作必要的保留數值標籤。
    /// 其餘玩法數值（物資、飽腹……）全部定義於 GameValueData 表，程式中不得寫死。
    /// </summary>
    public static class GameValueTags
    {
        /// <summary>目前天數。</summary>
        public const string Day = "Day";
        /// <summary>目前時間階段（TimePhaseData 的 ID）。</summary>
        public const string CurrentPhase = "CurrentPhase";
        /// <summary>目前所在地點（LocationData 的 ID）。</summary>
        public const string CurrentLocation = "CurrentLocation";

        /// <summary>一次性事件完成旗標：EventDone_{事件ID}。</summary>
        public static string EventDone(int eventId) => "EventDone_" + eventId;
        /// <summary>地點解鎖旗標：LocationUnlocked_{地點ID}。</summary>
        public static string LocationUnlocked(int locationId) => "LocationUnlocked_" + locationId;
    }
}
