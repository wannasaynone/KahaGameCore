using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class ShowCharacter : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            string positionArg = args[0];
            string imagePath = args[1];
            string fadeTimeArg = args[2];
            string noWaitArg = args[3];

            CharacterPositionParser.Parse(positionArg, out string slotName, out float offsetX);

            float fadeInTime = 0f;
            if (!string.IsNullOrEmpty(fadeTimeArg))
            {
                float.TryParse(fadeTimeArg, out fadeInTime);
            }

            bool noWait = !string.IsNullOrEmpty(noWaitArg);

            ProcessAsync(context, slotName, offsetX, imagePath, fadeInTime, noWait).Forget();
        }

        private async UniTaskVoid ProcessAsync(DialogueContext context, string slotName, float offsetX, string imagePath, float fadeInTime, bool noWait)
        {
            Texture2D texture = await context.cgProvider.LoadCGAsync(imagePath);
            if (texture == null)
            {
                Debug.LogError($"[ShowCharacter] Failed to load character image: {imagePath}");
                context.onComplete?.Invoke();
                return;
            }

            if (noWait)
            {
                context.onComplete?.Invoke();
            }

            await context.view.ShowCharacter(slotName, texture, offsetX, fadeInTime);

            if (!noWait)
            {
                context.onComplete?.Invoke();
            }
        }
    }

    public class ShowCharacterFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new ShowCharacter();
        }
    }
}