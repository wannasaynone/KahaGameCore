using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public class Teleporter : MonoBehaviour
    {
        public static event System.Action OnTeleported;

        [SerializeField] private Transform targetPosition;

        private Transform enterTransform;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.name.Equals("Player"))
            {
                enterTransform = other.transform;
                InputDetector.LockMovement(this);
                GeneralBlackScreen.Instance.FadeIn(OnFadeIn);
            }
        }

        private void OnFadeIn()
        {
            enterTransform.position = targetPosition.position;
            OnTeleported?.Invoke();
            Invoke(nameof(StartFadeOut), 0.25f);
        }

        private void StartFadeOut()
        {
            GeneralBlackScreen.Instance.FadeOut(OnFadeOut);
        }

        private void OnFadeOut()
        {
            InputDetector.UnlockMovement(this);
        }
    }
}