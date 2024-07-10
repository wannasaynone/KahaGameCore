using System;
using System.Collections.Generic;
using KahaGameCore.Processor;

namespace KahaGameCore.DialogueSystem
{
    public class DialogueProcesser
    {
        private readonly IDialogueView dialogueView;
        private readonly List<DialogueData> dialogueDatas = new List<DialogueData>();

        private Action onCompleted;

        public DialogueProcesser(int id, IDialogueView dialogueView, DialogueData[] allDialogueData)
        {
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
                processables[i] = CreateDialogueCommand(dialogueDatas[i]);
            }

            Processor<DialogueCommandBase> processor = new Processor<DialogueCommandBase>(processables);
            this.onCompleted = onCompleted;
            processor.Start(OnDialogueCommandCompleted, onForceQuit);
        }

        private void OnDialogueCommandCompleted()
        {
            onCompleted?.Invoke();
        }

        private DialogueCommandBase CreateDialogueCommand(DialogueData dialogueData)
        {
            switch (dialogueData.Command)
            {
                case "Say":
                    return new DialogueCommand_Say(dialogueData, dialogueView);
                case "SetCharacter":
                    return new DialogueCommand_SetCharacter(dialogueData, dialogueView);
                case "AddOption":
                    return new DialogueCommand_AddOption(dialogueData, dialogueView);
                default:
                    return null;
            }
        }
    }
}