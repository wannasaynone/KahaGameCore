using KahaGameCore.Package.DialogueSystem;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.DialogueSystemExtend
{
    public class DialogueCommandFactory : IDialogueFactory
    {
        private readonly DialogueSystem.DialogueCommandFactory originalFactory;

        public DialogueCommandFactory(Player player)
        {
            originalFactory = new DialogueSystem.DialogueCommandFactory(player);
        }

        public DialogueCommandBase CreateDialogueCommand(DialogueData dialogueData, IDialogueView dialogueView)
        {
            DialogueCommandBase command = originalFactory.CreateDialogueCommand(dialogueData, dialogueView);
            if (command != null)
            {
                return command;
            }

            switch (dialogueData.Command)
            {
                case "StopInGameBGM":
                    return new DialogueCommand_StopInGameBGM(dialogueData, dialogueView);
                case "PlayInGameBGM":
                    return new DialogueCommand_PlayInGameBGM(dialogueData, dialogueView);
                case "PlayInGameSound":
                    return new DialogueCommand_PlayInGameSound(dialogueData, dialogueView);
                default:
                    Debug.Log($"Unknown command: {dialogueData.Command}");
                    return null;
            }
        }
    }
}