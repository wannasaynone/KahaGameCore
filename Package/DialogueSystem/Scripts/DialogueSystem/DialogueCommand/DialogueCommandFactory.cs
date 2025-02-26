using KahaGameCore.Package.DialogueSystem.DialogueCommand;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommandFactory : IDialogueFactory
    {
        private readonly Player player;

        public DialogueCommandFactory(Player player)
        {
            this.player = player;
        }

        DialogueCommandBase IDialogueFactory.CreateDialogueCommand(DialogueData dialogueData, IDialogueView dialogueView)
        {
            switch (dialogueData.Command)
            {
                case "Say":
                    return new DialogueCommand_Say(dialogueData, dialogueView);
                case "SetCharacter":
                    return new DialogueCommand_SetCharacter(dialogueData, dialogueView);
                case "AddOption":
                    return new DialogueCommand_AddOption(dialogueData, dialogueView, player);
                case "AddItem":
                    return new DialogueCommand_AddItem(dialogueData, dialogueView);
                case "IfHasItem":
                    return new DialogueCommand_IfHasItem(dialogueData, dialogueView, player);
                case "ShowCG":
                    return new DialogueCommand_ShowCG(dialogueData, dialogueView);
                case "HideCG":
                    return new DialogueCommand_HideCG(dialogueData, dialogueView);
                case "IfRead":
                    return new DialogueCommand_IfRead(dialogueData, dialogueView, player);
                case "GoTo":
                    return new DialogueCommand_GoTo(dialogueData, dialogueView, player);
                case "IfHasTags":
                    return new DialogueCommand_IfHasTag(dialogueData, dialogueView, player);
                case "AddTag":
                    return new DialogueCommand_AddTag(dialogueData, dialogueView);
                case "RemoveTag":
                    return new DialogueCommand_RemoveTag(dialogueData, dialogueView);
                case "Flash":
                    return new DialogueCommand_Flash(dialogueData, dialogueView);
                case "Transition":
                    return new DialogueCommand_Transition(dialogueData, dialogueView);
                case "ShakeCG":
                    return new DialogueCommand_ShakeCG(dialogueData, dialogueView);
                case "IfDayLess":
                    return new DialogueCommand_IfDayLess(dialogueData, dialogueView, player);
                case "IfDayMore":
                    return new DialogueCommand_IfDayMore(dialogueData, dialogueView, player);
                case "AddCoin":
                    return new DialogueCommand_AddCoin(dialogueData, dialogueView);
                case "Wait":
                    return new DialogueCommand_Wait(dialogueData, dialogueView);
                case "ShowSimplifiedCG":
                    return new DialogueCommand_ShowSimplifiedCG(dialogueData, dialogueView);
                case "PlayBGM":
                    return new DialogueCommand_PlayBGM(dialogueData, dialogueView);
                case "StopBGM":
                    return new DialogueCommand_StopBGM(dialogueData, dialogueView);
                case "PopUp":
                    return new DialogueCommand_PopUp(dialogueData, dialogueView);
                case "Visable":
                    return new DialogueCommand_Visable(dialogueData, dialogueView);
                case "CreateGeneralAnimationPlayer":
                    return new DialogueCommand_CreateCreateGeneralAnimationPlayer(dialogueData, dialogueView);
                case "PlayAnimation":
                    return new DialogueCommand_PlayAnimation(dialogueData, dialogueView);

                default:
                    return null;
            }
        }
    }
}

