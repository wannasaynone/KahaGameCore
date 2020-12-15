using System;
using UnityEngine;
#if ENABLE_AD
using GoogleMobileAds.Api;
#endif

namespace FateTea
{
	public class AdmobManager : MonoBehaviour, IAdmobRewardVideo, IAdmobInterstitialVideo
    {
        public bool showAd = true;          // 是否顯示廣告(false將會直接得到獎勵).

        [SerializeField] private string ANDROID_ADMOB_REWARD_ID = "ca-app-pub-3940256099942544/5224354917";         // 獎勵廣告ID.
        [SerializeField] private string IOS_ADMOB_IOS_REWARD_ID = "ca-app-pub-3940256099942544/5224354917";
        [SerializeField] private string ANDROID_ADMOB_INTERSTITIAL_ID = "ca-app-pub-3940256099942544/1033173712";   // 插頁廣告ID.
        [SerializeField] private string IOS_ADMOB_IOS_INTERSTITIAL_ID = "ca-app-pub-3940256099942544/1033173712";

        /* 獎勵廣告 */
        public event Action GetAdReward;
        public event Action InvokeRewardAdBeforeEvent;
        public event Action InvokeRewardAdAfterEvent;
        public event Action InvokeNotReceivedAdEvent;

#if ENABLE_AD
        private RewardBasedVideoAd rewardBasedVideo;
		private AdRequest rewardRequest = null;
#endif
		private string adRewardVideoUnitId = "";        // 獎勵廣告admob碼.
        private bool isRewardVideoClosed = false;       // 檢測獎勵廣告是否開啟播完後關閉.
        private bool isRewarded = false;	            // 檢測是否看完獎勵廣告得到獎勵.
        private bool isRewardVideoPlaying = false;      // 獎勵廣告是否播放中.

        /* 插入廣告 */
        public event Action InvokeInterstitialAdBeforeEvent;
        public event Action InvokeInterstitialAdAfterEvent;
#if ENABLE_AD
        private InterstitialAd interstitial;
        private AdRequest interstitialRequest = null;
#endif
        private string adInterstitialUnitId = "";       // 插入廣告admob碼
        private bool isInterstitialPlaying = false;     // 廣告是否播放中.

        private void Awake()
        {
            InitRewardBasedVideoAd();
            InitInterstitialAd();
        }

        private void Update()
        {
            if (isRewardVideoClosed)
            {
                isRewardVideoPlaying = false;
                HandleRewardBasedVideoClosedEvent();

                if (isRewarded)
                {
                    GetReward();
                    isRewarded = false;
                }
                isRewardVideoClosed = false;
            }
        }

        public void ClearAllReward()
        {
            GetAdReward = null;
            InvokeInterstitialAdAfterEvent = null;
        }

        public void ShowRewardVideoAd()
        {
#if UNITY_EDITOR
            GetReward();
#else
            if (showAd)
            {
                if (IsRewardVideoAdLoaded())
                {
                    if (InvokeRewardAdBeforeEvent != null)
                        InvokeRewardAdBeforeEvent.Invoke();

                    rewardBasedVideo.Show();
                }
                else
                {
                    if (InvokeNotReceivedAdEvent != null)
                        InvokeNotReceivedAdEvent.Invoke();
                }
            }
            else
                GetReward();
#endif
        }

        public bool IsRewardVideoAdLoaded()
        {
#if ENABLE_AD
            return rewardBasedVideo.IsLoaded();
#else
            return false;
#endif
        }

        public bool IsRewardVideoAdPlaying()
        {
            return isRewardVideoPlaying;
        }

        public void ShowInterstitialAdmob()
        {
#if UNITY_EDITOR
            if(InvokeInterstitialAdAfterEvent != null)
            {
                InvokeInterstitialAdAfterEvent();
            }
#else
            if (showAd && IsInterstitialVideoAdLoaded())
            {
                if (InvokeInterstitialAdBeforeEvent != null)
                    InvokeInterstitialAdBeforeEvent.Invoke();

                interstitial.Show();
            }
#endif
        }

        public bool IsInterstitialVideoAdLoaded()
        {
#if ENABLE_AD
            return interstitial.IsLoaded();
#else
            return false;
#endif
        }

        public bool IsInterstitialVideoAdPlaying()
        {
            return isInterstitialPlaying;
        }

