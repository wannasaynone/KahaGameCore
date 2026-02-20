namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class GoToLine : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            // Arg1: Target line number to jump to
            string targetLineStr = args.Length > 0 ? args[0] : string.Empty;

            if (!int.TryParse(targetLineStr, out int targetLine))
            {
                UnityEngine.Debug.LogError("[GoToLine] Invalid target line: " + targetLineStr + ". Continuing to next line.");
                context.onComplete?.Invoke();
                return;
            }

            context.onRequestJumpToLine?.Invoke(targetLine);
        }
    }

    public class GoToLineFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new GoToLine();
        }
    }
}
