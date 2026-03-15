using System;
using System.Collections.Generic;
using KahaGameCore.GameData.Implemented;
using ProjectBSR.DialogueSystem.View;

namespace ProjectBSR.DialogueSystem
{
    public class DialogueManager
    {
        public static Action<int> OnAnyDialogueReadyToStart; // Parameter is dialogueId
        public static Action<int> OnAnyDialogueEnded; // Parameter is dialogueId

        private readonly DialogueView dialogueView;
        private readonly GameStaticDataManager staticDataManager;
        private readonly DialogueCommandFactoryContainer commandFactoryContainer;
        private readonly ICGProvider cgProvider;
        private readonly IAudioProvider audioProvider;
        private readonly AudioManager audioManager;
        private readonly List<OptionData> pendingOptions = new List<OptionData>();

        private class DialogueSession
        {
            public int DialogueId { get; }
            public Action OnDialogueComplete { get; }
            public int CurrentLine { get; set; } = 0;

            private readonly List<DialogueData> dialogueLines;

            public DialogueSession(int dialogueId, Action onDialogueComplete, GameStaticDataManager staticDataManager)
            {
                DialogueId = dialogueId;
                OnDialogueComplete = onDialogueComplete;
                dialogueLines = new List<DialogueData>();
                DialogueData[] dialogueDatas = staticDataManager.GetAllGameData<DialogueData>();
                foreach (var data in dialogueDatas)
                {
                    if (data.ID == dialogueId)
                    {
                        dialogueLines.Add(data);
                    }
                }
                dialogueLines.Sort((a, b) => a.Line.CompareTo(b.Line));
            }

            public DialogueData GetDialogueDataByLine(int line)
            {
                return dialogueLines.Find(data => data.Line == line);
            }
        }

        private readonly Queue<DialogueSession> dialogueQueue = new Queue<DialogueSession>();
        private DialogueSession activeSession = null;

        public DialogueManager(DialogueView dialogueView, GameStaticDataManager staticDataManager, DialogueCommandFactoryContainer commandFactoryContainer = null, ICGProvider cgProvider = null, IAudioProvider audioProvider = null)
        {
            this.dialogueView = dialogueView ?? throw new ArgumentNullException(nameof(dialogueView), "[DialogueManager] DialogueView reference is required.");
            this.staticDataManager = staticDataManager ?? throw new ArgumentNullException(nameof(staticDataManager), "[DialogueManager] GameStaticDataManager reference is required.");
            this.commandFactoryContainer = commandFactoryContainer;
            this.cgProvider = cgProvider;
            this.audioProvider = audioProvider;

            if (commandFactoryContainer == null)
            {
                commandFactoryContainer = new DialogueCommandFactoryContainer();
                commandFactoryContainer.RegisterFactory("Say", new DefaultImplements.Command.SayFactory());
                commandFactoryContainer.RegisterFactory("BlackIn", new DefaultImplements.Command.BlackInFactory());
                commandFactoryContainer.RegisterFactory("BlackOut", new DefaultImplements.Command.BlackOutFactory());
                commandFactoryContainer.RegisterFactory("AddOption", new DefaultImplements.Command.AddOptionFactory());
                commandFactoryContainer.RegisterFactory("ShowOptions", new DefaultImplements.Command.ShowOptionsFactory());
                commandFactoryContainer.RegisterFactory("GoToLine", new DefaultImplements.Command.GoToLineFactory());
                commandFactoryContainer.RegisterFactory("ShowFullScreenImage", new DefaultImplements.Command.ShowFullScreenImageFactory());
                commandFactoryContainer.RegisterFactory("HideFullScreenImage", new DefaultImplements.Command.HideFullScreenImageFactory());
                commandFactoryContainer.RegisterFactory("HideDialogueBox", new DefaultImplements.Command.HideDialogueBoxFactory());
                commandFactoryContainer.RegisterFactory("PlaySoundEffect", new DefaultImplements.Command.PlaySoundEffectFactory());
                commandFactoryContainer.RegisterFactory("PlayBackgroundMusic", new DefaultImplements.Command.PlayBackgroundMusicFactory());
                commandFactoryContainer.RegisterFactory("ShowCharacter", new DefaultImplements.Command.ShowCharacterFactory());
                commandFactoryContainer.RegisterFactory("HideCharacter", new DefaultImplements.Command.HideCharacterFactory());
                commandFactoryContainer.RegisterFactory("ChangeCharacter", new DefaultImplements.Command.ChangeCharacterFactory());
                this.commandFactoryContainer = commandFactoryContainer;
                UnityEngine.Debug.Log("[DialogueManager] DialogueCommandFactoryContainer is not set. Using a default one with built-in commands registered.");
            }

            if (audioProvider == null)
            {
                this.audioProvider = new DefaultImplements.AddressablesAudioProvider();
                UnityEngine.Debug.Log("[DialogueManager] AudioProvider is not set. Using default AddressablesAudioProvider.");
            }

            if (cgProvider == null)
            {
                this.cgProvider = new DefaultImplements.AddressablesCGProvider();
                UnityEngine.Debug.Log("[DialogueManager] CGProvider is not set. Using default AddressablesCGProvider.");
            }

            UnityEngine.GameObject audioManagerObj = new UnityEngine.GameObject("[DialogueAudioManager]");
            audioManager = audioManagerObj.AddComponent<AudioManager>();
            audioManagerObj.transform.SetParent(dialogueView.transform); // Make it a child of dialogueView for better hierarchy organization
        }

