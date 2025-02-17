using System;
using UnityEngine;


namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_Visable : DialogueCommandBase
    {
        public DialogueCommand_Visable(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            switch (DialogueData.Arg1)
            {
                case "Show":
                case "On":
                case "True":
                case "1":
                    UserInterfaceManager.Instance.DialogueView.gameObject.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    break;

                case "Hide":
                case "Off":
                case "False":
                case "0":
                    UserInterfaceManager.Instance.DialogueView.gameObject.GetComponent<RectTransform>().localScale = new Vector3(0.0f, 1.0f, 1.0f);
                    break;
            }
            onCompleted?.Invoke();
        }
    }
}
