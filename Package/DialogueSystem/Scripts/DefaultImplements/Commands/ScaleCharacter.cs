using Cysharp.Threading.Tasks;

namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class ScaleCharacter : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            string slotName = args[0];
            string scaleValueArg = args[1];
            string scaleTimeArg = args[2];
            string noWaitArg = args[3];

            float targetScale = 1f;
            if (!string.IsNullOrEmpty(scaleValueArg))
            {
                float.TryParse(scaleValueArg, out targetScale);
            }

            float scaleTime = 0f;
            if (!string.IsNullOrEmpty(scaleTimeArg))
            {
                float.TryParse(scaleTimeArg, out scaleTime);
            }

            bool noWait = !string.IsNullOrEmpty(noWaitArg);

            ProcessAsync(context, slotName, targetScale, scaleTime, noWait).Forget();
        }

        private async UniTaskVoid ProcessAsync(DialogueContext context, string slotName, float targetScale, float scaleTime, bool noWait)
        {
            if (noWait)
            {
                context.onComplete?.Invoke();
            }

            await context.view.ScaleCharacter(slotName, targetScale, scaleTime);

            if (!noWait)
            {
                context.onComplete?.Invoke();
            }
        }
    }

    public class ScaleCharacterFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new ScaleCharacter();
        }
    }
}