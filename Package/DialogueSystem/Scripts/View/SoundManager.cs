using DG.Tweening;
using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSourcePrefab;


        private AudioClip currentBGMClip;
        private bool isSyncingBGMVolume = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PlayBGM(AudioClip bgmClip)
        {
            if (currentBGMClip == bgmClip)
            {
                return;
            }

            if (currentBGMClip != bgmClip)
            {
                isSyncingBGMVolume = false;
                DOTween.KillAll();
                DOTween.To(GetBGMVolume, SetBGMVolume, 0, 0.5f).OnComplete(() =>
                {
                    currentBGMClip = bgmClip;
                    bgmSource.clip = bgmClip;
                    bgmSource.loop = true;
                    bgmSource.Play();
                    DOTween.To(GetBGMVolume, SetBGMVolume, PlayerPrefs.GetFloat("BGMVolume", 1f), 0.5f).OnComplete(() =>
                    {
                        isSyncingBGMVolume = true;
                    });
                });
                return;
            }

            currentBGMClip = bgmClip;

            isSyncingBGMVolume = false;
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            SetBGMVolume(0);
            bgmSource.Play();
            DOTween.To(GetBGMVolume, SetBGMVolume, PlayerPrefs.GetFloat("BGMVolume", 1f), 0.5f).OnComplete(() =>
            {
                isSyncingBGMVolume = true;
            });
        }

        public void StopBGM()
        {
            if (currentBGMClip == null)
            {
                return;
            }

            isSyncingBGMVolume = false;
            DOTween.KillAll();
            DOTween.To(GetBGMVolume, SetBGMVolume, 0, 0.5f).OnComplete(() =>
            {
                bgmSource.Stop();
                currentBGMClip = null;
            });
        }

        private float GetBGMVolume()
        {
            return bgmSource.volume;
        }

        private void SetBGMVolume(float volume)
        {
            bgmSource.volume = volume;
        }

        public void PlaySFX(AudioClip sfxClip)
        {
            AudioSource sfxSource = Instantiate(sfxSourcePrefab, transform);
            sfxSource.clip = sfxClip;
            sfxSource.loop = false;
            sfxSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxSource.Play();
            Destroy(sfxSource.gameObject, sfxClip.length + 0.1f);
        }

        private void Update()
        {
            if (isSyncingBGMVolume)
            {
                bgmSource.volume = PlayerPrefs.GetFloat("BGMVolume", 1f);
            }
        }
    }
}