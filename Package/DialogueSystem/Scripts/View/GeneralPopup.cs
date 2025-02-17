using System.Collections;
using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public class GeneralPopup : MonoBehaviour
    {
        public static GeneralPopup Instance { get; private set; }

        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TMPro.TextMeshProUGUI messageText;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PopUp(string message, System.Action onCompleted)
        {
            messageText.text = message.Replace("\\n", "\n");
            popupRoot.SetActive(true);
            StartCoroutine(IEWaitClosePopup(onCompleted));
        }

        private IEnumerator IEWaitClosePopup(System.Action onCompleted)
        {
            yield return null; // Wait for one frame to make sure the popup is active

            while (!InputDetector.IsSelectingInView())
            {
                yield return null;
            }

            popupRoot.SetActive(false);
            onCompleted?.Invoke();
        }
    }
}