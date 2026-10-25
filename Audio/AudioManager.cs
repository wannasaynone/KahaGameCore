using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("KahaGameCore.Audio.Tests")]

namespace KahaGameCore.Audio
{
    public enum AudioChannel
    {
        Master,
        Bgm,
        Sfx
    }

    /// <summary>
    /// The single entry point for playing sound. The first call creates a
    /// persistent host object, so any Scene can play sound without setup, and
    /// BGM carries on across Scene loads. Volumes are player settings: they are
    /// saved to PlayerPrefs and survive <see cref="Reset"/>.
    /// </summary>
    public static class AudioManager
    {
        private const string VolumePrefsKeyPrefix = "KahaGameCore.Audio.Volume.";
        private const float DefaultVolume = 1f;

        private static AudioHost host;
        private static float[] volumes;

        /// <summary>Fires with the channel whose own volume changed and its new value.</summary>
        public static event Action<AudioChannel, float> VolumeChanged;

        /// <summary>
        /// Distance at which positional sound starts to fade. The default
        /// covers a 2D camera sitting 10 units in front of the scene.
        /// </summary>
        public static float SpatialMinDistance { get; set; } = 10f;

        /// <summary>Distance at which positional sound becomes silent.</summary>
        public static float SpatialMaxDistance { get; set; } = 25f;

        internal static AudioHost Host
        {
            get
            {
                if (host == null)
                {
                    var hostObject = new GameObject("[AudioManager]");
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.DontDestroyOnLoad(hostObject);
                    }

                    host = hostObject.AddComponent<AudioHost>();
                    host.Initialize();
                }

                return host;
            }
        }

        public static float GetVolume(AudioChannel channel)
        {
            LoadVolumesIfNeeded();
            return volumes[(int)channel];
        }

        public static void SetVolume(AudioChannel channel, float volume)
        {
            LoadVolumesIfNeeded();
            float clamped = Mathf.Clamp01(volume);
            if (Mathf.Approximately(volumes[(int)channel], clamped))
            {
                return;
            }

            volumes[(int)channel] = clamped;
            PlayerPrefs.SetFloat(VolumePrefsKeyPrefix + channel, clamped);

            if (host != null)
            {
                host.ApplyVolumes();
            }

            VolumeChanged?.Invoke(channel, clamped);
        }

        /// <summary>Channel volume multiplied by Master.</summary>
        internal static float GetEffectiveVolume(AudioChannel channel)
        {
            float master = GetVolume(AudioChannel.Master);
            return channel == AudioChannel.Master ? master : GetVolume(channel) * master;
        }

        /// <summary>
        /// Crossfades to <paramref name="clip"/>. Asking for the clip that is
        /// already playing keeps it going instead of restarting it.
        /// </summary>
        public static void PlayBgm(AudioClip clip, float fadeSeconds = 1f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] PlayBgm: clip is null.");
                return;
            }

            Host.PlayBgm(clip, fadeSeconds);
        }

        public static void StopBgm(float fadeSeconds = 1f)
        {
            if (host != null)
            {
                host.StopBgm(fadeSeconds);
            }
        }

        /// <summary>Plays a non-positional one-shot (UI, hit feedback).</summary>
        public static void PlaySfx(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] PlaySfx: clip is null.");
                return;
            }

            Host.PlaySfx(clip, spatial: false, Vector3.zero);
        }

        /// <summary>Plays a one-shot at a world position.</summary>
        public static void PlaySfx(AudioClip clip, Vector3 position)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] PlaySfx: clip is null.");
                return;
            }

            Host.PlaySfx(clip, spatial: true, position);
        }

        /// <summary>
        /// Loops <paramref name="clip"/> on a child of <paramref name="owner"/>.
        /// The loop follows the owner and ends when the owner is destroyed,
        /// so it lives and dies with the owner's Scene.
        /// </summary>
        public static AudioLoop PlayLoop(AudioClip clip, Transform owner, bool spatial = true)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] PlayLoop: clip is null.");
                return null;
            }

            if (owner == null)
            {
                Debug.LogWarning($"[AudioManager] PlayLoop: owner is null for clip '{clip.name}'.");
                return null;
            }

            return Host.PlayLoop(clip, owner, spatial);
        }

        /// <summary>Stops every one-shot and loop on the SFX channel.</summary>
        public static void StopAllSfx()
        {
            if (host != null)
            {
                host.StopAllSfx();
            }
        }

        /// <summary>
        /// Stops all sound at once. Call when starting a new game or returning
        /// to the title. Volumes are kept.
        /// </summary>
        public static void Reset()
        {
            if (host != null)
            {
                host.StopBgm(0f);
                host.StopAllSfx();
            }
        }

        private static void LoadVolumesIfNeeded()
        {
            if (volumes != null)
            {
                return;
            }

            AudioChannel[] channels = (AudioChannel[])Enum.GetValues(typeof(AudioChannel));
            volumes = new float[channels.Length];
            foreach (AudioChannel channel in channels)
            {
                volumes[(int)channel] = PlayerPrefs.GetFloat(VolumePrefsKeyPrefix + channel, DefaultVolume);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetStatics()
        {
            // Statics survive entering Play Mode when domain reload is disabled.
            if (host != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(host.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(host.gameObject);
                }
            }

            host = null;
            volumes = null;
            VolumeChanged = null;
        }
    }

    /// <summary>Handle to a sound started by <see cref="AudioManager.PlayLoop"/>.</summary>
    public sealed class AudioLoop
    {
        internal AudioLoop(AudioSource source)
        {
            Source = source;
        }

        internal AudioSource Source { get; }

        /// <summary>False once stopped or once its owner is destroyed.</summary>
        public bool IsAlive => Source != null;

        public void Stop()
        {
            if (Source != null)
            {
                UnityEngine.Object.Destroy(Source.gameObject);
            }
        }
    }
}
