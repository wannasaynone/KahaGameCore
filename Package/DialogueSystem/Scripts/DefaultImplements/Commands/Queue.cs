namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class Queue : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class QueueFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new Queue();
        }
    }
}
