using System.Collections;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Utlity
{
    public class GeneralHintDisplayer : MonoBehaviour
    {
        [SerializeField] private GeneralHintObject hintPrefab;

        private static GeneralHintDisplayer instance;

        public static GeneralHintDisplayer Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("GeneralHintDisplayer is in the scene.");
                    return null;
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null)
            {
                Debug.LogError("GeneralHintDisplayer is already in the scene.");
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        public void Create(string hintMessage, Vector3 position, Color textColor)
        {
            if (hintPrefab == null)
            {
                Debug.LogError("Hint prefab is not assigned in the inspector.");
                return;
            }
            StartCoroutine(IEHint(hintMessage, position, textColor));
        }

        private IEnumerator IEHint(string hintMessage, Vector3 position, Color textColor)
        {
            GeneralHintObject clone = Instantiate(hintPrefab, position, Quaternion.identity);
            clone.transform.SetParent(null);
            clone.transform.localScale = hintPrefab.transform.localScale;
            clone.Alpha = 0f;
            clone.Text = hintMessage;
            clone.TextColor = textColor;
            clone.gameObject.SetActive(true);

            while (clone.Alpha < 1f)
            {
                clone.Alpha += Time.deltaTime * 3f;
                clone.transform.position += 0.5f * Time.deltaTime * Vector3.up;
                yield return null;
            }

            while (clone.Alpha > 0f)
            {
                clone.Alpha -= Time.deltaTime * 3f;
                clone.transform.position += 0.5f * Time.deltaTime * Vector3.up;
                yield return null;
            }

            Destroy(clone.gameObject);
        }
    }
}