        // 初始化獎勵廣告宣告.
        private void InitRewardBasedVideoAd()
        {
#if UNITY_ANDROID
            adRewardVideoUnitId = ANDROID_ADMOB_REWARD_ID;
#elif UNITY_IPHONE
			adRewardVideoUnitId = IOS_ADMOB_IOS_REWARD_ID;
#else
            adRewardVideoUnitId = "unexpected_platform";
#endif

#if ENABLE_AD
            MobileAds.Initialize(adRewardVideoUnitId);

            rewardBasedVideo = RewardBasedVideoAd.Instance;

            rewardBasedVideo.OnAdFailedToLoad += HandleRewardBasedVideoFailedToLoad;
            rewardBasedVideo.OnAdStarted += HandleRewardBasedVideoStarted;
            rewardBasedVideo.OnAdRewarded += HandleRewardBasedVideoRewarded;
            rewardBasedVideo.OnAdClosed += HandleRewardBasedVideoClosed;
#endif

            RequestRewardBasedVideo();
        }

#if ENABLE_AD
        // 廣告讀取失敗.
        private void HandleRewardBasedVideoFailedToLoad(object sender, AdFailedToLoadEventArgs args)
        {
            rewardBasedVideo.LoadAd(rewardRequest, adRewardVideoUnitId);
        }
#endif

        // 廣告開始播放.
        private void HandleRewardBasedVideoStarted(object sender, EventArgs args)
        {
            isRewardVideoPlaying = true;
        }

#if ENABLE_AD
        // 獲得獎勵.
        private void HandleRewardBasedVideoRewarded(object sender, GoogleMobileAds.Api.Reward args)
        {
            isRewarded = true;
        }
#endif

        // 廣告結束.
        private void HandleRewardBasedVideoClosed(object sender, EventArgs args)
        {
            isRewardVideoClosed = true;
        }
		
		// 廣告被關閉觸發事件.
		private void HandleRewardBasedVideoClosedEvent()
		{
            if (InvokeRewardAdAfterEvent != null)
                InvokeRewardAdAfterEvent.Invoke();

#if ENABLE_AD
            rewardBasedVideo.LoadAd(rewardRequest, adRewardVideoUnitId);
#endif
        }

		// 廣告獎勵.
		private void GetReward()
		{
            if (GetAdReward != null)
                GetAdReward.Invoke();
#if ENABLE_AD
            rewardBasedVideo.LoadAd(rewardRequest, adRewardVideoUnitId);
#endif
		}

		// 請求廣告內容.
		private void RequestRewardBasedVideo()
		{
#if ENABLE_AD
#if UNITY_EDITOR
            string testDeviceID = AdCommon.DeviceIdForAdmob;
            rewardRequest = new AdRequest.Builder ().AddTestDevice (testDeviceID).Build ();
#else
			rewardRequest = new AdRequest.Builder().Build();
#endif
            if (rewardRequest != null)
				rewardBasedVideo.LoadAd(rewardRequest, adRewardVideoUnitId);
#endif
		}

        // 初始化插頁廣告宣告.
        private void InitInterstitialAd()
        {
#if ENABLE_AD
#if UNITY_ANDROID
            adInterstitialUnitId = ANDROID_ADMOB_INTERSTITIAL_ID;
#elif UNITY_IPHONE
			adInterstitialUnitId = IOS_ADMOB_IOS_INTERSTITIAL_ID;
#else
			adInterstitialUnitId = "unexpected_platform";
#endif
            interstitial = new InterstitialAd(adInterstitialUnitId);

            interstitial.OnAdOpening += HandleOnAdOpened;
            interstitial.OnAdClosed += HandleOnAdClosed;
#endif
            RequestInterstitialVideo();
        }

        // 請求廣告內容.
        private void RequestInterstitialVideo()
        {
#if ENABLE_AD
#if UNITY_EDITOR
            string testDeviceID = AdCommon.DeviceIdForAdmob;
            interstitialRequest = new AdRequest.Builder().AddTestDevice(testDeviceID).Build();
#else
			interstitialRequest = new AdRequest.Builder().Build();
#endif
            if (interstitialRequest != null)
                interstitial.LoadAd(interstitialRequest);
#endif
        }

        private void HandleOnAdOpened(object sender, EventArgs args)
        {
            isInterstitialPlaying = true;
        }

        private void HandleOnAdClosed(object sender, EventArgs args)
        {
            isInterstitialPlaying = false;

            if (InvokeInterstitialAdAfterEvent != null)
                InvokeInterstitialAdAfterEvent.Invoke();

#if ENABLE_AD
            // for iOS can't play AD issue, work around lol
            interstitial = new InterstitialAd(adInterstitialUnitId);

            interstitial.OnAdOpening += HandleOnAdOpened;
            interstitial.OnAdClosed += HandleOnAdClosed;
# endif
            RequestInterstitialVideo();
        }
    }
}