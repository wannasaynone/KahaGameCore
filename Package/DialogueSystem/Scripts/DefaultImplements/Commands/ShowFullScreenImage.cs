using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class ShowFullScreenImage : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            string cgName = args.Length > 0 ? args[0] : string.Empty;
            float fadeInTime = 0.5f;
            bool waitForCompletion = true;

            if (string.IsNullOrEmpty(cgName))
            {
                Debug.LogError($"[ShowFullScreenImage] Arg1 (cgName) is required. dialogueId={context.curDialogueId}");
                context.onComplete?.Invoke();
                return;
            }

            if (context.cgProvider == null)
            {
                Debug.LogError($"[ShowFullScreenImage] ICGProvider is not set. Arg1={cgName}, dialogueId={context.curDialogueId}");
                context.onComplete?.Invoke();
                return;
            }

            if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
            {
                if (float.TryParse(args[1], out float parsedTime))
                {
                    fadeInTime = parsedTime;
                }
            }

            if (args.Length > 2 && !string.IsNullOrEmpty(args[2]))
            {
                waitForCompletion = false;
            }

            _ = ProcessAsync(context, cgName, fadeInTime, waitForCompletion);
        }

        private async UniTask ProcessAsync(DialogueContext context, string cgName, float fadeInTime, bool waitForCompletion)
        {
            try
            {
                Texture2D texture = await context.cgProvider.LoadCGAsync(cgName);

                if (texture == null)
                {
                    Debug.LogError($"[ShowFullScreenImage] Failed to load CG. Arg1={cgName}, dialogueId={context.curDialogueId}");
                    context.onComplete?.Invoke();
                    return;
                }

                context.view.gameObject.SetActive(true);

                if (waitForCompletion)
                {
                    await context.view.ShowCG(texture, cgName, fadeInTime);
                    context.onComplete?.Invoke();
                }
                else
                {
                    _ = context.view.ShowCG(texture, cgName, fadeInTime);
                    context.onComplete?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShowFullScreenImage] Error. Arg1={cgName}, dialogueId={context.curDialogueId}, exception={ex.Message}");
                context.onComplete?.Invoke();
            }
        }
    }

    public class ShowFullScreenImageFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new ShowFullScreenImage();
        }
    }
}
