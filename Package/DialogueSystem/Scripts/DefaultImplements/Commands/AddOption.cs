namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class AddOption : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            // Arg1: ContextData ID for option text (localized)
            // Arg2: Dialogue ID to jump to when selected
            // Arg3: Target line number (optional, defaults to 1)
            string buttonStr = args.Length > 0 ? args[0] : string.Empty;
            string dialogueIdStr = args.Length > 1 ? args[1] : string.Empty;
            string targetLineStr = args.Length > 2 ? args[2] : string.Empty;

            if (!int.TryParse(dialogueIdStr, out int dialogueId))
            {
                UnityEngine.Debug.LogError("[AddOption] Invalid Dialogue ID: " + dialogueIdStr);
                context.onComplete?.Invoke();
                return;
            }

            int targetLine = 1;
            if (!string.IsNullOrEmpty(targetLineStr) && !int.TryParse(targetLineStr, out targetLine))
            {
                UnityEngine.Debug.LogError("[AddOption] Invalid target line: " + targetLineStr);
                context.onComplete?.Invoke();
                return;
            }

            context.pendingOptions.Add(new OptionData
            {
                text = buttonStr,
                targetDialogueId = dialogueId,
                targetLine = targetLine
            });

            context.onComplete?.Invoke();
        }
    }

    public class AddOptionFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new AddOption();
        }
    }
}
