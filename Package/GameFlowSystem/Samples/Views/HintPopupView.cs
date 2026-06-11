using System;
using KahaGameCore.UserInterfaceSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KahaGameCore.Package.GameFlowSystem.Samples.Views
{
    /// <summary>提示視窗：顯示一段文字並等待玩家按下確認。</summary>
    public class HintPopupView : AView
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button confirmButton;

        private Action onConfirmed;

        private void Awake()
        {
            confirmButton.onClick.AddListener(() => onConfirmed?.Invoke());
        }

        public void Bind(string message, Action onConfirmed)
        {
            messageText.text = message;
            this.onConfirmed = onConfirmed;
        }
    }
}
