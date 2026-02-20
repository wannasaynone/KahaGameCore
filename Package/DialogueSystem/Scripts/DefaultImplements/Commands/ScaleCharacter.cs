namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class ScaleCharacter : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ScaleCharacterFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new ScaleCharacter();
        }
    }
}
