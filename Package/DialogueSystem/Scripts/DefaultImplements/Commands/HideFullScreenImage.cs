using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class HideFullScreenImage : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            string cgName = args.Length > 0 ? args[0] : string.Empty;
            float fadeOutTime = 0.5f;
            bool waitForCompletion = true;

            if (string.IsNullOrEmpty(cgName))
            {
                Debug.LogError($"[HideFullScreenImage] Arg1 (cgName) is required. dialogueId={context.curDialogueId}");
                context.onComplete?.Invoke();
                return;
            }

            if (!context.view.HasCG(cgName))
            {
                context.onComplete?.Invoke();
                return;
            }

            if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
            {
                if (float.TryParse(args[1], out float parsedTime))
                {
                    fadeOutTime = parsedTime;
                }
            }

            if (args.Length > 2 && !string.IsNullOrEmpty(args[2]))
            {
                waitForCompletion = false;
            }

            _ = ProcessAsync(context, cgName, fadeOutTime, waitForCompletion);
        }

        private async UniTask ProcessAsync(DialogueContext context, string cgName, float fadeOutTime, bool waitForCompletion)
        {
            try
            {
                if (waitForCompletion)
                {
                    await context.view.HideCG(cgName, fadeOutTime);
                    context.cgProvider?.ReleaseCG(cgName);
                    context.onComplete?.Invoke();
                }
                else
                {
                    _ = HideAndReleaseCG(context, cgName, fadeOutTime);
                    context.onComplete?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HideFullScreenImage] Error. Arg1={cgName}, dialogueId={context.curDialogueId}, exception={ex.Message}");
                context.cgProvider?.ReleaseCG(cgName);
                context.onComplete?.Invoke();
            }
        }

        private async UniTask HideAndReleaseCG(DialogueContext context, string cgName, float fadeOutTime)
        {
            try
            {
                await context.view.HideCG(cgName, fadeOutTime);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HideFullScreenImage] Error during HideAndReleaseCG. Arg1={cgName}, dialogueId={context.curDialogueId}, exception={ex.Message}");
            }
            finally
            {
                context.cgProvider?.ReleaseCG(cgName);
            }
        }
    }

    public class HideFullScreenImageFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new HideFullScreenImage();
        }
    }
}
