using System;
using KahaGameCore.Package.DialogueSystem;
using KahaGameCore.Package.EffectProcessor;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_TriggerDialogue : EffectCommandFactoryBase
    {
        private readonly IDialogueView dialogueView;

        public CutsceneCommandFactory_TriggerDialogue(IDialogueView dialogueView)
        {
            this.dialogueView = dialogueView;
        }

        public override EffectCommandBase Create()
        {
            return new TriggerDialogue(dialogueView);
        }
    }
    public class TriggerDialogue : EffectCommandBase
    {
        private readonly IDialogueView dialogueView;

        public TriggerDialogue(IDialogueView dialogueView)
        {
            this.dialogueView = dialogueView;
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            DialogueManager.Instance.TriggerDialogue(new DialogueManager.PendingDialogueData
            {
                id = int.Parse(vars[0]),
                dialogueView = dialogueView,
                onCompleted = () =>
                {
                    onCompleted?.Invoke();
                }
            });
        }
    }
}