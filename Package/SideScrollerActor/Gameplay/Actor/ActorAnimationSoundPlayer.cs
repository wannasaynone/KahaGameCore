using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    public class ActotSoundPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip[] muted_pain_sound;
        [SerializeField] private float muted_pain_sound_cooldown = 0.5f;

        private float soundCooldownTimer = 0f;

        private void Update()
        {
            if (soundCooldownTimer > 0f)
            {
                soundCooldownTimer -= Time.deltaTime;
            }
        }

        public void PlaySound(AudioClip audioClip)
        {
            if (audioClip == null)
            {
                return;
            }

            Audio.AudioManager.Instance.PlaySound(audioClip);
        }

        private int lastMutedPainSoundIndex = -1;
        public void PlayRandomMutedPainSound()
        {
            if (muted_pain_sound.Length == 0)
            {
                return;
            }

            if (soundCooldownTimer > 0f)
            {
                return;
            }

            int index = Random.Range(0, muted_pain_sound.Length);

            if (index == lastMutedPainSoundIndex)
            {
                PlayRandomMutedPainSound();
                return;
            }

            lastMutedPainSoundIndex = index;
            PlaySound(muted_pain_sound[lastMutedPainSoundIndex]);

            soundCooldownTimer = muted_pain_sound_cooldown;
        }
    }
}