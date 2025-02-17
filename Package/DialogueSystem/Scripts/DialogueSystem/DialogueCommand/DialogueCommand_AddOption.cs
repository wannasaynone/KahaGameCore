using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_AddOption : DialogueCommandBase
    {
        private readonly Player player;

        public DialogueCommand_AddOption(DialogueData dialogueData, IDialogueView dialogueView, Player player) : base(dialogueData, dialogueView)
        {
            this.player = player;
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            IDialogueOptionButton button = DialogueView.AddOptionButton();
            button.SetUpButtonText(DialogueData.Arg1);
            button.SetUpOnClicked(OnClickedOption);
            onCompleted?.Invoke();
        }

        private void OnClickedOption()
        {
            DialogueManager.Instance.TriggerDialogue(int.Parse(DialogueData.Arg2), DialogueView);
            DialogueView.ClearOptions();
        }
    }
}