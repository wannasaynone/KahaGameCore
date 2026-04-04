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

        private bool shouldSnap = false;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
            canvasGroup = GetComponent<CanvasGroup>();
        }
        
        private void OnEnable()
        {
            DialogueView.OnSpeedStateChanged += OnSpeedStateChanged;
        }

        private void OnSpeedStateChanged(DialogueView.SpeedState speedState)
        {
            if (speedState == DialogueView.SpeedState.Accelerated)
            {
                shouldSnap = true;
            }
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
            shouldSnap = false;

            if (fadeTime <= 0f)
            {
                canvasGroup.alpha = 1f;
                return;
            }

            canvasGroup.alpha = 0f;
            float speed = 1f / fadeTime;

            while (canvasGroup.alpha < 1f - Time.deltaTime * speed && !shouldSnap)
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
            shouldSnap = false;

            if (fadeTime <= 0f)
            {
                canvasGroup.alpha = 0f;
                return;
            }

            float speed = 1f / fadeTime;

            while (canvasGroup.alpha > Time.deltaTime * speed && !shouldSnap)
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
            DialogueView.OnSpeedStateChanged -= OnSpeedStateChanged;
            CancelFade();
        }
    }
}
