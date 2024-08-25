using System;
using System.Collections;
using System.Collections.Generic;
using KahaGameCore.Package.DialogueSystem;
using KahaGameCore.Package.GameFlowSystem;
using UnityEngine;

public class DialogueCommand_IfValue : DialogueCommandBase
{
    public DialogueCommand_IfValue(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
    {
    }

    public override void Process(Action onCompleted, Action onForceQuit)
    {
        string valueName = DialogueData.Arg1;
        string compareOperator = DialogueData.Arg2;
        string compareValue = DialogueData.Arg3;

        switch (compareOperator)
        {
            case "==":
                if (SharedRepoditory.playerInstance.Stats.GetTotal(valueName, true) == int.Parse(compareValue))
                {
                    onCompleted?.Invoke();
                }
                else
                {
                    onForceQuit?.Invoke();
                }
                break;
            case ">=":
                if (SharedRepoditory.playerInstance.Stats.GetTotal(valueName, true) >= int.Parse(compareValue))
                {
                    onCompleted?.Invoke();
                }
                else
                {
                    onForceQuit?.Invoke();
                }
                break;
            case "<=":
                if (SharedRepoditory.playerInstance.Stats.GetTotal(valueName, true) <= int.Parse(compareValue))
                {
                    onCompleted?.Invoke();
                }
                else
                {
                    onForceQuit?.Invoke();
                }
                break;
        }
    }
}
