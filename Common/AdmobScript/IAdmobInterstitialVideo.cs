using System;

namespace FateTea
{
    public interface IAdmobInterstitialVideo
    {
        event Action InvokeInterstitialAdBeforeEvent;   // 播廣告之前發生的事件(To-do 播放廣告前先記得關BGM).
        event Action InvokeInterstitialAdAfterEvent;    // 播廣告之後發生的事件(To-do 這裡要重新開啟你的BGM).

        void ShowInterstitialAdmob();                   // 開啟插頁式廣告.
        bool IsInterstitialVideoAdLoaded();             // 插頁式廣告是否已讀取完成.
        bool IsInterstitialVideoAdPlaying();            // 插頁式廣告是否播放中.
    }
}