namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class ShakeCamera : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ShakeCameraFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new ShakeCamera();
        }
    }
}
