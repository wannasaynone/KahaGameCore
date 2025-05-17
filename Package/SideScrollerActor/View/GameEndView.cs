using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using TMPro;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.View
{
    public class GameEndView : MonoBehaviour
    {
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        public RectTransform LoseCGRoot => loseCGRoot;
        [SerializeField] private RectTransform loseCGRoot;
        [SerializeField] private TextMeshProUGUI addDarkCurrencyText;
        [SerializeField] private TextMeshProUGUI addLightCurrencyText;

        private void OnEnable()
        {
            winPanel.SetActive(false);
            losePanel.SetActive(false);
        }

        public void ShowWinPanel()
        {
            winPanel.SetActive(true);
        }

        public void ShowLosePanel()
        {
            losePanel.SetActive(true);
        }

        public void SetAddDarkCurrency(int value)
        {
            addDarkCurrencyText.text = value.ToString("N0");
        }

        public void SetAddLightCurrency(int value)
        {
            addLightCurrencyText.text = value.ToString("N0");
        }

        public void Button_Comfirm()
        {
            EventBus.Publish(new GameEndView_OnConfirmClicked());
        }
    }
}