namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class ShowCharacter : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ShowCharacterFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new ShowCharacter();
        }
    }
}
