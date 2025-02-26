using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_IfHasTag : DialogueCommandBase
    {
        private readonly Player player;

        public DialogueCommand_IfHasTag(DialogueData dialogueData, IDialogueView dialogueView, Player player) : base(dialogueData, dialogueView)
        {
            this.player = player;
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            string tagArray = DialogueData.Arg1;
            string[] tags = tagArray.Split(';');

            bool hasAllTags = true;
            foreach (string tag in tags)
            {
                if (!player.HasTag(tag))
                {
                    hasAllTags = false;
                    break;
                }
            }

            if (hasAllTags)
            {
                DialogueManager.Instance.TriggerDialogue(new DialogueManager.PendingDialogueData
                {
                    id = int.Parse(DialogueData.Arg2),
                    dialogueView = DialogueView,
                    onCompleted = null
                });
                onForceQuit?.Invoke();
                return;
            }
            else
            {
                if (!string.IsNullOrEmpty(DialogueData.Arg3))
                {
                    DialogueManager.Instance.TriggerDialogue(new DialogueManager.PendingDialogueData
                    {
                        id = int.Parse(DialogueData.Arg3),
                        dialogueView = DialogueView,
                        onCompleted = null
                    });
                    onForceQuit?.Invoke();
                    return;
                }
            }

            onCompleted?.Invoke();
        }
    }
}