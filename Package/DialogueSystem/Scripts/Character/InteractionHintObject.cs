using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public class InteractionHintObject : MonoBehaviour
    {
        [SerializeField] private GameObject hintObject;
        [SerializeField] private int skipIfReadID;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.name.Equals("Player") && !PlayerManager.Instance.Player.HasReadDialogue(skipIfReadID))
            {
                hintObject.SetActive(true);
            }
        }

        private void Update()
        {
            if (InputDetector.IsInteracting() && hintObject.activeSelf)
            {
                hintObject.SetActive(false);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.name.Equals("Player"))
            {
                hintObject.SetActive(false);
            }
        }
    }
}