namespace KahaGameCore.SubSystem.DialogueSystem
{
    public interface IDialogueFactory
    {
        DialogueCommand.DialogueCommandBase CreateDialogueCommand(DialogueData dialogueData, IDialogueView dialogueView);
    }
}