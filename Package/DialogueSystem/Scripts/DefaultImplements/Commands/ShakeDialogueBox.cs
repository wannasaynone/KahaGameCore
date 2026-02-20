namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public class ShakeDialogueBox : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ShakeDialogueBoxFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new ShakeDialogueBox();
        }
    }
}
