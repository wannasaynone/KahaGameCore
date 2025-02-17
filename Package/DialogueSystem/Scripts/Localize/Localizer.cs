using TMPro;
using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class Localizer : MonoBehaviour
    {
        [SerializeField] private int id;
        private TextMeshProUGUI text;

        private void Awake()
        {
            LanguageManager.Instance.OnLanguageChanged += UpdateLanguage;
            UpdateLanguage(LanguageManager.Instance.CurrentLanguage);
        }

        private void UpdateLanguage(Language language)
        {
            if (text == null)
            {
                text = GetComponent<TextMeshProUGUI>();
            }
            text.text = LanguageManager.Instance.GameStaticDataManager.GetGameData<LocalizeData>(id).GetLocalizedContent(language);
        }
    }
}