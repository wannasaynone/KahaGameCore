using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KahaGameCore.UserInterfaceSystem
{
    public abstract class AView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float transitionDuration = 0.5f;

        public async Task Show(CancellationToken token)
        {
            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;

            float transitionTimer = 0f;

            while (transitionTimer < transitionDuration)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                transitionTimer += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(transitionTimer / transitionDuration);
                await UniTask.Yield(token);
            }

            canvasGroup.alpha = 1f;
        }

        public virtual BackButtonResult OnBackButtonPressed()
        {
            return BackButtonResult.Close;
        }

        public async Task Hide(CancellationToken token)
        {
            canvasGroup.alpha = 1f;

            float transitionTimer = 0f;

            while (transitionTimer < transitionDuration)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                transitionTimer += Time.deltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(transitionTimer / transitionDuration);
                await UniTask.Yield(token);
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}