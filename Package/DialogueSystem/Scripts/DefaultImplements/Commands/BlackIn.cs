using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class BlackIn : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            float fadeTime = 2f;
            bool waitForCompletion = true;

            // Parse fadeTime from arg1 if provided
            if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
            {
                if (float.TryParse(args[0], out float parsedTime))
                {
                    fadeTime = parsedTime;
                }
                else
                {
                    Debug.LogError($"Invalid fadeTime value in BlackIn command: {args[0]}");
                }
            }

            // Parse wait option from arg2 if provided
            if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
            {
                waitForCompletion = false;
            }

            _ = ProcessAsync(context, fadeTime, waitForCompletion);
        }

        private async UniTask ProcessAsync(DialogueContext context, float fadeTime, bool waitForCompletion)
        {
            try
            {
                if (waitForCompletion)
                {
                    await context.view.BlackIn(fadeTime);
                    context.onComplete?.Invoke();
                }
                else
                {
                    // Start the effect but don't wait for it to complete
                    // Ensure we don't capture the task to allow it to run independently
                    _ = context.view.BlackIn(fadeTime);
                    context.onComplete?.Invoke();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in BlackIn command: {ex.Message}");
                context.onComplete?.Invoke();
            }
        }
    }

    public class BlackInFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new BlackIn();
        }
    }
}
