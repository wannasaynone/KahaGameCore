using System;
using KahaGameCore.UserInterfaceSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KahaGameCore.Package.GameFlowSystem.Samples.Views
{
    /// <summary>
    /// 製作人員名單畫面（佔位版本：顯示文字，點擊結束）。
    /// 正式的捲動動畫請替換或擴充此 View 與 CreditsPerformance。
    /// </summary>
    public class CreditsView : AView
    {
        [SerializeField] private TextMeshProUGUI creditsText;
        [SerializeField] private Button finishButton;

        private Action onFinished;

        private void Awake()
        {
            finishButton.onClick.AddListener(() => onFinished?.Invoke());
        }

        public void Bind(string credits, Action onFinished)
        {
            creditsText.text = credits;
            this.onFinished = onFinished;
        }
    }
}
