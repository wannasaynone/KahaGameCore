using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_MoveCamera : DialogueCommandBase
    {
        private readonly CameraController cameraController;

        public DialogueCommand_MoveCamera(DialogueData dialogueData, IDialogueView dialogueView, CameraController cameraController) : base(dialogueData, dialogueView)
        {
            this.cameraController = cameraController;
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            float x = float.Parse(DialogueData.Arg1);
            float y = float.Parse(DialogueData.Arg2);

            cameraController.MoveCamera(x, y, onCompleted);
        }
    }
}