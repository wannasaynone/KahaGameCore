using System;
using KahaGameCore.Package.DialogueSystem;
using UnityEngine;

public class DialogueCommand_AddValue : DialogueCommandBase
{
    public DialogueCommand_AddValue(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
    {
    }

    public override void Process(Action onCompleted, Action onForceQuit)
    {
        string valueName = DialogueData.Arg1;
        int value = int.Parse(DialogueData.Arg2);

        Debug.Log("AddValue: " + valueName + " " + value);

        KahaGameCore.Package.GameFlowSystem.SharedRepoditory.playerInstance.Stats.AddBase(valueName, value);

        onCompleted?.Invoke();
    }
}
