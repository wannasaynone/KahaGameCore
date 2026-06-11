using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace KahaGameCore.Package.GameFlowSystem
{
    /// <summary>行動選單的一個項目。</summary>
    public class ActionMenuEntry
    {
        public IGameFlowAction Action { get; }
        public bool IsEnabled { get; }

        public ActionMenuEntry(IGameFlowAction action, bool isEnabled)
        {
            Action = action;
            IsEnabled = isEnabled;
        }
    }

    /// <summary>顯示行動選單並等待玩家選擇（流程層只依賴介面，不接觸 UGUI）。回傳 null 表示流程被中止。</summary>
    public interface IActionMenuPresenter
    {
        UniTask<IGameFlowAction> SelectActionAsync(IReadOnlyList<ActionMenuEntry> entries);
    }
}
