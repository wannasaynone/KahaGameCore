using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_SetCharacter : DialogueCommandBase
    {
        public DialogueCommand_SetCharacter(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(System.Action onCompleted, System.Action onForceQuit)
        {
            string materialName = DialogueData.Arg4 == "" ? "Default" : DialogueData.Arg4;
            if (DialogueData.Arg1 == "Left")
            {
                if (DialogueData.Arg3 == "NoWait")
                {
                    DialogueView.SetLeftCharacterImage(DialogueData.Arg2, materialName);
                    onCompleted?.Invoke();
                }
                else
                {
                    DialogueView.SetLeftCharacterImage(DialogueData.Arg2, materialName, onCompleted);
                }
            }
            else if (DialogueData.Arg1 == "Right")
            {
                if (DialogueData.Arg3 == "NoWait")
                {
                    DialogueView.SetRightCharacterImage(DialogueData.Arg2, materialName);
                    onCompleted?.Invoke();
                }
                else
                {
                    DialogueView.SetRightCharacterImage(DialogueData.Arg2, materialName, onCompleted);
                }
            }
            else
            {
                Debug.LogError("Invalid Arg1: " + DialogueData.Arg1);
            }
        }
    }
}