using FateTea;
using UnityEngine;
using System.Collections;
using System;

namespace KahaGameCore.Common
{
    public class AdvertisementManager : Interface.Manager
    {
        public event Action OnLoadingAd = null;
        
        private const float TIME_OUT_TIME = 15f;

        public static AdmobManager admobManager = null;

        public static AdvertisementManager Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new AdvertisementManager();
                    GameObject admobGameobject = new GameObject
                    {
                        name = "AdmobManager"
                    };
                    admobManager = admobGameobject.AddComponent<AdmobManager>();
                }
                return m_instance;
            }
        }
        private static AdvertisementManager m_instance = null;

        public void Init()
        {
            // do nothing, AdmobManager will rest of things.
        }

        public void ShowAD(Action onADCompleted, Action onFailed)
        {
            GameUtility.CheckConnection
            (
                delegate (bool havingConn)
                {
                    if (havingConn)
                    {
                        waitADLoadingTimer = TIME_OUT_TIME;
                        admobManager.ClearAllReward();
                        if (onADCompleted != null)
                        {
                            admobManager.GetAdReward += onADCompleted;
                        }
                        Static.GeneralCoroutineRunner.Instance.StartCoroutine(IEWaitADLoading(onFailed));
                    }
                    else
                    {
                        if(onFailed != null)
                        {
                            onFailed();
                        }
                    }
                }
            );
        }

        public void ShowInterstitialVideoAd(Action onADCompleted, Action onFailed)
        {
            GameUtility.CheckConnection
            (
                delegate (bool havingConn)
                {
                    if (havingConn)
                    {
                        interstitialAdLoadingTimer = TIME_OUT_TIME;
                        admobManager.ClearAllReward();
                        if (onADCompleted != null)
                        {
                            admobManager.InvokeInterstitialAdAfterEvent += onADCompleted;
                        }
                        KahaGameCore.Static.GeneralCoroutineRunner.Instance.StartCoroutine(IEWaitInterstitialAdLoading(onFailed));
                    }
                    else
                    {
                        if (onFailed != null)
                        {
                            onFailed();
                        }
                    }
                }
            );
        }

        private float waitADLoadingTimer = 0f;
        private IEnumerator IEWaitADLoading(Action onFailed)
        {
            if(OnLoadingAd != null)
            {
                OnLoadingAd();
            }
            if (IsRewardVideoAdLoaded())
            {
                admobManager.ShowRewardVideoAd();
            }
            else
            {
                yield return null;
                waitADLoadingTimer -= Time.deltaTime;
                if(waitADLoadingTimer < 0f)
                {
                    if(onFailed != null)
                    {
                        onFailed();
                    }
                }
                else
                {
                    Static.GeneralCoroutineRunner.Instance.StartCoroutine(IEWaitADLoading(onFailed));
                }
            }
        }

        private float interstitialAdLoadingTimer = 0f;
        private IEnumerator IEWaitInterstitialAdLoading(System.Action onFailed)
        {
            if (OnLoadingAd != null)
            {
                OnLoadingAd();
            }
            if (IsInterstitialVideoAdLoaded())
            {
                admobManager.ShowInterstitialAdmob();
            }
            else
            {
                yield return null;
                interstitialAdLoadingTimer -= Time.deltaTime;

                if(interstitialAdLoadingTimer <= 0f)
                {
                    if (onFailed != null)
                    {
                        onFailed();
                    }
                }
                else
                {
                    KahaGameCore.Static.GeneralCoroutineRunner.Instance.StartCoroutine(IEWaitInterstitialAdLoading(onFailed));
                }
            }
        }

        // 開關是否看廣告.
        public void EnableAd(bool enable)
        {
            admobManager.showAd = enable;
        }

        // 取得獎勵廣告是否讀取完成.
        public bool IsRewardVideoAdLoaded()
        {
            return admobManager.IsRewardVideoAdLoaded();
        }

        // 取得插頁廣告是否讀取完成.
        public bool IsInterstitialVideoAdLoaded()
        {
            return admobManager.IsInterstitialVideoAdLoaded();
        }
    }
}
