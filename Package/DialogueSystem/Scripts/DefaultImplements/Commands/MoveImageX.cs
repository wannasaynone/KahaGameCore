namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class MoveImageX : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class MoveImageXFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new MoveImageX();
        }
    }
}
