namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class MoveImageY : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class MoveImageYFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new MoveImageY();
        }
    }
}
