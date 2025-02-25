using System;
using System.Collections.Generic;
using KahaGameCore.Package.EffectProcessor.Processor;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueProcesser
    {
        public int CurrentProcessingID { get; private set; }

        private readonly IDialogueFactory dialogueFactory;
        private readonly IDialogueView dialogueView;
        private readonly List<DialogueData> dialogueDatas = new List<DialogueData>();

        private Action onCompleted;

        public DialogueProcesser(int id, IDialogueView dialogueView, DialogueData[] allDialogueData, IDialogueFactory dialogueFactory)
        {
            if (allDialogueData == null)
            {
                UnityEngine.Debug.LogError("[DialogueManager][Process] allDialogueData is null");
                return;
            }

            if (dialogueFactory == null)
            {
                UnityEngine.Debug.LogError("[DialogueManager][Process] dialogueFactory is null");
                return;
            }

            this.dialogueFactory = dialogueFactory;

            for (int i = 0; i < allDialogueData.Length; i++)
            {
                if (allDialogueData[i].ID == id)
                {
                    dialogueDatas.Add(allDialogueData[i]);
                }
            }

            if (dialogueDatas.Count <= 0)
            {
                UnityEngine.Debug.LogError("[DialogueManager][Process] Can't find dialogue data with id=" + id);
                onCompleted?.Invoke();
                return;
            }

            dialogueDatas.Sort((a, b) => a.Line.CompareTo(b.Line));

            this.dialogueView = dialogueView;
            CurrentProcessingID = id;
        }

        public void Process(Action onCompleted, Action onForceQuit)
        {
            List<DialogueCommandBase> processables = new List<DialogueCommandBase>();
            for (int i = 0; i < dialogueDatas.Count; i++)
            {
                DialogueCommandBase processable = dialogueFactory.CreateDialogueCommand(dialogueDatas[i], dialogueView);
                if (processable != null)
                {
                    processables.Add(processable);
                }
                else
                {
                    UnityEngine.Debug.LogError("[DialogueManager][Process] Can't create dialogue command with id=" + dialogueDatas[i].ID + " line=" + dialogueDatas[i].Line + " Command=" + dialogueDatas[i].Command + ", will skip it.");
                }
            }

            Processor<DialogueCommandBase> processor = new Processor<DialogueCommandBase>(processables.ToArray());
            this.onCompleted = onCompleted;
            processor.Start(OnDialogueCommandCompleted, onForceQuit);
        }

        private void OnDialogueCommandCompleted()
        {
            onCompleted?.Invoke();
        }
    }
}