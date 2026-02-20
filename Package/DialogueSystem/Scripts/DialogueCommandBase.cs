using System.Collections.Generic;
using ProjectBSR.DialogueSystem.View;

namespace ProjectBSR.DialogueSystem
{
    public class OptionData
    {
        public string text;
        public int targetDialogueId;
        public int targetLine;
    }

    public class DialogueContext
    {
        public DialogueView view;
        public ICGProvider cgProvider;
        public IAudioProvider audioProvider;
        public AudioManager audioManager;
        public int curDialogueId;
        public System.Action onComplete;
        public System.Action onForceQuit;
        public List<OptionData> pendingOptions;
        public System.Action<int> onRequestJumpToLine;
        public System.Action<int, int> onRequestJumpToDialogueLine;
    }

    public abstract class DialogueCommandBase
    {
        public abstract void Process(string[] args, DialogueContext context);
    }
}
