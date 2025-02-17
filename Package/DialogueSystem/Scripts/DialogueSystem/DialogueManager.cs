using System;
using System.Collections.Generic;
using KahaGameCore.GameData.Implemented;

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
        public GameStaticDataManager GameStaticDataManager { get; private set; }
        public static void Initialize(GameStaticDataManager gameStaticDataManager, IDialogueFactory dialogueFactory)
        {
            instance = new DialogueManager
            {
                GameStaticDataManager = gameStaticDataManager,
                dialogueFactory = dialogueFactory
            };
        }

        private DialogueManager() { }

        public event Action OnDialogueViewHided;


        private DialogueProcesser dialogueProcesser;
        private IDialogueView currentUsingDialogueView;
        private IDialogueFactory dialogueFactory;

        private List<int> pendingDialogueIDs = new List<int>();

        public void TriggerDialogue(int id, IDialogueView dialogueView)
        {
            if (dialogueProcesser == null)
            {
                currentUsingDialogueView = dialogueView;
                dialogueProcesser = new DialogueProcesser(id, dialogueView, GameStaticDataManager.GetAllGameData<DialogueData>(), dialogueFactory);
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

            PlayerManager.Instance.Player.ReadDialogue(dialogueProcesser.CurrentProcessingID);
            PlayerManager.Instance.SavePlayer();

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
                    OnDialogueViewHided?.Invoke();
                });
            }
        }
    }
}