        public void StartDialogue(int dialogueId, Action onDialogueComplete)
        {
            dialogueQueue.Enqueue(new DialogueSession(dialogueId, onDialogueComplete, staticDataManager));
            if (activeSession == null)
            {
                ShowNextDialogue();
            }
        }

        private void ShowNextDialogue()
        {
            if (dialogueQueue.Count == 0)
            {
                audioManager.StopBGM();
                dialogueView.gameObject.SetActive(false);
                return;
            }

            activeSession = dialogueQueue.Dequeue();

            OnAnyDialogueReadyToStart?.Invoke(activeSession.DialogueId);

            ShowNextLine();
        }

        public void ShowNextLine()
        {
            if (activeSession == null)
            {
                return;
            }

            activeSession.CurrentLine++;

            DialogueData dialogueData = activeSession.GetDialogueDataByLine(activeSession.CurrentLine);
            if (dialogueData != null)
            {
                DialogueCommandBase effectCommand = commandFactoryContainer.GetDialogueCommand(dialogueData.Command);
                if (effectCommand != null)
                {
                    string[] args = new string[] {
                        dialogueData.Arg1,
                        dialogueData.Arg2,
                        dialogueData.Arg3,
                        dialogueData.Arg4,
                        dialogueData.Arg5
                    };

                    DialogueContext context = new DialogueContext
                    {
                        view = dialogueView,
                        cgProvider = cgProvider,
                        audioProvider = audioProvider,
                        audioManager = audioManager,
                        curDialogueId = activeSession.DialogueId,
                        onComplete = ShowNextLine,
                        onForceQuit = ForceQuitCurrentDialogue,
                        pendingOptions = pendingOptions,
                        onRequestJumpToLine = JumpToLine,
                        onRequestJumpToDialogueLine = JumpToDialogueLine
                    };

                    effectCommand.Process(args, context);
                }
                else
                {
                    UnityEngine.Debug.LogError("[DialogueManager][ShowNextLine] Invalid command=" + dialogueData.Command + " for dialogueId=" + activeSession.DialogueId + " line=" + activeSession.CurrentLine + ". Skipping to next line.");
                    ShowNextLine();
                }
            }
            else
            {
                activeSession.OnDialogueComplete?.Invoke();
                OnAnyDialogueEnded?.Invoke(activeSession.DialogueId);
                activeSession = null;
                ShowNextDialogue();
            }
        }

        private void JumpToLine(int targetLine)
        {
            if (activeSession == null)
            {
                UnityEngine.Debug.LogError("[DialogueManager][JumpToLine] No active session to jump to line " + targetLine);
                return;
            }

            // Set CurrentLine to targetLine - 1 because ShowNextLine will increment it
            activeSession.CurrentLine = targetLine - 1;
            ShowNextLine();
        }

        private void JumpToDialogueLine(int dialogueId, int targetLine)
        {
            // Mark the current dialogue as ended before jumping to the new one
            if (activeSession != null)
            {
                OnAnyDialogueEnded?.Invoke(activeSession.DialogueId);
            }

            // Replace the active session with a new one for the target dialogue
            activeSession = new DialogueSession(dialogueId, activeSession?.OnDialogueComplete, staticDataManager);
            // Set CurrentLine to targetLine - 1 because ShowNextLine will increment it
            activeSession.CurrentLine = targetLine - 1;
            ShowNextLine();
        }

        private void ForceQuitCurrentDialogue()
        {
            if (activeSession != null)
            {
                activeSession = null;
            }
            ShowNextDialogue();
        }
    }
}
