using System;
using KahaGameCore.Package.EffectProcessor.Processor;

namespace KahaGameCore.Package.DialogueSystem
{
    public abstract class DialogueCommandBase : IProcessable
    {
        public DialogueCommandBase(DialogueData dialogueData, IDialogueView dialogueView)
        {
            DialogueData = dialogueData;
            DialogueView = dialogueView;
        }

        protected DialogueData DialogueData { get; private set; }
        protected IDialogueView DialogueView { get; private set; }

        public abstract void Process(Action onCompleted, Action onForceQuit);
    }
}