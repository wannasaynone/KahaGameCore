using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class PlayBackgroundMusic : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            string bgmPath = args.Length > 0 ? args[0] : string.Empty;

            if (string.IsNullOrEmpty(bgmPath))
            {
                Debug.LogError($"[PlayBackgroundMusic] Arg1 (bgmPath) is required. dialogueId={context.curDialogueId}");
                context.onComplete?.Invoke();
                return;
            }

            if (context.audioProvider == null || context.audioManager == null)
            {
                Debug.LogError($"[PlayBackgroundMusic] IAudioProvider or AudioManager is not set. dialogueId={context.curDialogueId}");
                context.onComplete?.Invoke();
                return;
            }

            float fadeInDuration = 0.5f;
            if (args.Length > 1 && !string.IsNullOrEmpty(args[1]) && float.TryParse(args[1], out float parsedDuration))
            {
                fadeInDuration = parsedDuration;
            }

            _ = ProcessAsync(context, bgmPath, fadeInDuration);
        }

        private async UniTask ProcessAsync(DialogueContext context, string bgmPath, float fadeInDuration)
        {
            AudioClip clip = await context.audioProvider.LoadAudioAsync(bgmPath);

            if (clip == null)
            {
                Debug.LogWarning($"[PlayBackgroundMusic] Failed to load audio clip: {bgmPath}");
                context.onComplete?.Invoke();
                return;
            }

            context.audioManager.PlayBGM(clip, fadeInDuration);
            context.onComplete?.Invoke();
        }
    }

    public class PlayBackgroundMusicFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new PlayBackgroundMusic();
        }
    }
}
