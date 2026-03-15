using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBSR.DialogueSystem.View
{
    [RequireComponent(typeof(RawImage), typeof(CanvasGroup))]
    public class CharacterDisplayer : MonoBehaviour
    {
        private RawImage rawImage;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private CancellationTokenSource fadeCts;
        private CancellationTokenSource moveCts;
        private CancellationTokenSource scaleCts;

        private Vector2 defaultAnchoredPosition;
        private Vector3 defaultLocalScale;

        private const float JUMP_HEIGHT = 50f;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();

            defaultAnchoredPosition = rectTransform.anchoredPosition;
            defaultLocalScale = rectTransform.localScale;

            canvasGroup.alpha = 0f;
        }

        public void SetTexture(Texture2D texture)
        {
            rawImage.texture = texture;
            rawImage.SetNativeSize();
        }

        public void SetPositionOffsetX(float offsetX)
        {
            rectTransform.anchoredPosition = new Vector2(defaultAnchoredPosition.x + offsetX, defaultAnchoredPosition.y);
        }

        public void ResetToDefault()
        {
            rectTransform.anchoredPosition = defaultAnchoredPosition;
            rectTransform.localScale = defaultLocalScale;
        }

        #region Fade

        public async UniTask FadeIn(float fadeTime)
        {
            CancelFade();
            fadeCts = new CancellationTokenSource();

            canvasGroup.alpha = 0f;

            if (fadeTime <= 0f)
            {
                canvasGroup.alpha = 1f;
                return;
            }

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

        #endregion

        #region Move

        public async UniTask MoveX(float addX, float moveTime)
        {
            CancelMove();
            moveCts = new CancellationTokenSource();

            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 targetPos = new Vector2(startPos.x + addX, startPos.y);

            if (moveTime <= 0f)
            {
                rectTransform.anchoredPosition = targetPos;
                return;
            }

            float elapsed = 0f;

            while (elapsed < moveTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveTime);
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                await UniTask.Yield(moveCts.Token);
            }

            rectTransform.anchoredPosition = targetPos;
        }

        public async UniTask MoveY(float addY, float moveTime)
        {
            CancelMove();
            moveCts = new CancellationTokenSource();

            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 targetPos = new Vector2(startPos.x, startPos.y + addY);

            if (moveTime <= 0f)
            {
                rectTransform.anchoredPosition = targetPos;
                return;
            }

            float elapsed = 0f;

            while (elapsed < moveTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveTime);
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                await UniTask.Yield(moveCts.Token);
            }

            rectTransform.anchoredPosition = targetPos;
        }

        public async UniTask Jump(float totalTime)
        {
            CancelMove();
            moveCts = new CancellationTokenSource();

            Vector2 startPos = rectTransform.anchoredPosition;

            if (totalTime <= 0f)
            {
                return;
            }

            float elapsed = 0f;

            while (elapsed < totalTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / totalTime);
                // Parabola: 0 -> 1 -> 0, peak at t=0.5
                float yOffset = JUMP_HEIGHT * 4f * t * (1f - t);
                rectTransform.anchoredPosition = new Vector2(startPos.x, startPos.y + yOffset);
                await UniTask.Yield(moveCts.Token);
            }

            rectTransform.anchoredPosition = startPos;
        }

        private void CancelMove()
        {
            if (moveCts != null)
            {
                moveCts.Cancel();
                moveCts.Dispose();
                moveCts = null;
            }
        }

        #endregion

        #region Scale

        public async UniTask ScaleTo(float targetScale, float scaleTime)
        {
            CancelScale();
            scaleCts = new CancellationTokenSource();

            Vector3 startScale = rectTransform.localScale;
            Vector3 endScale = new Vector3(targetScale, targetScale, 1f);

            if (scaleTime <= 0f)
            {
                rectTransform.localScale = endScale;
                return;
            }

            float elapsed = 0f;

            while (elapsed < scaleTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / scaleTime);
                rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
                await UniTask.Yield(scaleCts.Token);
            }

            rectTransform.localScale = endScale;
        }

        private void CancelScale()
        {
            if (scaleCts != null)
            {
                scaleCts.Cancel();
                scaleCts.Dispose();
                scaleCts = null;
            }
        }

        #endregion

        private void OnDestroy()
        {
            CancelFade();
            CancelMove();
            CancelScale();
        }
    }
}