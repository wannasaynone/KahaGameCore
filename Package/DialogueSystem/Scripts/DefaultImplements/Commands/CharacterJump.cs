using Cysharp.Threading.Tasks;

namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class CharacterJump : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            string slotName = args[0];
            string moveTimeArg = args[1];
            string noWaitArg = args[2];

            float totalTime = 0f;
            if (!string.IsNullOrEmpty(moveTimeArg))
            {
                float.TryParse(moveTimeArg, out totalTime);
            }

            bool noWait = !string.IsNullOrEmpty(noWaitArg);

            ProcessAsync(context, slotName, totalTime, noWait).Forget();
        }

        private async UniTaskVoid ProcessAsync(DialogueContext context, string slotName, float totalTime, bool noWait)
        {
            if (noWait)
            {
                context.onComplete?.Invoke();
            }

            await context.view.CharacterJump(slotName, totalTime);

            if (!noWait)
            {
                context.onComplete?.Invoke();
            }
        }
    }

    public class CharacterJumpFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new CharacterJump();
        }
    }
}