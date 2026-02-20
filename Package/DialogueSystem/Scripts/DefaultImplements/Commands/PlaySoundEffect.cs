using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class PlaySoundEffect : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            string sePath = args.Length > 0 ? args[0] : string.Empty;

            if (string.IsNullOrEmpty(sePath))
            {
                Debug.LogError($"[PlaySoundEffect] Arg1 (sePath) is required. dialogueId={context.curDialogueId}");
                context.onComplete?.Invoke();
                return;
            }

            if (context.audioProvider == null || context.audioManager == null)
            {
                Debug.LogError($"[PlaySoundEffect] IAudioProvider or AudioManager is not set. dialogueId={context.curDialogueId}");
                context.onComplete?.Invoke();
                return;
            }

            _ = ProcessAsync(context, sePath);
        }

        private async UniTask ProcessAsync(DialogueContext context, string sePath)
        {
            AudioClip clip = await context.audioProvider.LoadAudioAsync(sePath);

            if (clip == null)
            {
                context.onComplete?.Invoke();
                return;
            }

            context.audioManager.PlaySE(clip);
            context.onComplete?.Invoke();
        }
    }

    public class PlaySoundEffectFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new PlaySoundEffect();
        }
    }
}
