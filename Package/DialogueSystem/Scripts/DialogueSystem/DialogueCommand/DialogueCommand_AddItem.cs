using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_AddItem : DialogueCommandBase
    {
        public DialogueCommand_AddItem(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            int id = Convert.ToInt32(DialogueData.Arg1);
            int count = Convert.ToInt32(DialogueData.Arg2);
            PlayerManager.Instance.Player.AddItem(id, count);
            onCompleted?.Invoke();
        }
    }
}