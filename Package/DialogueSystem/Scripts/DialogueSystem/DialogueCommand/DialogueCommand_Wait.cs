using System;
using System.Collections;
using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_Wait : DialogueCommandBase
    {
        public DialogueCommand_Wait(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            float waitTime = float.Parse(DialogueData.Arg1);
            Common.GeneralCoroutineRunner.Instance.StartCoroutine(IEWait(waitTime, onCompleted));
        }

        private IEnumerator IEWait(float waitTime, Action onCompleted)
        {
            yield return new WaitForSeconds(waitTime);
            onCompleted?.Invoke();
        }
    }
}