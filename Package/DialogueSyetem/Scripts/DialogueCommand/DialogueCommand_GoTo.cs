using System;

namespace KahaGameCore.Package.DialogueSystem.DialogueCommand
{
    public class DialogueCommand_GoTo : DialogueCommandBase
    {
        public DialogueCommand_GoTo(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            DialogueManager.Instance.TriggerDialogue(int.Parse(DialogueData.Arg1), DialogueView);
            onCompleted?.Invoke();
        }
    }
}