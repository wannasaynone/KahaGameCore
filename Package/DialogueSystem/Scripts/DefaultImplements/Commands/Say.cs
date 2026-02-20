namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class Say : DialogueCommandBase
    {
        private DialogueContext currentContext;

        public override void Process(string[] args, DialogueContext context)
        {
            string speakerName = args.Length > 0 ? args[0] : string.Empty;
            string dialogueText = args.Length > 1 ? args[1] : string.Empty;

            currentContext = context;

            context.view.OnDialogueTextCompleted += DialogueView_OnDialogueTextCompleted;

            context.view.gameObject.SetActive(true);

            context.view.SetSpeakerName(speakerName);
            context.view.SetDialogueText(dialogueText);
        }

        private void DialogueView_OnDialogueTextCompleted()
        {
            currentContext.view.OnDialogueTextCompleted -= DialogueView_OnDialogueTextCompleted;
            currentContext.onComplete?.Invoke();
        }
    }

    public class SayFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new Say();
        }
    }
}