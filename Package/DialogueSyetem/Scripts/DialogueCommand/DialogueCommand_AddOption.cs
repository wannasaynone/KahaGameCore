using System;

namespace KahaGameCore.DialogueSystem.DialogueCommand
{
    public class DialogueCommand_AddOption : DialogueCommandBase
    {
        public DialogueCommand_AddOption(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
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