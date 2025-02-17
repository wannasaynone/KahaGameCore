using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public class UserInterfaceManager : MonoBehaviour
    {
        public static UserInterfaceManager Instance { get; private set; }

        public DialogueView DialogueView { get { return dialogueView; } }
        [SerializeField] private DialogueView dialogueView;

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
    }
}