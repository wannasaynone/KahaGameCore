using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.Audio.Tests
{
    public sealed class AudioManagerTests
    {
        private float savedMaster;
        private float savedBgm;
        private float savedSfx;
        private AudioClip clipA;
        private AudioClip clipB;

        [SetUp]
        public void SetUp()
        {
            AudioManager.ResetStatics();
            savedMaster = AudioManager.GetVolume(AudioChannel.Master);
            savedBgm = AudioManager.GetVolume(AudioChannel.Bgm);
            savedSfx = AudioManager.GetVolume(AudioChannel.Sfx);
            AudioManager.SetVolume(AudioChannel.Master, 1f);
            AudioManager.SetVolume(AudioChannel.Bgm, 1f);
            AudioManager.SetVolume(AudioChannel.Sfx, 1f);

            clipA = AudioClip.Create("A", 44100, 1, 44100, false);
            clipB = AudioClip.Create("B", 44100, 1, 44100, false);
        }

        [TearDown]
        public void TearDown()
        {
            // Volumes live in PlayerPrefs: put the player's values back.
            AudioManager.SetVolume(AudioChannel.Master, savedMaster);
            AudioManager.SetVolume(AudioChannel.Bgm, savedBgm);
            AudioManager.SetVolume(AudioChannel.Sfx, savedSfx);
            AudioManager.ResetStatics();
            Object.DestroyImmediate(clipA);
            Object.DestroyImmediate(clipB);
        }

        [Test]
        public void PlayBgm_SameClipTwice_KeepsPlayingOnOneSource()
        {
            AudioManager.PlayBgm(clipA, 1f);
            AudioSource first = AudioManager.Host.CurrentBgmSource;

            AudioManager.PlayBgm(clipA, 1f);

            Assert.AreSame(first, AudioManager.Host.CurrentBgmSource);
            Assert.AreSame(clipA, first.clip);
            Assert.IsNull(AudioManager.Host.OtherBgmSource.clip);
        }

        [Test]
        public void PlayBgm_NewClip_CrossfadesAndStopsTheOld()
        {
            AudioManager.PlayBgm(clipA, 1f);
            AudioManager.Host.Tick(1f);
            AudioSource oldSource = AudioManager.Host.CurrentBgmSource;

            AudioManager.PlayBgm(clipB, 1f);
            AudioManager.Host.Tick(0.5f);
            Assert.AreEqual(0.5f, oldSource.volume, 0.001f);
            Assert.AreEqual(0.5f, AudioManager.Host.CurrentBgmSource.volume, 0.001f);

            AudioManager.Host.Tick(0.5f);
            Assert.IsNull(oldSource.clip);
            Assert.AreSame(clipB, AudioManager.Host.CurrentBgmSource.clip);
            Assert.AreEqual(1f, AudioManager.Host.CurrentBgmSource.volume, 0.001f);
        }

        [Test]
        public void SetVolume_DuringBgmFadeIn_FadeEndsAtNewVolume()
        {
            AudioManager.PlayBgm(clipA, 1f);
            AudioManager.Host.Tick(0.5f);

            AudioManager.SetVolume(AudioChannel.Bgm, 0.2f);
            AudioManager.Host.Tick(1f);

            Assert.AreEqual(0.2f, AudioManager.Host.CurrentBgmSource.volume, 0.001f);
        }

        [Test]
        public void SetVolume_Master_ScalesBgmAndSfx()
        {
            AudioManager.PlayBgm(clipA, 0f);
            AudioManager.PlaySfx(clipB);

            AudioManager.SetVolume(AudioChannel.Sfx, 0.5f);
            AudioManager.SetVolume(AudioChannel.Master, 0.5f);

            Assert.AreEqual(0.5f, AudioManager.Host.CurrentBgmSource.volume, 0.001f);
            Assert.AreEqual(0.25f, AudioManager.Host.SfxPool[0].volume, 0.001f);
        }

        [Test]
        public void SetVolume_ClampsAndReportsOnlyItsChannel()
        {
            AudioChannel? reported = null;
            float reportedVolume = -1f;
            AudioManager.VolumeChanged += (channel, volume) =>
            {
                reported = channel;
                reportedVolume = volume;
            };

            AudioManager.SetVolume(AudioChannel.Sfx, -3f);

            Assert.AreEqual(AudioChannel.Sfx, reported);
            Assert.AreEqual(0f, reportedVolume);
            Assert.AreEqual(0f, AudioManager.GetVolume(AudioChannel.Sfx));
        }
    }
}
