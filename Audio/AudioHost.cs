using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Audio
{
    /// <summary>
    /// Owns the AudioSources behind <see cref="AudioManager"/>. Created by it;
    /// nothing else should add this component.
    /// </summary>
    internal sealed class AudioHost : MonoBehaviour
    {
        // Same clip within this window is dropped, so stacked hits do not clip.
        private const float SfxRepeatGuardSeconds = 0.05f;

        private sealed class BgmChannel
        {
            public AudioSource Source;
            public float Level;
            public float Target;
            public float Speed;
        }

        private readonly BgmChannel[] bgm = new BgmChannel[2];
        private int currentBgm;
        private readonly List<AudioSource> sfxPool = new List<AudioSource>();
        private readonly List<AudioSource> loops = new List<AudioSource>();
        private readonly Dictionary<AudioClip, float> sfxLastPlayTime = new Dictionary<AudioClip, float>();

        internal AudioSource CurrentBgmSource => bgm[currentBgm].Source;
        internal AudioSource OtherBgmSource => bgm[1 - currentBgm].Source;
        internal IReadOnlyList<AudioSource> SfxPool => sfxPool;

        internal void Initialize()
        {
            for (int i = 0; i < bgm.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.volume = 0f;
                bgm[i] = new BgmChannel { Source = source };
            }
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        internal void Tick(float deltaTime)
        {
            float bgmVolume = AudioManager.GetEffectiveVolume(AudioChannel.Bgm);
            foreach (BgmChannel channel in bgm)
            {
                channel.Level = Mathf.MoveTowards(channel.Level, channel.Target, channel.Speed * deltaTime);
                channel.Source.volume = channel.Level * bgmVolume;

                if (channel.Level <= 0f && channel.Target <= 0f && channel.Source.clip != null)
                {
                    channel.Source.Stop();
                    channel.Source.clip = null;
                }
            }
        }

        internal void PlayBgm(AudioClip clip, float fadeSeconds)
        {
            BgmChannel current = bgm[currentBgm];
            if (current.Source.clip == clip)
            {
                // Already playing (or fading out after StopBgm): bring it back.
                FadeTo(current, 1f, fadeSeconds);
                return;
            }

            FadeTo(current, 0f, fadeSeconds);

            currentBgm = 1 - currentBgm;
            BgmChannel next = bgm[currentBgm];
            next.Source.clip = clip;
            next.Level = 0f;
            next.Source.Play();
            FadeTo(next, 1f, fadeSeconds);
            Tick(0f);
        }

        internal void StopBgm(float fadeSeconds)
        {
            foreach (BgmChannel channel in bgm)
            {
                FadeTo(channel, 0f, fadeSeconds);
            }

            Tick(0f);
        }

        private static void FadeTo(BgmChannel channel, float target, float fadeSeconds)
        {
            channel.Target = target;
            if (fadeSeconds <= 0f)
            {
                channel.Level = target;
                channel.Speed = 0f;
            }
            else
            {
                channel.Speed = 1f / fadeSeconds;
            }
        }

        internal void PlaySfx(AudioClip clip, bool spatial, Vector3 position)
        {
            float now = Time.realtimeSinceStartup;
            if (sfxLastPlayTime.TryGetValue(clip, out float last) && now - last < SfxRepeatGuardSeconds)
            {
                return;
            }

            sfxLastPlayTime[clip] = now;

            AudioSource source = GetFreeSfxSource();
            source.transform.position = position;
            ConfigureSpatial(source, spatial);
            source.clip = clip;
            source.volume = AudioManager.GetEffectiveVolume(AudioChannel.Sfx);
            source.Play();
        }

        internal AudioLoop PlayLoop(AudioClip clip, Transform owner, bool spatial)
        {
            var loopObject = new GameObject($"[AudioLoop] {clip.name}");
            loopObject.transform.SetParent(owner, false);

            AudioSource source = loopObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            ConfigureSpatial(source, spatial);
            source.clip = clip;
            source.volume = AudioManager.GetEffectiveVolume(AudioChannel.Sfx);
            source.Play();

            loops.RemoveAll(s => s == null);
            loops.Add(source);
            return new AudioLoop(source);
        }

        internal void StopAllSfx()
        {
            foreach (AudioSource source in sfxPool)
            {
                source.Stop();
                source.clip = null;
            }

            foreach (AudioSource loop in loops)
            {
                if (loop != null)
                {
                    Destroy(loop.gameObject);
                }
            }

            loops.Clear();
            sfxLastPlayTime.Clear();
        }

        internal void ApplyVolumes()
        {
            Tick(0f);

            float sfxVolume = AudioManager.GetEffectiveVolume(AudioChannel.Sfx);
            foreach (AudioSource source in sfxPool)
            {
                source.volume = sfxVolume;
            }

            loops.RemoveAll(s => s == null);
            foreach (AudioSource loop in loops)
            {
                loop.volume = sfxVolume;
            }
        }

        private AudioSource GetFreeSfxSource()
        {
            foreach (AudioSource source in sfxPool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            var sourceObject = new GameObject($"SFX {sfxPool.Count}");
            sourceObject.transform.SetParent(transform, false);
            AudioSource created = sourceObject.AddComponent<AudioSource>();
            created.playOnAwake = false;
            sfxPool.Add(created);
            return created;
        }

        private static void ConfigureSpatial(AudioSource source, bool spatial)
        {
            source.spatialBlend = spatial ? 1f : 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = AudioManager.SpatialMinDistance;
            source.maxDistance = AudioManager.SpatialMaxDistance;
        }
    }
}
