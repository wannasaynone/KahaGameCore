namespace KahaGameCore.DialogueSystem.DialogueCommand
{
    public class DialogueCommandFactory : IDialogueFactory
    {
        DialogueCommandBase IDialogueFactory.CreateDialogueCommand(DialogueData dialogueData, IDialogueView dialogueView)
        {
            switch (dialogueData.Command)
            {
                case "Say":
                    return new DialogueCommand_Say(dialogueData, dialogueView);
                case "SetCharacter":
                    return new DialogueCommand_SetCharacter(dialogueData, dialogueView);
                case "AddOption":
                    return new DialogueCommand_AddOption(dialogueData, dialogueView);
                case "ShowCG":
                    return new DialogueCommand_ShowCG(dialogueData, dialogueView);
                case "HideCG":
                    return new DialogueCommand_HideCG(dialogueData, dialogueView);
                case "GoTo":
                    return new DialogueCommand_GoTo(dialogueData, dialogueView);
                default:
                    return null;
            }
        }
    }
}

