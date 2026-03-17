using Cysharp.Threading.Tasks;

namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class MoveCharacterX : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            string slotName = args[0];
            string addXArg = args[1];
            string moveTimeArg = args[2];
            string noWaitArg = args[3];

            float addX = 0f;
            if (!string.IsNullOrEmpty(addXArg))
            {
                float.TryParse(addXArg, out addX);
            }

            float moveTime = 0f;
            if (!string.IsNullOrEmpty(moveTimeArg))
            {
                float.TryParse(moveTimeArg, out moveTime);
            }

            bool noWait = !string.IsNullOrEmpty(noWaitArg);

            ProcessAsync(context, slotName, addX, moveTime, noWait).Forget();
        }

        private async UniTaskVoid ProcessAsync(DialogueContext context, string slotName, float addX, float moveTime, bool noWait)
        {
            if (noWait)
            {
                context.onComplete?.Invoke();
            }

            await context.view.MoveCharacterX(slotName, addX, moveTime);

            if (!noWait)
            {
                context.onComplete?.Invoke();
            }
        }
    }

    public class MoveCharacterXFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new MoveCharacterX();
        }
    }
}