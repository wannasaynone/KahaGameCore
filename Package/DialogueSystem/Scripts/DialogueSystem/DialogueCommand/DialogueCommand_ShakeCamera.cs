using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_ShakeCamera : DialogueCommandBase
    {
        private readonly CameraController cameraController;

        public DialogueCommand_ShakeCamera(DialogueData dialogueData, IDialogueView dialogueView, CameraController cameraController) : base(dialogueData, dialogueView)
        {
            this.cameraController = cameraController;
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            float duration = float.Parse(DialogueData.Arg1);
            float magnitude = float.Parse(DialogueData.Arg2);

            if (string.IsNullOrEmpty(DialogueData.Arg3))
            {
                cameraController.ShakeCamera(duration, magnitude, onCompleted);
            }
            else
            {
                cameraController.ShakeCamera(duration, magnitude);
                onCompleted?.Invoke();
            }
        }
    }
}