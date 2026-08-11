using UnityEngine;

namespace KahaGameCore.GameFlowSystem
{
    /// <summary>一個玩家行動。實際定義（表格欄位等）由各專案的資料類別實作。</summary>
    public interface IGameFlowAction
    {
        int ID { get; }
        /// <summary>按鈕顯示名稱。</summary>
        string Name { get; }
        /// <summary>按鈕說明文字。</summary>
        string Description { get; }
        /// <summary>選擇此行動時交給 Game Event runner 的時機名稱。</summary>
        string TriggerTiming { get; }
        /// <summary>按鈕的 UGUI 座標（anchoredPosition）。</summary>
        Vector2 AnchoredPosition { get; }
        /// <summary>所屬選單群組；空字串 = 根選單。子選單依此欄位篩選成員。</summary>
        string MenuGroup { get; }
    }
}
