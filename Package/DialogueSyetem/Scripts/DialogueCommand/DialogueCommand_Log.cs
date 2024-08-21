using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem.DialogueCommand
{
    public class DialogueCommand_Log : DialogueCommandBase
    {
        public DialogueCommand_Log(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(System.Action onCompleted, System.Action onForceQuit)
        {
            Debug.Log("Log with command " + DialogueData.Command + " and arg1 " + DialogueData.Arg1 + " and arg2 " + DialogueData.Arg2 + " and arg3 " + DialogueData.Arg3);
            onCompleted?.Invoke();
        }
    }
}