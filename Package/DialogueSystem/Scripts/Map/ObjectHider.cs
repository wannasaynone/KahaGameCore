using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public class ObjectHider : MonoBehaviour
    {
        [SerializeField] private int hideIfRead;

        private void OnEnable()
        {
            if (PlayerManager.Instance.Player == null)
            {
                KahaGameCore.Common.TimerManager.Schedule(0.1f, OnEnable);
                return;
            }

            Player.OnDialogueRead += OnDialogueRead;
            OnDialogueRead(0); // 0 is a dummy value
        }

        private void OnDialogueRead(int obj)
        {
            if (PlayerManager.Instance.Player.HasReadDialogue(hideIfRead))
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            Player.OnDialogueRead -= OnDialogueRead;
        }
    }
}