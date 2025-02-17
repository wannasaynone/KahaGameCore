using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace KahaGameCore.Package.DialogueSystem
{
    public class GeneralBlackScreen : MonoBehaviour
    {
        public static GeneralBlackScreen Instance { get; private set; }

        [SerializeField] private RawImage rootImage;

        private enum State
        {
            None,
            FadingIn,
            FadingOut
        }

        private State state = State.None;

        private void Awake()
        {
            Instance = this;
        }

        public void FadeIn(Action onComplete)
        {
            if (state != State.None)
                return;

            StartCoroutine(FadeInCoroutine(onComplete));
        }

        private IEnumerator FadeInCoroutine(Action onComplete)
        {
            state = State.FadingIn;
            rootImage.DOFade(1, 0.25f);
            yield return new WaitForSeconds(0.25f);
            state = State.None;
            onComplete?.Invoke();
        }

        public void FadeOut(Action onComplete)
        {
            if (state != State.None)
                return;

            StartCoroutine(FadeOutCoroutine(onComplete));
        }

        private IEnumerator FadeOutCoroutine(Action onComplete)
        {
            state = State.FadingOut;
            rootImage.DOFade(0, 0.5f);
            yield return new WaitForSeconds(0.5f);
            state = State.None;
            onComplete?.Invoke();
        }
    }
}