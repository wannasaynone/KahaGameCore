namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class ChangeCharacter : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ChangeCharacterFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new ChangeCharacter();
        }
    }
}
