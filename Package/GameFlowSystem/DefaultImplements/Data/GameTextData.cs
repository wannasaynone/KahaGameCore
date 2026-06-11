using KahaGameCore.GameData;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data
{
    /// <summary>
    /// 遊戲文字表（Google Sheet: GameTextData）。
    /// 提示視窗文字（ShowHint 指令以 ID 引用）與隨機自言自語（Monologue 指令以 Group 抽選）。
    /// </summary>
    public class GameTextData : IGameData
    {
        public int ID { get; private set; }
        /// <summary>群組名稱（Monologue 指令依群組隨機抽選，提示文字可留空白群組）。</summary>
        public string Group { get; private set; }
        /// <summary>抽選/顯示條件（條件式語法，空白 = 恆成立）。</summary>
        public string Condition { get; private set; }
        /// <summary>文字內容。</summary>
        public string Text { get; private set; }
        /// <summary>隨機抽選權重（預設 1）。</summary>
        public int Weight { get; private set; }
        /// <summary>備註欄，僅供企劃閱讀。</summary>
        public string Note { get; private set; }
    }
}
