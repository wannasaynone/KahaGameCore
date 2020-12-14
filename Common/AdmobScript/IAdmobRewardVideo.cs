using System;

namespace FateTea
{
    public interface IAdmobRewardVideo
    {
        event Action GetAdReward;                   // 得到獎勵用此註冊.
        event Action InvokeRewardAdBeforeEvent;     // 播廣告之前發生的事件(To-do 播放廣告前先記得關BGM).
        event Action InvokeRewardAdAfterEvent;      // 播廣告之後發生的事件(To-do 這裡要重新開啟你的BGM).
        event Action InvokeNotReceivedAdEvent;      // 沒有讀取到廣告時的事件.

        void ClearAllReward();                      // 清空獎勵Action.
        void ShowRewardVideoAd();                   // 顯示獎勵式廣告.
        bool IsRewardVideoAdLoaded();               // 獎勵式廣告是否已讀取完成.
        bool IsRewardVideoAdPlaying();              // 獎勵式廣告是否播放中.
    }
}