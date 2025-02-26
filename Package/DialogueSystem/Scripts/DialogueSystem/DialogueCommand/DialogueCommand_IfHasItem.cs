using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_IfHasItem : DialogueCommandBase
    {
        private readonly Player player;

        public DialogueCommand_IfHasItem(DialogueData dialogueData, IDialogueView dialogueView, Player player) : base(dialogueData, dialogueView)
        {
            this.player = player;
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            int goToIfHave = int.Parse(DialogueData.Arg2);
            int goToIfNotHave = string.IsNullOrEmpty(DialogueData.Arg3) ? -1 : int.Parse(DialogueData.Arg3);

            string[] itemIDs = DialogueData.Arg1.Split(';');

            bool hasAllItem = true;
            for (int i = 0; i < itemIDs.Length; i++)
            {
                if (!string.IsNullOrEmpty(itemIDs[i])
                    && !player.HasItem(int.Parse(itemIDs[i])))
                {
                    hasAllItem = false;
                    break;
                }
            }

            if (hasAllItem)
            {
                DialogueManager.Instance.TriggerDialogue(new DialogueManager.PendingDialogueData
                {
                    id = goToIfHave,
                    dialogueView = DialogueView,
                    onCompleted = null
                });
                onForceQuit?.Invoke();
                return;
            }
            else if (goToIfNotHave != -1)
            {
                DialogueManager.Instance.TriggerDialogue(new DialogueManager.PendingDialogueData
                {
                    id = goToIfNotHave,
                    dialogueView = DialogueView,
                    onCompleted = null
                });
                onForceQuit?.Invoke();
                return;
            }

            onCompleted?.Invoke();
        }
    }
}