namespace KahaGameCore.Dialogue.DefaultImplements.Command
{
    public class White : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class WhiteFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new White();
        }
    }
}
