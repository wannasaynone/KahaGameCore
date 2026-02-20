namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class HideCharacter : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class HideCharacterFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new HideCharacter();
        }
    }
}
