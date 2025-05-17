using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource soundEffectSourcePrefab;
        [SerializeField] private AudioSource bgmAudioSource;
        [SerializeField] private AudioSource whiteNoiseAudio;

        private float clearPlayingSourcesInterval = 0.05f;
        private float clearPlayingSourcesTimer = 0f;
        private List<AudioSource> playingSources = new List<AudioSource>();

        public bool IsPlayingWhiteNoise => whiteNoiseAudio != null && whiteNoiseAudio.isPlaying;

        private Tween bgmFadeTween;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            clearPlayingSourcesTimer += Time.deltaTime;
            if (clearPlayingSourcesTimer >= clearPlayingSourcesInterval)
            {
                playingSources.Clear();
                clearPlayingSourcesTimer = 0f;
            }
        }

        public void PlaySound(AudioClip audioClip)
        {
            if (audioClip == null)
            {
                Debug.Log("AudioManager: PlaySound clip is null.");
                return;
            }

            AudioSource clone = Instantiate(soundEffectSourcePrefab, transform.position, Quaternion.identity);

            for (int i = 0; i < playingSources.Count; i++)
            {
                if (playingSources[i] == null)
                {
                    playingSources.RemoveAt(i);
                    i--;
                    continue;
                }

                if (playingSources[i].clip == audioClip)
                {
                    Destroy(clone.gameObject);
                    return;
                }
            }

            playingSources.Add(clone);

            clone.clip = audioClip;
            clone.PlayOneShot(audioClip);
            Destroy(clone.gameObject, audioClip.length);
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.Log("AudioManager: PlayBGM clip is null.");
                return;
            }

            if (clip == bgmAudioSource.clip)
            {
                return;
            }

            if (bgmFadeTween != null) bgmFadeTween.Kill();

            if (GetBGMAudioSourceVolume() == 0f)
            {
                ForcePlay(clip);
                return;
            }

            bgmFadeTween = DOTween.To(GetBGMAudioSourceVolume, SetBGMAudioSourceVolume, 0f, 1f).OnComplete(delegate
            {
                ForcePlay(clip);
            });
        }

        private void ForcePlay(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogError("AudioManager: ForcePlay clip is null.");
                return;
            }

            bgmAudioSource.volume = 0f;
            bgmAudioSource.clip = clip;
            bgmAudioSource.Play();
            bgmFadeTween = DOTween.To(GetBGMAudioSourceVolume, SetBGMAudioSourceVolume, 0.1f, 1f);
        }

        public void StopBGM()
        {
            if (bgmFadeTween != null) bgmFadeTween.Kill();
            bgmFadeTween = DOTween.To(GetBGMAudioSourceVolume, SetBGMAudioSourceVolume, 0f, 1f);
        }

        private float GetBGMAudioSourceVolume()
        {
            return bgmAudioSource.volume;
        }

        public void SetBGMAudioSourceVolume(float volume)
        {
            bgmAudioSource.volume = volume;
        }

        public void EnableWhiteNoise(bool enable)
        {
            if (whiteNoiseAudio == null)
            {
                return;
            }

            if (enable)
            {
                if (!whiteNoiseAudio.isPlaying) whiteNoiseAudio.Play();
                whiteNoiseAudio.volume = 0;
                DOTween.To(GetWhiteNoiseVolume, SetWhiteNoiseVolume, 1f, 1f);
            }
            else
            {
                DOTween.To(GetWhiteNoiseVolume, SetWhiteNoiseVolume, 0f, 1f);
            }
        }

        private float GetWhiteNoiseVolume()
        {
            return whiteNoiseAudio.volume;
        }

        private void SetWhiteNoiseVolume(float volume)
        {
            whiteNoiseAudio.volume = volume;
        }

    }
}