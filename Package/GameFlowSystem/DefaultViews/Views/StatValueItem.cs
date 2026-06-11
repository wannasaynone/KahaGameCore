using TMPro;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem.DefaultViews
{
    /// <summary>HUD 狀態列上的單一數值項目（名稱 + 數值）。</summary>
    public class StatValueItem : MonoBehaviour
    {
        public string Tag { get; private set; }

        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI valueText;

        public void Bind(string tag, string displayName, int value)
        {
            Tag = tag;
            nameText.text = displayName;
            UpdateValue(value);
        }

        public void UpdateValue(int value)
        {
            valueText.text = value.ToString();
        }
    }
}
