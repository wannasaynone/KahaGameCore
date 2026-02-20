using TMPro;
using UnityEngine;

namespace ProjectBSR.DialogueSystem.View
{
    public class DialogueView_OptionButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI optionText;

        private OptionData optionData;
        private System.Action<int> onSelected;

        public void Bind(OptionData optionData, System.Action<int> onOptionSelected)
        {
            optionText.text = optionData.text;
            onSelected = onOptionSelected;
            this.optionData = optionData;
        }

        public void OnClicked()
        {
            onSelected?.Invoke(optionData.targetLine);
        }
    }
}