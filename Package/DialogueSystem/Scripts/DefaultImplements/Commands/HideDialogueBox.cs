namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class HideDialogueBox : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            context.view.HideDialogueBox();
            context.onComplete?.Invoke();
        }
    }

    public class HideDialogueBoxFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new HideDialogueBox();
        }
    }
}
