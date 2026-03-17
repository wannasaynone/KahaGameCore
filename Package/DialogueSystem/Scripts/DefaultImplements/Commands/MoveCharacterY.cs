using Cysharp.Threading.Tasks;

namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class MoveCharacterY : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            string slotName = args[0];
            string addYArg = args[1];
            string moveTimeArg = args[2];
            string noWaitArg = args[3];

            float addY = 0f;
            if (!string.IsNullOrEmpty(addYArg))
            {
                float.TryParse(addYArg, out addY);
            }

            float moveTime = 0f;
            if (!string.IsNullOrEmpty(moveTimeArg))
            {
                float.TryParse(moveTimeArg, out moveTime);
            }

            bool noWait = !string.IsNullOrEmpty(noWaitArg);

            ProcessAsync(context, slotName, addY, moveTime, noWait).Forget();
        }

        private async UniTaskVoid ProcessAsync(DialogueContext context, string slotName, float addY, float moveTime, bool noWait)
        {
            if (noWait)
            {
                context.onComplete?.Invoke();
            }

            await context.view.MoveCharacterY(slotName, addY, moveTime);

            if (!noWait)
            {
                context.onComplete?.Invoke();
            }
        }
    }

    public class MoveCharacterYFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new MoveCharacterY();
        }
    }
}