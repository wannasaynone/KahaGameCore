using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBSR.DialogueSystem.View
{
    [RequireComponent(typeof(RawImage), typeof(CanvasGroup))]
    public class CGLayer : MonoBehaviour
    {
        private RawImage rawImage;
        private CanvasGroup canvasGroup;
        private CancellationTokenSource fadeCts;

        public string CGName { get; private set; }

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Setup(Texture2D texture, string cgName)
        {
            CGName = cgName;
            rawImage.texture = texture;
            rawImage.SetNativeSize();
            canvasGroup.alpha = 0f;
        }

        public async UniTask FadeIn(float fadeTime)
        {
            CancelFade();
            fadeCts = new CancellationTokenSource();

            if (fadeTime <= 0f)
            {
                canvasGroup.alpha = 1f;
                return;
            }

            canvasGroup.alpha = 0f;
            float speed = 1f / fadeTime;

            while (canvasGroup.alpha < 1f - Time.deltaTime * speed)
            {
                canvasGroup.alpha += Time.deltaTime * speed;
                await UniTask.Yield(fadeCts.Token);
            }

            canvasGroup.alpha = 1f;
        }

        public async UniTask FadeOut(float fadeTime)
        {
            CancelFade();
            fadeCts = new CancellationTokenSource();

            if (fadeTime <= 0f)
            {
                canvasGroup.alpha = 0f;
                return;
            }

            float speed = 1f / fadeTime;

            while (canvasGroup.alpha > Time.deltaTime * speed)
            {
                canvasGroup.alpha -= Time.deltaTime * speed;
                await UniTask.Yield(fadeCts.Token);
            }

            canvasGroup.alpha = 0f;
        }

        private void CancelFade()
        {
            if (fadeCts != null)
            {
                fadeCts.Cancel();
                fadeCts.Dispose();
                fadeCts = null;
            }
        }

        private void OnDestroy()
        {
            CancelFade();
        }
    }
}
