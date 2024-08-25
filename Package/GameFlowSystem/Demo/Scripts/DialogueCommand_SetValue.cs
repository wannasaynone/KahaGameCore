using System;
using KahaGameCore.Package.DialogueSystem;

public class DialogueCommand_SetValue : DialogueCommandBase
{
    public DialogueCommand_SetValue(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
    {
    }

    public override void Process(Action onCompleted, Action onForceQuit)
    {
        string valueName = DialogueData.Arg1;
        int value = int.Parse(DialogueData.Arg2);

        KahaGameCore.Package.GameFlowSystem.SharedRepoditory.playerInstance.Stats.SetBase(valueName, value);

        onCompleted?.Invoke();
    }
}
