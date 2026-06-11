using KahaGameCore.GameData;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data
{
    /// <summary>
    /// 事件觸發表（Google Sheet: GameEventTriggerData）。
    /// 整個遊戲的劇情流程由本表驅動：在指定時機（Timing）檢查條件（Condition），
    /// 依序執行 前演出 → 劇情對話 → 效果指令 → 後演出。
    /// </summary>
    public class GameEventTriggerData : IGameData
    {
        public int ID { get; private set; }
        /// <summary>
        /// 觸發時機字串。系統會在以下時機點發出檢查：
        /// GameStart / PhaseStart:{階段Key} / AfterAction:{行動ID} / EnterLocation:{地點ID}
        /// </summary>
        public string Timing { get; private set; }
        /// <summary>觸發條件（條件式語法，空白 = 恆成立）。</summary>
        public string Condition { get; private set; }
        /// <summary>同時機多事件命中時的執行優先度（大到小）。</summary>
        public int Priority { get; private set; }
        /// <summary>1 = 一生只觸發一次（觸發後自動記錄 EventDone_{ID} = 1）。</summary>
        public int OneTime { get; private set; }
        /// <summary>觸發的劇情對話 ID（DialogueData 表），0 = 無對話。</summary>
        public int DialogueID { get; private set; }
        /// <summary>對話前播放的演出 ID（預留給 UGUI 動畫串接），空白 = 無。</summary>
        public string PrePerformance { get; private set; }
        /// <summary>對話與指令結束後播放的演出 ID，空白 = 無。</summary>
        public string PostPerformance { get; private set; }
        /// <summary>對話結束後執行的效果指令串（EffectProcessor 語法）。</summary>
        public string Commands { get; private set; }
        /// <summary>備註欄，僅供企劃閱讀。</summary>
        public string Note { get; private set; }
    }
}
