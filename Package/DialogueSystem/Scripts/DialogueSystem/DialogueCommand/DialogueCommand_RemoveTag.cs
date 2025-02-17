namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_RemoveTag : DialogueCommandBase
    {
        public DialogueCommand_RemoveTag(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(System.Action onCompleted, System.Action onForceQuit)
        {
            string tag = DialogueData.Arg1;
            PlayerManager.Instance.Player.RemoveTag(tag);
            onCompleted?.Invoke();
        }
    }
}