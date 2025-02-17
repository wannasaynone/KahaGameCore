namespace KahaGameCore.Package.DialogueSystem
{
    public interface IDialogueFactory
    {
        DialogueCommandBase CreateDialogueCommand(DialogueData dialogueData, IDialogueView dialogueView);
    }
}