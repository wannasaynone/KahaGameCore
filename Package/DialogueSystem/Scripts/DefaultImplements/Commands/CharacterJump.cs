namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class CharacterJump : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class CharacterJumpFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new CharacterJump();
        }
    }
}
