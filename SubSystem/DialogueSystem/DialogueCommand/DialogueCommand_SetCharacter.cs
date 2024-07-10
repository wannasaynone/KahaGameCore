using UnityEngine;

namespace KahaGameCore.DialogueSystem
{
    public class DialogueCommand_SetCharacter : DialogueCommandBase
    {
        public DialogueCommand_SetCharacter(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(System.Action onCompleted, System.Action onForceQuit)
        {
            if (DialogueData.Arg1 == "Left")
            {
                DialogueView.SetLeftCharacterImage(DialogueData.Arg2);
            }
            else if (DialogueData.Arg1 == "Right")
            {
                DialogueView.SetRightCharacterImage(DialogueData.Arg2);
            }
            else
            {
                Debug.LogError("Invalid Arg1: " + DialogueData.Arg1);
            }
            onCompleted?.Invoke();
        }
    }
}