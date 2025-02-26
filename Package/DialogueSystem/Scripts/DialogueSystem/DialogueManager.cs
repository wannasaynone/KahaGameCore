using System;
using System.Collections.Generic;
using KahaGameCore.GameData.Implemented;
using KahaGameCore.Package.DialogueSystem.DialogueCommand;

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

        private DialogueProcesser dialogueProcesser;
        private IDialogueFactory dialogueFactory;

        public class PendingDialogueData
        {
            public int id;
            public IDialogueView dialogueView;
            public Action onCompleted;
        }

        private PendingDialogueData currentDialogueData;
        private List<PendingDialogueData> pendingDialogueIDs = new List<PendingDialogueData>();

        public void TriggerDialogue(PendingDialogueData pendingDialogueData)
        {
            if (dialogueProcesser == null)
            {
                currentDialogueData = pendingDialogueData;
                dialogueProcesser = new DialogueProcesser(pendingDialogueData.id, pendingDialogueData.dialogueView, GameStaticDataManager.GetAllGameData<DialogueData>(), dialogueFactory);
                dialogueProcesser.Process(OnDialogueEnded, OnDialogueEnded);
            }
            else
            {
                pendingDialogueIDs.Add(pendingDialogueData);
            }
        }

        private void OnDialogueEnded()
        {
            Common.GeneralCoroutineRunner.Instance.StartCoroutine(IECheckIsPlayerSelectingOption());
        }

        private System.Collections.IEnumerator IECheckIsPlayerSelectingOption()
        {
            while (currentDialogueData.dialogueView.IsWaitingSelection())
            {
                yield return null;
            }

            PlayerManager.Instance.Player.ReadDialogue(dialogueProcesser.CurrentProcessingID);
            PlayerManager.Instance.SavePlayer();

            if (pendingDialogueIDs.Count > 0)
            {
                currentDialogueData.onCompleted?.Invoke();

                PendingDialogueData nextDialogueData = pendingDialogueIDs[0];
                pendingDialogueIDs.RemoveAt(0);

                dialogueProcesser = null;
                TriggerDialogue(nextDialogueData);
            }
            else
            {
                if (currentDialogueData.dialogueView.IsVisible)
                {
                    currentDialogueData.dialogueView.Hide(delegate
                                    {
                                        dialogueProcesser = null;
                                        DialogueCommand_CreateCreateGeneralAnimationPlayer.ClearGeneralAnimationPlayers();
                                        currentDialogueData.onCompleted?.Invoke();
                                    });
                }
                else
                {
                    dialogueProcesser = null;
                    DialogueCommand_CreateCreateGeneralAnimationPlayer.ClearGeneralAnimationPlayers();
                    currentDialogueData.onCompleted?.Invoke();
                }
            }
        }
    }
}

