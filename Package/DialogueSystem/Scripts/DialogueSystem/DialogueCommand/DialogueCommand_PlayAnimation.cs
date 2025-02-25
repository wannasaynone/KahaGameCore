using System;

namespace KahaGameCore.Package.DialogueSystem.DialogueCommand
{
    public class DialogueCommand_PlayAnimation : DialogueCommandBase
    {
        public DialogueCommand_PlayAnimation(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            string playerName = DialogueData.Arg1;
            string animationName = DialogueData.Arg2;

            GeneralAnimationPlayer generalAnimationPlayer = DialogueCommand_CreateCreateGeneralAnimationPlayer.GetGeneralAnimationPlayer(playerName);
            if (generalAnimationPlayer != null)
            {
                generalAnimationPlayer.PlayAnimation(animationName);
            }

            onCompleted?.Invoke();
        }
    }
}