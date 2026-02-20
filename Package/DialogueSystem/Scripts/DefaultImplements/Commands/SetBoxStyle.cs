namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class SetBoxStyle : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class SetBoxStyleFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new SetBoxStyle();
        }
    }
}
