using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class HideCharacter : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            string slotName = args[0];
            string fadeTimeArg = args[1];
            string noWaitArg = args[2];

            float fadeOutTime = 0f;
            if (!string.IsNullOrEmpty(fadeTimeArg))
            {
                float.TryParse(fadeTimeArg, out fadeOutTime);
            }

            bool noWait = !string.IsNullOrEmpty(noWaitArg);

            ProcessAsync(context, slotName, fadeOutTime, noWait).Forget();
        }

        private async UniTaskVoid ProcessAsync(DialogueContext context, string slotName, float fadeOutTime, bool noWait)
        {
            if (noWait)
            {
                context.onComplete?.Invoke();
            }

            await context.view.HideCharacter(slotName, fadeOutTime);

            if (!noWait)
            {
                context.onComplete?.Invoke();
            }
        }
    }

    public class HideCharacterFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new HideCharacter();
        }
    }
}