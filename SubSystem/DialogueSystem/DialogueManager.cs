using System.Collections.Generic;

namespace KahaGameCore.SubSystem.DialogueSystem
{
    public class DialogueManager
    {
        public static DialogueManager Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new System.Exception("DialogueManager is not initialized, use Initialize() first.");
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

        public void TriggerDialogue(int id, IDialogueView dialogueView)
        {
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
            Common.GeneralCoroutineRunner.Instance.StartCoroutine(IECheckIsPlayerSelectingOption());
        }

        private System.Collections.IEnumerator IECheckIsPlayerSelectingOption()
        {
            while (currentUsingDialogueView.IsWaitingSelection())
            {
                yield return null;
            }

            dialogueProcesser = null;
            currentUsingDialogueView.Hide();
            if (pendingDialogueIDs.Count > 0)
            {
                int id = pendingDialogueIDs[0];
                pendingDialogueIDs.RemoveAt(0);
                TriggerDialogue(id, currentUsingDialogueView);
            }
        }

        public bool IsProcessing()
        {
            return dialogueProcesser != null;
        }
    }
}

