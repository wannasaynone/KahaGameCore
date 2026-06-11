using System;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KahaGameCore.Package.GameFlowSystem.Samples.Views
{
    /// <summary>移動選單上的單一地點按鈕。</summary>
    public class LocationButtonItem : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        public void Bind(LocationData location, Action<LocationData> onClicked)
        {
            nameText.text = location.Name;
            descriptionText.text = location.Description;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked?.Invoke(location));
        }
    }
}
