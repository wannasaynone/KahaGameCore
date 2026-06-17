using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem
{
    /// <summary>一個玩家行動。實際定義（表格欄位等）由各專案的資料類別實作。</summary>
    public interface IGameFlowAction
    {
        int ID { get; }
        /// <summary>按鈕顯示名稱。</summary>
        string Name { get; }
        /// <summary>按鈕說明文字。</summary>
        string Description { get; }
        /// <summary>執行的效果指令串（EffectProcessor 語法）。</summary>
        string Commands { get; }
        /// <summary>按鈕的 UGUI 座標（anchoredPosition）。</summary>
        Vector2 AnchoredPosition { get; }
    }
}
