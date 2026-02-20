namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class MoveCharacterY : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class MoveCharacterYFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new MoveCharacterY();
        }
    }
}
