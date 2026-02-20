using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ProjectBSR.DialogueSystem
{
    public class AudioManager : MonoBehaviour
    {
        private AudioSource bgmSource;
        private readonly List<AudioSource> seSourcePool = new List<AudioSource>();
        private const int INITIAL_SE_POOL_SIZE = 3;

        private Tweener bgmVolumeTweener;
        private float seVolume = 1f;
        private Tweener seVolumeTweener;
        private readonly Dictionary<AudioClip, float> seLastPlayTimeMap = new Dictionary<AudioClip, float>();
        private const float SE_COOLDOWN = 0.05f;

        private void Awake()
        {
            InitializeAudioSources();
        }

        private void InitializeAudioSources()
        {
            GameObject bgmObject = new GameObject("BGM_AudioSource");
            bgmObject.transform.SetParent(transform);
            bgmSource = bgmObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;

            for (int i = 0; i < INITIAL_SE_POOL_SIZE; i++)
            {
                CreateSESource();
            }
        }

        private AudioSource CreateSESource()
        {
            GameObject seObject = new GameObject($"SE_AudioSource_{seSourcePool.Count}");
            seObject.transform.SetParent(transform);
            AudioSource source = seObject.AddComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;
            source.volume = seVolume;
            seSourcePool.Add(source);
            return source;
        }

        private AudioSource GetAvailableSESource()
        {
            for (int i = 0; i < seSourcePool.Count; i++)
            {
                if (!seSourcePool[i].isPlaying)
                    return seSourcePool[i];
            }

            return CreateSESource();
        }

        public void PlaySE(AudioClip clip)
        {
            if (clip == null)
                return;

            if (seLastPlayTimeMap.TryGetValue(clip, out float lastTime) && Time.time - lastTime < SE_COOLDOWN)
                return;

            seLastPlayTimeMap[clip] = Time.time;

            AudioSource source = GetAvailableSESource();
            source.clip = clip;
            source.volume = seVolume;
            source.Play();
        }

        public void PlayBGM(AudioClip clip, float fadeInDuration = 0.5f)
        {
            if (clip == null)
                return;

            KillBGMTweener();

            bgmSource.clip = clip;

            if (fadeInDuration <= 0f)
            {
                bgmSource.volume = 1f;
                bgmSource.Play();
                return;
            }

            bgmSource.volume = 0f;
            bgmSource.Play();
            bgmVolumeTweener = DOTween.To(() => bgmSource.volume, x => bgmSource.volume = x, 1f, fadeInDuration).SetEase(Ease.Linear);
        }

        public void StopBGM(float fadeOutDuration = 0.5f)
        {
            KillBGMTweener();

            if (fadeOutDuration <= 0f)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
                return;
            }

            bgmVolumeTweener = DOTween.To(() => bgmSource.volume, x => bgmSource.volume = x, 0f, fadeOutDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    bgmSource.Stop();
                    bgmSource.clip = null;
                });
        }

        public void SetBGMVolume(float targetVolume, float duration = 0.5f)
        {
            targetVolume = Mathf.Clamp01(targetVolume);
            KillBGMTweener();

            if (duration <= 0f)
            {
                bgmSource.volume = targetVolume;
                return;
            }

            bgmVolumeTweener = DOTween.To(() => bgmSource.volume, x => bgmSource.volume = x, targetVolume, duration).SetEase(Ease.Linear);
        }

        public void SetSEVolume(float targetVolume, float duration = 0.5f)
        {
            targetVolume = Mathf.Clamp01(targetVolume);
            KillSETweener();

            if (duration <= 0f)
            {
                seVolume = targetVolume;
                ApplySEVolume();
                return;
            }

            seVolumeTweener = DOTween.To(() => seVolume, x =>
            {
                seVolume = x;
                ApplySEVolume();
            }, targetVolume, duration).SetEase(Ease.Linear);
        }

        private void ApplySEVolume()
        {
            for (int i = 0; i < seSourcePool.Count; i++)
            {
                seSourcePool[i].volume = seVolume;
            }
        }

        public float GetBGMVolume()
        {
            return bgmSource.volume;
        }

        public float GetSEVolume()
        {
            return seVolume;
        }

        public bool IsBGMPlaying()
        {
            return bgmSource.isPlaying;
        }

        private void KillBGMTweener()
        {
            if (bgmVolumeTweener != null && bgmVolumeTweener.IsActive())
            {
                bgmVolumeTweener.Kill();
                bgmVolumeTweener = null;
            }
        }

        private void KillSETweener()
        {
            if (seVolumeTweener != null && seVolumeTweener.IsActive())
            {
                seVolumeTweener.Kill();
                seVolumeTweener = null;
            }
        }

        private void OnDestroy()
        {
            KillBGMTweener();
            KillSETweener();
        }
    }
}
