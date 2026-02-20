namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class ShowOptions : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            if (context.pendingOptions == null || context.pendingOptions.Count == 0)
            {
                UnityEngine.Debug.LogWarning("[ShowOptions] No pending options to show. Continuing to next line.");
                context.onComplete?.Invoke();
                return;
            }

            // Show options in the view and pause dialogue (do NOT call onComplete)
            // Dialogue resumes when player selects an option via onRequestJumpToLine
            context.view.ShowOptions(context.pendingOptions, (selectedOption) =>
            {
                context.pendingOptions.Clear();
                context.onRequestJumpToDialogueLine?.Invoke(selectedOption.targetDialogueId, selectedOption.targetLine);
            });
        }
    }

    public class ShowOptionsFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new ShowOptions();
        }
    }
}
