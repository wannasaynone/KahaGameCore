using System;
using KahaGameCore.Package.GameFlowSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KahaGameCore.Package.GameFlowSystem.Samples.Views
{
    /// <summary>行動選單上的單一行動按鈕。</summary>
    public class ActionButtonItem : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        public void Bind(ActionMenuEntry entry, Action<ActionMenuEntry> onClicked)
        {
            nameText.text = entry.Action.Name;
            descriptionText.text = entry.Action.Description;
            button.interactable = entry.IsEnabled;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked?.Invoke(entry));
        }
    }
}
