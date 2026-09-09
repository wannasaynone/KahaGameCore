using System.Globalization;
using UnityEngine;

namespace KahaGameCore.Common
{
    /// <summary>
    /// 把數字解析鎖在 InvariantCulture。
    /// 對話指令參數、參數表、存檔裡的數字都是資料而非顯示內容，一律以 "." 當小數點撰寫，
    /// 不該跟著玩家 OS 的地區設定跑：de-DE 會把 "0.5" 讀成 5，fr-FR 則解析失敗靜默落回 0。
    /// </summary>
    public static class InvariantCultureBootstrap
    {
        // ponytail: 靠 attribute 觸發，EditMode 測試不會跑到。
        // 之後若要替 Dialogue command 寫 EditMode 測試，測試需自行呼叫 Apply()，
        // 否則在歐陸 locale 的機器或 CI 上會紅。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Apply()
        {
            // CurrentCulture 蓋已經在跑的主執行緒，
            // DefaultThreadCurrentCulture 蓋之後才建立的執行緒。
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }
    }
}
