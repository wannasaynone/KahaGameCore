namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class CoverColor : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class CoverColorFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new CoverColor();
        }
    }
}
