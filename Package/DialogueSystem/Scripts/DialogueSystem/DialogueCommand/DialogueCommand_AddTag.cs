using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_AddTag : DialogueCommandBase
    {
        public DialogueCommand_AddTag(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            string tag = DialogueData.Arg1;
            PlayerManager.Instance.Player.AddTag(tag);
            onCompleted?.Invoke();
        }
    }
}