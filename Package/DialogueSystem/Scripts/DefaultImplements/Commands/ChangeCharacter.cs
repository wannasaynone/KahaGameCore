using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class ChangeCharacter : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            string positionArg = args[0];
            string imagePath = args[1];
            string fadeInTimeArg = args[2];
            string fadeOutTimeArg = args[3];
            string noWaitArg = args[4];

            CharacterPositionParser.Parse(positionArg, out string slotName, out float offsetX);

            float fadeInTime = 0f;
            if (!string.IsNullOrEmpty(fadeInTimeArg))
            {
                float.TryParse(fadeInTimeArg, out fadeInTime);
            }

            float fadeOutTime = 0f;
            if (!string.IsNullOrEmpty(fadeOutTimeArg))
            {
                float.TryParse(fadeOutTimeArg, out fadeOutTime);
            }

            bool noWait = !string.IsNullOrEmpty(noWaitArg);

            ProcessAsync(context, slotName, offsetX, imagePath, fadeInTime, fadeOutTime, noWait).Forget();
        }

        private async UniTaskVoid ProcessAsync(DialogueContext context, string slotName, float offsetX, string imagePath, float fadeInTime, float fadeOutTime, bool noWait)
        {
            if (noWait)
            {
                context.onComplete?.Invoke();
            }

            await context.view.HideCharacter(slotName, fadeOutTime);

            Texture2D texture = await context.cgProvider.LoadCGAsync(imagePath);
            if (texture == null)
            {
                Debug.LogError($"[ChangeCharacter] Failed to load character image: {imagePath}");
                if (!noWait)
                {
                    context.onComplete?.Invoke();
                }
                return;
            }

            await context.view.ShowCharacter(slotName, texture, offsetX, fadeInTime);

            if (!noWait)
            {
                context.onComplete?.Invoke();
            }
        }
    }

    public class ChangeCharacterFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new ChangeCharacter();
        }
    }
}