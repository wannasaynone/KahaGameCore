using TMPro;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Utlity
{
    public class GeneralHintObject : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI textMeshProUGUI;

        public float Alpha
        {
            get => canvasGroup.alpha;
            set => canvasGroup.alpha = value;
        }

        public string Text
        {
            get => textMeshProUGUI.text;
            set => textMeshProUGUI.text = value;
        }

        public Color TextColor
        {
            get => textMeshProUGUI.color;
            set => textMeshProUGUI.color = value;
        }
    }
}