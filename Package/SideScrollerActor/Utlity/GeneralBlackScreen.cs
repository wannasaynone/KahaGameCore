using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KahaGameCore.Package.SideScrollerActor.Utlity
{
    public class GeneralBlackScreen : MonoBehaviour
    {
        public static GeneralBlackScreen Instance { get; private set; }

        [SerializeField] private Image generalBlackScreen;
        [SerializeField] private TextMeshProUGUI cutInText;
        [SerializeField] private Image storyCGImage;

        private void Awake()
        {
            generalBlackScreen.gameObject.SetActive(false);
            Instance = this;
        }

        public void FadeIn(TweenCallback onEnded)
        {
            generalBlackScreen.gameObject.SetActive(true);
            generalBlackScreen.color = Color.clear;
            generalBlackScreen.DOFade(1f, 0.5f).SetEase(Ease.Linear).OnComplete(onEnded);
        }

        public void FadeOut(TweenCallback onEnded)
        {
            generalBlackScreen.gameObject.SetActive(true);
            generalBlackScreen.color = Color.black;
            generalBlackScreen.DOFade(0f, 0.5f).SetEase(Ease.Linear).OnComplete(() =>
            {
                generalBlackScreen.gameObject.SetActive(false);
                onEnded?.Invoke();
            });
        }

        public void ShowCutInText(string text, TweenCallback onEnded)
        {
            cutInText.gameObject.SetActive(true);
            cutInText.text = text.Replace("\\n", "\n");
            cutInText.color = new Color(1f, 1f, 1f, 0f);
            cutInText.DOFade(1f, 0.5f).SetEase(Ease.Linear).OnComplete(onEnded);
        }

        public void HideCutInText(TweenCallback tweenCallback)
        {
            cutInText.DOKill();
            cutInText.gameObject.SetActive(true);
            cutInText.color = Color.white;
            cutInText.DOFade(0f, 0.5f).SetEase(Ease.Linear).OnComplete(() =>
            {
                cutInText.gameObject.SetActive(false);
                tweenCallback?.Invoke();
            });
        }

        public void ShowStoryCGImage(Sprite sprite, TweenCallback onEnded)
        {
            storyCGImage.gameObject.SetActive(true);
            storyCGImage.sprite = sprite;
            storyCGImage.color = new Color(1f, 1f, 1f, 0f);
            storyCGImage.DOFade(1f, 0.5f).SetEase(Ease.Linear).OnComplete(onEnded);
        }

        public void HideStoryCGImage(TweenCallback tweenCallback)
        {
            storyCGImage.DOKill();
            storyCGImage.gameObject.SetActive(true);
            storyCGImage.color = Color.white;
            storyCGImage.DOFade(0f, 0.5f).SetEase(Ease.Linear).OnComplete(() =>
            {
                storyCGImage.gameObject.SetActive(false);
                tweenCallback?.Invoke();
            });
        }
    }
}