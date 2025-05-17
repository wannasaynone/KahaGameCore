using KahaGameCore.Package.SideScrollerActor.Utlity;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.View
{
    public class UserInterfaceContextSetter : MonoBehaviour
    {
        [SerializeField] private int contextID;

        private TMPro.TextMeshProUGUI contextText;

        private void OnEnable()
        {
            if (contextText == null)
            {
                contextText = GetComponent<TMPro.TextMeshProUGUI>();
                if (contextText == null)
                {
                    Debug.LogError("TextMeshProUGUI Text is not found in UserInterfaceContextSetter. Game Object: " + gameObject.name);
                    return;
                }
            }

            ContextHandler.Instance.OnLanguageChanged += UpdateContextText;
            UpdateContextText();
        }

        private void OnDisable()
        {
            ContextHandler.Instance.OnLanguageChanged -= UpdateContextText;
        }

        private void UpdateContextText()
        {
            contextText.text = ContextHandler.Instance.GetContext(contextID);
        }
    }
}