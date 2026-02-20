namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class ScaleImage : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ScaleImageFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new ScaleImage();
        }
    }
}
