using System;
using KahaGameCore.UserInterfaceSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KahaGameCore.Package.GameFlowSystem.DefaultViews
{
    /// <summary>主標題畫面。僅負責顯示與轉發按鈕事件，不含流程邏輯。</summary>
    public class MainMenuView : AView
    {
        public event Action OnStartRequested;

        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button startButton;

        private void Awake()
        {
            startButton.onClick.AddListener(() => OnStartRequested?.Invoke());
        }

        public void SetTitle(string title)
        {
            titleText.text = title;
        }
    }
}
