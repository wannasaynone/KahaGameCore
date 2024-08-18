namespace KahaGameCore.DialogueSystem
{
    public interface IDialogueFactory
    {
        DialogueCommandBase CreateDialogueCommand(DialogueData dialogueData, IDialogueView dialogueView);
    }
}