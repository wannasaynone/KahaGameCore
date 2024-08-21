using System;
using System.Collections.Generic;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueManager
    {
        public static DialogueManager Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new Exception("DialogueManager is not initialized, use Initialize() first.");
                }
                return instance;
            }
        }
        private static DialogueManager instance;
        public static void Initialize(DialogueData[] allDialogueDatas, IDialogueFactory dialogueFactory)
        {
            instance = new DialogueManager
            {
                allDialogueDatas = allDialogueDatas,
                dialogueFactory = dialogueFactory
            };
        }

        private DialogueManager() { }

        private DialogueProcesser dialogueProcesser;
        private IDialogueView currentUsingDialogueView;
        private DialogueData[] allDialogueDatas;
        private IDialogueFactory dialogueFactory;

        private List<int> pendingDialogueIDs = new List<int>();
        private List<Action> pendingOnAllCompleted = new List<Action>();

        public void TriggerDialogue(int id, IDialogueView dialogueView, Action onAllCompleted = null)
        {
            pendingOnAllCompleted.Add(onAllCompleted);
            if (dialogueProcesser == null)
            {
                currentUsingDialogueView = dialogueView;
                dialogueProcesser = new DialogueProcesser(id, dialogueView, allDialogueDatas, dialogueFactory);
                dialogueProcesser.Process(OnDialogueEnded, OnDialogueEnded);
            }
            else
            {
                pendingDialogueIDs.Add(id);
            }
        }

        private void OnDialogueEnded()
        {
            KahaGameCore.Common.GeneralCoroutineRunner.Instance.StartCoroutine(IECheckIsPlayerSelectingOption());
        }

        private System.Collections.IEnumerator IECheckIsPlayerSelectingOption()
        {
            while (currentUsingDialogueView.IsWaitingSelection())
            {
                yield return null;
            }

            if (pendingDialogueIDs.Count > 0)
            {
                int id = pendingDialogueIDs[0];
                pendingDialogueIDs.RemoveAt(0);
                dialogueProcesser = null;
                TriggerDialogue(id, currentUsingDialogueView);
            }
            else
            {
                currentUsingDialogueView.Hide(delegate
                {
                    dialogueProcesser = null;
                    pendingOnAllCompleted.ForEach((onAllCompleted) => onAllCompleted?.Invoke());
                });
            }
        }
    }
}

