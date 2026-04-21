using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace KahaGameCore.Audio
{
    public struct VolumeSnapshot
    {
        public float MasterVolume;
        public float BGMVolume;
        public float SFXVolume;
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource soundEffectSourcePrefab;
        [SerializeField] private AudioSource bgmAudioSource;
        [SerializeField] private AudioSource whiteNoiseAudio;
        [SerializeField] private float distanceVolumeMaxRange = 10f;
        [SerializeField] private float distanceVolumeLerpSpeed = 5f;

        private float masterVolume = 1f;
        private float bgmVolume = 0.5f;
        private float sfxVolume = 1f;

        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(masterVolume, clamped)) return;
                masterVolume = clamped;
                ApplyVolumeChange();
            }
        }

        public float BGMVolume
        {
            get => bgmVolume;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(bgmVolume, clamped)) return;
                bgmVolume = clamped;
                ApplyVolumeChange();
            }
        }

        public float SFXVolume
        {
            get => sfxVolume;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(sfxVolume, clamped)) return;
                sfxVolume = clamped;
                ApplyVolumeChange();
            }
        }

        public event Action<VolumeSnapshot> OnVolumeChanged;

        private float clearPlayingSourcesInterval = 0.05f;
        private float clearPlayingSourcesTimer = 0f;
        private List<AudioSource> playingSources = new List<AudioSource>();
        private List<AudioSource> allSpawnedSources = new List<AudioSource>();

        private class DistanceAudioSource
        {
            public AudioSource source;
            public Transform soundOrigin;
            public Transform listener;
            public float targetVolume;
        }

        private List<DistanceAudioSource> distanceAudioSources = new List<DistanceAudioSource>();

        [Serializable]
        private struct DistanceAudioDebugInfo
        {
            public string clipName;
            public string originName;
            public float distance;
            public float targetVolume;
            public float currentVolume;
        }

        [Header("Distance Audio Debug")]
        [SerializeField] private List<DistanceAudioDebugInfo> debugDistanceAudioInfo = new List<DistanceAudioDebugInfo>();

        public bool IsPlayingWhiteNoise => whiteNoiseAudio != null && whiteNoiseAudio.isPlaying;

        private Tween bgmFadeTween;

        private float EffectiveSFXVolume => sfxVolume * masterVolume;
        private float EffectiveBGMVolume => bgmVolume * masterVolume;

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

                // 清理已被銷毀的 AudioSource 引用，避免 allSpawnedSources 無限增長
                for (int i = allSpawnedSources.Count - 1; i >= 0; i--)
                {
                    if (allSpawnedSources[i] == null)
                    {
                        allSpawnedSources.RemoveAt(i);
                    }
                }
            }

            UpdateDistanceAudioSources();
        }

        private void UpdateDistanceAudioSources()
        {
            float effectiveSfx = EffectiveSFXVolume;

            debugDistanceAudioInfo.Clear();

            for (int i = distanceAudioSources.Count - 1; i >= 0; i--)
            {
                DistanceAudioSource das = distanceAudioSources[i];

                if (das.source == null || !das.source.isPlaying || das.soundOrigin == null || das.listener == null)
                {
                    if (das.source != null)
                    {
                        Destroy(das.source.gameObject);
                    }
                    distanceAudioSources.RemoveAt(i);
                    continue;
                }

                float distance = Vector2.Distance(das.soundOrigin.position, das.listener.position);
                das.targetVolume = Mathf.Clamp01(1f - distance / distanceVolumeMaxRange) * effectiveSfx;
                das.source.volume = Mathf.Lerp(das.source.volume, das.targetVolume, Time.deltaTime * distanceVolumeLerpSpeed);

                debugDistanceAudioInfo.Add(new DistanceAudioDebugInfo
                {
                    clipName = das.source.clip != null ? das.source.clip.name : "null",
                    originName = das.soundOrigin.name,
                    distance = distance,
                    targetVolume = das.targetVolume,
                    currentVolume = das.source.volume
                });
            }
        }

        private void ApplyVolumeChange()
        {
            // Update BGM volume
            SetBGMAudioSourceVolume(EffectiveBGMVolume);

            // Update white noise volume if playing
            if (IsPlayingWhiteNoise)
            {
                whiteNoiseAudio.volume = EffectiveSFXVolume;
            }

            // Update SFX volume for all currently playing sound effects
            foreach (var source in playingSources)
            {
                if (source != null)
                {
                    source.volume = EffectiveSFXVolume;
                }
            }

            OnVolumeChanged?.Invoke(new VolumeSnapshot
            {
                MasterVolume = masterVolume,
                BGMVolume = bgmVolume,
                SFXVolume = sfxVolume
            });
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

                if (playingSources[i].clip == audioClip && playingSources[i].volume >= 0.35f) // 避免爆音，當同一個clip已經有一個以上的實例在播放且音量較大時，不再播放新的實例
                {
                    Destroy(clone.gameObject);
                    return;
                }
            }

            playingSources.Add(clone);
            allSpawnedSources.Add(clone);

            clone.clip = audioClip;
            clone.volume = EffectiveSFXVolume;
            clone.PlayOneShot(audioClip);
            Destroy(clone.gameObject, audioClip.length);
        }

        public void ForcePlaySound(AudioClip audioClip)
        {
            if (audioClip == null)
            {
                Debug.Log("AudioManager: ForcePlaySound clip is null.");
                return;
            }

            AudioSource clone = Instantiate(soundEffectSourcePrefab, transform.position, Quaternion.identity);

            playingSources.Add(clone);
            allSpawnedSources.Add(clone);

            clone.clip = audioClip;
            clone.volume = EffectiveSFXVolume;
            clone.PlayOneShot(audioClip);
            Destroy(clone.gameObject, audioClip.length);
        }

        public void PlaySoundWithDistance(AudioClip audioClip, Transform soundOrigin, Transform listener)
        {
            if (audioClip == null)
            {
                Debug.Log("AudioManager: PlaySoundWithDistance clip is null.");
                return;
            }

            float distance = Vector2.Distance(soundOrigin.position, listener.position);
            float initialVolume = Mathf.Clamp01(1f - distance / distanceVolumeMaxRange) * EffectiveSFXVolume;

            if (initialVolume <= 0f)
            {
                return;
            }

            AudioSource clone = Instantiate(soundEffectSourcePrefab, soundOrigin.position, Quaternion.identity);

            for (int i = 0; i < playingSources.Count; i++)
            {
                if (playingSources[i] == null)
                {
                    playingSources.RemoveAt(i);
                    i--;
                    continue;
                }

                if (playingSources[i].clip == audioClip && playingSources[i].volume >= 0.35f) // 避免爆音，當同一個clip已經有一個以上的實例在播放且音量較大時，不再播放新的實例)
                {
                    Destroy(clone.gameObject);
                    return;
                }
            }

            playingSources.Add(clone);
            allSpawnedSources.Add(clone);

            clone.clip = audioClip;
            clone.volume = initialVolume;
            clone.Play();

            distanceAudioSources.Add(new DistanceAudioSource
            {
                source = clone,
                soundOrigin = soundOrigin,
                listener = listener,
                targetVolume = initialVolume
            });
        }

        public void ForceStopAllSounds()
        {
            foreach (var source in allSpawnedSources)
            {
                if (source != null)
                {
                    source.Stop();
                    Destroy(source.gameObject);
                }
            }

            foreach (var das in distanceAudioSources)
            {
                if (das.source != null)
                {
                    das.source.Stop();
                    Destroy(das.source.gameObject);
                }
            }

            allSpawnedSources.Clear();
            playingSources.Clear();
            distanceAudioSources.Clear();
            EnableWhiteNoise(false);
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.Log("AudioManager: PlayBGM clip is null.");
                return;
            }

            if (bgmAudioSource.clip == clip && bgmAudioSource.isPlaying)
            {
                return;
            }

            if (bgmFadeTween != null) bgmFadeTween.Kill();

            if (GetBGMAudioSourceVolume() <= 0.1f || !bgmAudioSource.isPlaying)
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
            bgmFadeTween = DOTween.To(GetBGMAudioSourceVolume, SetBGMAudioSourceVolume, EffectiveBGMVolume, 2f);
        }

        public void StopBGM()
        {
            if (bgmFadeTween != null) bgmFadeTween.Kill();
            bgmFadeTween = DOTween.To(GetBGMAudioSourceVolume, SetBGMAudioSourceVolume, 0f, 1f).OnComplete(delegate
            {
                bgmAudioSource.Stop();
                bgmAudioSource.clip = null;
            });
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
                DOTween.To(GetWhiteNoiseVolume, SetWhiteNoiseVolume, EffectiveSFXVolume, 1f);
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
