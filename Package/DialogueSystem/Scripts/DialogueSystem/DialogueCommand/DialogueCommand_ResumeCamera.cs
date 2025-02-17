using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_ResumeCamera : DialogueCommandBase
    {
        private readonly CameraController cameraController;

        public DialogueCommand_ResumeCamera(DialogueData dialogueData, IDialogueView dialogueView, CameraController cameraController) : base(dialogueData, dialogueView)
        {
            this.cameraController = cameraController;
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            cameraController.ResuemTarget();
            onCompleted?.Invoke();
        }
    }
}