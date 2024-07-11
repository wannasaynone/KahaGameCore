using System;
using System.Collections.Generic;
using KahaGameCore.Processor;
using KahaGameCore.SubSystem.DialogueSystem.DialogueCommand;

namespace KahaGameCore.SubSystem.DialogueSystem
{
    public class DialogueProcesser
    {
        private readonly IDialogueFactory dialogueFactory;
        private readonly IDialogueView dialogueView;
        private readonly List<DialogueData> dialogueDatas = new List<DialogueData>();

        private Action onCompleted;

        public DialogueProcesser(int id, IDialogueView dialogueView, DialogueData[] allDialogueData, IDialogueFactory dialogueFactory)
        {
            this.dialogueFactory = dialogueFactory;

            for (int i = 0; i < allDialogueData.Length; i++)
            {
                if (allDialogueData[i].ID == id)
                {
                    dialogueDatas.Add(allDialogueData[i]);
                }
            }

            dialogueDatas.Sort((a, b) => a.Line.CompareTo(b.Line));

            this.dialogueView = dialogueView;
        }

        public void Process(Action onCompleted, Action onForceQuit)
        {
            DialogueCommandBase[] processables = new DialogueCommandBase[dialogueDatas.Count];
            for (int i = 0; i < dialogueDatas.Count; i++)
            {
                processables[i] = dialogueFactory.CreateDialogueCommand(dialogueDatas[i], dialogueView);
            }

            Processor<DialogueCommandBase> processor = new Processor<DialogueCommandBase>(processables);
            this.onCompleted = onCompleted;
            processor.Start(OnDialogueCommandCompleted, onForceQuit);
        }

        private void OnDialogueCommandCompleted()
        {
            onCompleted?.Invoke();
        }
    }
}