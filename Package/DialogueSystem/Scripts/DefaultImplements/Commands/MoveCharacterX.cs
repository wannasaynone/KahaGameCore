namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class MoveCharacterX : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class MoveCharacterXFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new MoveCharacterX();
        }
    }
}
